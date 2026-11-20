namespace NetEvolve.Schemas.Schematron.Scopes;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>
/// Binds the variables of one run lazily and demand-driven (design #150 EP-5): a slot is evaluated at its first read, once per run
/// for the schema and phase level, once per pattern pass for the pattern level, and once per firing for the rule level. The
/// evaluator announces a pattern pass with <see cref="BeginPattern"/> and a firing with <see cref="BeginFiring"/>; both only move
/// an epoch counter, so nothing is cleared. A failed slot stays failed for its span and is reported once, at the first read.
/// </summary>
/// <remarks>
/// The walk over the slots a value reads is iterative, with an explicit stack, so a chain of any length is bound without recursion.
/// A slot that is already bound is found by an array index and costs no allocation. Not thread-safe: one binder per run.
/// A global <c>xsl:variable</c> or <c>xsl:param</c> (SCH-66) is a slot of the schema level and is bound here like a schema
/// <see langword="let"/>: lazily, once per run, with the circularity error XTDE0640 and a caller value for a parameter.
/// Limitation: the values themselves live in the variable table of the run of the binding; a temporary tree as a value
/// (SCH-84, #794) is not bound here yet.
/// A cycle that runs through the body of a stylesheet function cannot be seen by the walk over the slots a value reads: the call
/// binds the slots of the body from inside the evaluation of the value. A slot that is in progress when <see cref="EnsureBound"/>
/// is entered is therefore a cycle (XTDE0640), reported once; the slots on the path of the evaluation fail without a new issue.
/// </remarks>
internal sealed class SlotBinder : ISlotBinder
{
    private const byte Unbound = 0;
    private const byte Bound = 1;
    private const byte InProgress = 2;
    private const byte Failed = 3;

    private readonly ScopePlan _plan;
    private readonly IQueryRun _run;
    private readonly QueryFocus _document;
    private readonly IProblemSink _problems;
    private readonly VariableSlot?[] _declarations;
    private readonly string?[] _external;
    private readonly int[] _stamps;
    private readonly byte[] _states;
    private int[] _stack = new int[16];
    private int _depth;
    private int _patternEpoch = 1;
    private int _firingEpoch = 1;
    private int _evaluating;
    private bool _cycleReported;
    private RuleScope? _rule;
    private QueryFocus _focus;

    /// <summary>Initializes a new instance of the <see cref="SlotBinder"/> class.</summary>
    /// <param name="plan">The scopes of the compiled schema.</param>
    /// <param name="run">The run that evaluates and holds the values.</param>
    /// <param name="document">The focus of the schema, phase, and pattern level: the document node.</param>
    /// <param name="problems">Receives the problem of a failed slot, once.</param>
    internal SlotBinder(ScopePlan plan, IQueryRun run, in QueryFocus document, IProblemSink problems)
    {
        Argument.ThrowIfNull(plan);
        Argument.ThrowIfNull(run);
        Argument.ThrowIfNull(problems);

        _plan = plan;
        _run = run;
        _document = document;
        _focus = document;
        _problems = problems;
        _declarations = new VariableSlot?[plan.SlotCount];
        _external = new string?[plan.SlotCount];
        _stamps = new int[plan.SlotCount];
        _states = new byte[plan.SlotCount];
        Install(plan.Globals);
        Install(plan.PhaseLets);
    }

    /// <summary>Sets the caller value of a schema-level <c>param</c>; it is bound as a literal string and the default is not evaluated.</summary>
    /// <param name="name">The parameter name.</param>
    /// <param name="value">The value.</param>
    /// <returns><see langword="false"/> when no such parameter is declared.</returns>
    internal bool SetParameter(string name, string value)
    {
        Argument.ThrowIfNull(name);
        Argument.ThrowIfNull(value);

        for (var i = 0; i < _plan.Globals.Count; i++)
        {
            var declaration = _plan.Globals[i];
            if (declaration.IsParameter && string.Equals(declaration.Name, name, StringComparison.Ordinal))
            {
                _external[declaration.Slot] = value;
                return true;
            }
        }

        return false;
    }

    /// <summary>Starts a pattern pass: the pattern block of slots becomes the variables of <paramref name="pattern"/>, unbound.</summary>
    /// <param name="pattern">The pattern about to run.</param>
    internal void BeginPattern(PatternScope pattern)
    {
        Argument.ThrowIfNull(pattern);

        _patternEpoch++;
        Install(pattern.Lets);
        ReportStatic(pattern.Lets);
    }

    /// <summary>Starts a firing: the rule variables become unbound and the matched node is their focus. Once per firing, without allocation.</summary>
    /// <param name="rule">The rule that fired.</param>
    /// <param name="focus">The matched node.</param>
    internal void BeginFiring(RuleScope rule, in QueryFocus focus)
    {
        Argument.ThrowIfNull(rule);

        _firingEpoch++;
        _focus = focus;
        if (!ReferenceEquals(_rule, rule))
        {
            _rule = rule;
            Install(rule.Lets);
        }

        ReportStatic(rule.Lets);
    }

    /// <inheritdoc />
    public QueryStatus EnsureBound(ReadOnlySpan<int> slots)
    {
        var status = QueryStatus.Ok;
        for (var i = 0; i < slots.Length; i++)
        {
            var slot = slots[i];
            Debug.Assert((uint)slot < (uint)_states.Length, "The slot is outside the plan.");

            var state = StateOf(slot);
            if (state == Bound)
            {
                continue;
            }

            // Entered from inside the evaluation of the value of this very slot, through the body of a function: a cycle.
            if (state == InProgress)
            {
                ReportCycle(slot);
                _cycleReported = _evaluating > 0;
                status = QueryStatus.Problem;
            }
            else if (state == Failed || Resolve(slot) != Bound)
            {
                status = QueryStatus.Problem;
            }
        }

        return status;
    }

    // Re-entrant: evaluating a value can call a function whose body binds slots, which starts a nested walk. The nested walk
    // starts above the entries of this one (_depth), so the slots in progress are exactly the slots on the path of the evaluation.
    private byte Resolve(int root)
    {
        var start = _depth;
        try
        {
            var top = Push(start, root);
            while (top > start)
            {
                var slot = _stack[top - 1];
                var state = StateOf(slot);
                if (state is Bound or Failed)
                {
                    top--;
                }
                else if (state == InProgress)
                {
                    // Every value it reads is settled, because those were pushed above it and have been removed again.
                    top--;
                    _depth = top;
                    Settle(slot);
                }
                else
                {
                    top = Expand(slot, top);
                }
            }
        }
        finally
        {
            _depth = start;
        }

        return StateOf(root);
    }

    // Marks the slot as in progress and pushes the unbound slots its value reads; a read of a slot in progress is a cycle.
    private int Expand(int slot, int top)
    {
        Mark(slot, InProgress);
        var value = _declarations[slot]?.Value;
        var reads = value is null ? default : value.ReferencedSlots;
        for (var i = 0; i < reads.Length; i++)
        {
            var read = reads[i];
            var state = StateOf(read);
            if (state == Unbound)
            {
                top = Push(top, read);
            }
            else if (state == InProgress)
            {
                ReportCycle(read);
                Mark(slot, Failed);
                break;
            }
        }

        return top;
    }

    // Evaluates the value of a slot whose dependencies are settled; a failed dependency fails the slot without a new issue.
    private void Settle(int slot)
    {
        var declaration = _declarations[slot];
        if (declaration is null)
        {
            Mark(slot, Failed);
            return;
        }

        var reads = declaration.Value is null ? default : declaration.Value.ReferencedSlots;
        for (var i = 0; i < reads.Length; i++)
        {
            if (StateOf(reads[i]) != Bound)
            {
                Mark(slot, Failed);
                return;
            }
        }

        if (_external[slot] is { } literal)
        {
            _run.BindVariable(slot, literal);
            Mark(slot, Bound);
        }
        else if (declaration.Value is null)
        {
            Fail(
                slot,
                new SchematronProblem(
                    SchematronDiagnosticCodes.ParamMissing,
                    $"The parameter '{declaration.Name}' has neither a default value nor a value from the caller.",
                    declaration.Location
                )
            );
        }
        else
        {
            Evaluate(slot, declaration.Value, declaration.Level == ScopeLevel.Rule ? _focus : _document);
        }
    }

    // A cycle that was reported from inside the evaluation fails every slot on its path, and the cycle is the only issue.
    private void Evaluate(int slot, QueryExpression value, in QueryFocus focus)
    {
        QueryStatus status;
        _evaluating++;
        try
        {
            status = _run.TryBindVariable(slot, value, in focus);
        }
        finally
        {
            _evaluating--;
        }

        if (status == QueryStatus.Ok)
        {
            Mark(slot, Bound);
        }
        else if (_cycleReported)
        {
            Mark(slot, Failed);
        }
        else
        {
            Fail(slot, _run.GetProblem());
        }

        _cycleReported &= _evaluating > 0;
    }

    private void Fail(int slot, in SchematronProblem problem)
    {
        Mark(slot, Failed);
        _problems.Add(in problem);
    }

    private void ReportCycle(int slot)
    {
        var declaration = _declarations[slot];
        var problem = new SchematronProblem(
            SchematronDiagnosticCodes.EvaluationFailed,
            $"The variable '{declaration?.Name}' depends on itself.",
            declaration?.Location ?? default,
            "XTDE0640"
        );
        _problems.Add(in problem);
    }

    // A constant that does not match its declared type is a failed slot from the start of its span, reported once per span.
    private void ReportStatic(IReadOnlyList<VariableSlot> lets)
    {
        for (var i = 0; i < lets.Count; i++)
        {
            if (lets[i].StaticProblem is { } problem)
            {
                Fail(lets[i].Slot, in problem);
            }
        }
    }

    private void Install(IReadOnlyList<VariableSlot> lets)
    {
        for (var i = 0; i < lets.Count; i++)
        {
            _declarations[lets[i].Slot] = lets[i];
        }
    }

    private int Push(int top, int slot)
    {
        if (top == _stack.Length)
        {
            Array.Resize(ref _stack, top * 2);
        }

        _stack[top] = slot;
        return top + 1;
    }

    private int EpochOf(int slot) =>
        slot < _plan.PatternStart ? 1
        : slot < _plan.RuleStart ? _patternEpoch
        : _firingEpoch;

    private byte StateOf(int slot) => _stamps[slot] == EpochOf(slot) ? _states[slot] : Unbound;

    private void Mark(int slot, byte state)
    {
        _stamps[slot] = EpochOf(slot);
        _states[slot] = state;
    }
}
