namespace NetEvolve.Schemas.Schematron.Xslt.Functions;

using System;
using System.Buffers;
using System.Threading;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Construction;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// Calls compiled stylesheet functions for one run (XSLT 2.0 section 10.3, design #150 EP-5, EP-13, EP-16). A call checks
/// cancellation and the depth, binds the schema-level slots its body reads, enters the function scope (no focus), converts each
/// argument to its parameter type by the function conversion rules and binds it, evaluates the body, converts the result, and
/// leaves the scope. The slots of the parameters and local variables are saved before and restored after the call, so a recursive
/// call never disturbs the values of its caller. Not thread-safe: one caller per run.
/// </summary>
/// <remarks>
/// Limitation: the depth counter is the one of the function calls only. Design #150 EP-16 shares one counter with every nested
/// evaluation of the run, which needs the run state of the evaluation package; until then a run that nests calls and instruction
/// recursion is bounded by the sum of both limits.
/// </remarks>
internal sealed class XsltFunctionCaller
{
    /// <summary>The default limit of nested calls (design #150 EP-16).</summary>
    internal const int DefaultMaxDepth = 128;

    /// <summary>The ceiling of the limit (design #150 EP-16).</summary>
    internal const int MaxDepthCeiling = 1024;

    private readonly IXsltFunctionRun _run;
    private readonly ISlotBinder _binder;
    private readonly XmlNameTable _names;
    private readonly CancellationToken _cancellationToken;
    private readonly int _maxDepth;
    private readonly XsltFrame _frame;
    private SchematronProblem _problem;
    private bool _problemIsOfTheRun;

    /// <summary>Initializes a new instance of the <see cref="XsltFunctionCaller"/> class.</summary>
    /// <param name="run">The run that evaluates the bodies and holds the slot values.</param>
    /// <param name="binder">The binder of the schema-level slots of the run.</param>
    /// <param name="names">The name table of the run, for the sequence a body constructs.</param>
    /// <param name="cancellationToken">The token checked at the entry of every call.</param>
    /// <param name="maxDepth">The limit of nested calls, 1 to <see cref="MaxDepthCeiling"/>.</param>
    internal XsltFunctionCaller(
        IXsltFunctionRun run,
        ISlotBinder binder,
        XmlNameTable names,
        CancellationToken cancellationToken,
        int maxDepth = DefaultMaxDepth
    )
    {
        Argument.ThrowIfNull(run);
        Argument.ThrowIfNull(binder);
        Argument.ThrowIfNull(names);
        Argument.ThrowIfLessThan(maxDepth, 1);
        Argument.ThrowIfGreaterThan(maxDepth, MaxDepthCeiling);

        _run = run;
        _binder = binder;
        _names = names;
        _cancellationToken = cancellationToken;
        _maxDepth = maxDepth;
        _frame = new XsltFrame(run);
    }

    /// <summary>Gets the number of calls that are active now.</summary>
    internal int Depth { get; private set; }

    /// <summary>Calls a function.</summary>
    /// <param name="function">The function.</param>
    /// <param name="arguments">The arguments as evaluated, one sequence each; their number is the arity.</param>
    /// <param name="result">The result when the status is <see cref="QueryStatus.Ok"/>.</param>
    /// <returns><see cref="QueryStatus.Problem"/> when a conversion, the body, or a limit failed; the problem is read with <see cref="GetProblem"/>.</returns>
    /// <exception cref="OperationCanceledException">The token of the caller is cancelled.</exception>
    internal QueryStatus TryCall(
        XsltFunctionComponent function,
        ReadOnlySpan<XdmSequence> arguments,
        out XdmSequence result
    )
    {
        Argument.ThrowIfNull(function);
        Argument.ThrowIfNotEqual(arguments.Length, function.Arity);

        result = XdmSequence.Empty;
        _cancellationToken.ThrowIfCancellationRequested();
        if (Depth >= _maxDepth)
        {
            return Fail(
                new SchematronProblem(
                    SchematronDiagnosticCodes.EvaluationLimitStop,
                    $"The limit of {_maxDepth} nested calls of stylesheet functions was reached in a call of '{function.DisplayName}'.",
                    function.Location
                )
            );
        }

        Depth++;
        var saved = function.SlotCount == 0 ? null : ArrayPool<SavedSlot>.Shared.Rent(function.SlotCount);
        var entered = false;
        try
        {
            Save(function, saved);
            if (_binder.EnsureBound(function.ReferencedSlots) == QueryStatus.Problem)
            {
                return FailInRun();
            }

            _run.EnterFunction();
            entered = true;
            var status = Bind(function, arguments);
            if (status != QueryStatus.Ok)
            {
                return status;
            }

            var sink = new ItemSequenceSink(_names);
            if (function.Body.TryExecute(_frame, sink) == QueryStatus.Problem)
            {
                return FailInRun();
            }

            return Convert(function, sink.Result, out result);
        }
        finally
        {
            if (entered)
            {
                _run.ExitFunction();
            }

            Restore(function, saved);
            Depth--;
        }
    }

    /// <summary>Gets the problem of the last call that returned <see cref="QueryStatus.Problem"/>.</summary>
    /// <returns>The problem.</returns>
    internal SchematronProblem GetProblem() => _problemIsOfTheRun ? _run.GetProblem() : _problem;

    private QueryStatus Bind(XsltFunctionComponent function, ReadOnlySpan<XdmSequence> arguments)
    {
        for (var i = 0; i < arguments.Length; i++)
        {
            var parameter = function.Parameters[i];
            var value = arguments[i];
            if (parameter.Type is { } type && !type.TryConvert(in arguments[i], out value, out var diagnostic))
            {
                return Fail(
                    new SchematronProblem(
                        SchematronDiagnosticCodes.EvaluationFailed,
                        $"The argument '{parameter.Name}' of the function '{function.DisplayName}' does not match the required type '{type.Text}' (XTTE0790): {diagnostic.Message}",
                        function.Location,
                        diagnostic.Code
                    )
                );
            }

            _run.SetValue(parameter.Slot, in value);
        }

        return QueryStatus.Ok;
    }

    private QueryStatus Convert(XsltFunctionComponent function, XdmSequence value, out XdmSequence result)
    {
        result = value;
        if (function.ResultType is { } type && !type.TryConvert(in value, out result, out var diagnostic))
        {
            return Fail(
                new SchematronProblem(
                    SchematronDiagnosticCodes.EvaluationFailed,
                    $"The result of the function '{function.DisplayName}' does not match the declared type '{type.Text}' (XTTE0780): {diagnostic.Message}",
                    function.Location,
                    diagnostic.Code
                )
            );
        }

        return QueryStatus.Ok;
    }

    private void Save(XsltFunctionComponent function, SavedSlot[]? saved)
    {
        for (var i = 0; saved is not null && i < function.SlotCount; i++)
        {
            saved[i] = _run.TryGetValue(function.SlotBase + i, out var value) ? new SavedSlot(true, value) : default;
        }
    }

    private void Restore(XsltFunctionComponent function, SavedSlot[]? saved)
    {
        if (saved is null)
        {
            return;
        }

        for (var i = 0; i < function.SlotCount; i++)
        {
            if (saved[i].Bound)
            {
                var value = saved[i].Value;
                _run.SetValue(function.SlotBase + i, in value);
            }
            else
            {
                _run.ClearValue(function.SlotBase + i);
            }
        }

        // Cleared, because a pooled array must not keep the values of a call alive.
        ArrayPool<SavedSlot>.Shared.Return(saved, clearArray: true);
    }

    private QueryStatus Fail(SchematronProblem problem)
    {
        _problem = problem;
        _problemIsOfTheRun = false;
        return QueryStatus.Problem;
    }

    private QueryStatus FailInRun()
    {
        _problemIsOfTheRun = true;
        return QueryStatus.Problem;
    }

    private readonly struct SavedSlot
    {
        internal SavedSlot(bool bound, XdmSequence value)
        {
            Bound = bound;
            Value = value;
        }

        internal bool Bound { get; }

        internal XdmSequence Value { get; }
    }
}
