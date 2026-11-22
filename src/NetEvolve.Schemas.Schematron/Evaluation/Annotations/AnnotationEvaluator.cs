namespace NetEvolve.Schemas.Schematron.Evaluation.Annotations;

using System;
using System.Globalization;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Evaluation.Assertions;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>
/// Resolves the <c>role</c>, <c>flag</c>, and <c>severity</c> of a finding when its assertion fires (design #150 EP-6): per attribute the
/// value of the assertion wins, else that of the rule, else, for <c>role</c> only, that of the pattern or group; a <c>flag</c> has no
/// pattern value and a <c>severity</c> is the assertion's alone. The values are never merged and never change the severity of the
/// issue (design #137 AP-12).
/// </summary>
/// <remarks>
/// <para>
/// A dynamic value is read only when it is needed: an assertion that does not fire reads nothing, and an attribute that a nearer
/// level overrides is not read at all. The value of a rule is read once per firing and that of a pattern or group once per pass,
/// because <see cref="BeginRule"/> and <see cref="BeginPattern"/> forget it. A value must be exactly one item, converted to its string
/// value; a <c>flag</c> is then split at white space and joined with one space and must keep at least one token. A value that fails
/// (the empty sequence, several items, an evaluation error, a type without a text form, a text longer than the limit, or a
/// <c>flag</c> without a token) reports one <see cref="SchematronDiagnosticCodes.EvaluationFailed"/> problem that names the assertion in
/// brackets and leaves the attribute out of that finding; the finding is still delivered. A failed value is not remembered, so every
/// finding that reads it reports it again. A variable that failed was already reported by the binder, so the attribute is only
/// left out. The attribute of the nearest level that is present wins even when it failed: a failed rule role does not fall back to the
/// pattern role.
/// </para>
/// <para>
/// Limitations: the events of an active pattern and a fired rule carry the static role and flags only, because they are delivered before
/// any assertion fires and a dynamic value must cost nothing then; the finding carries the resolved values. A value of a type that
/// <see cref="TextSink"/> cannot convert yet (double, float, dates, durations, untyped atomic, URI, #624) is reported as failed. Not
/// thread-safe: one evaluator per run.
/// </para>
/// </remarks>
internal sealed class AnnotationEvaluator : IDisposable
{
    private readonly IQueryRun _run;
    private readonly ISlotBinder _binder;
    private readonly IProblemSink _problems;
    private readonly TextSink _text;
    private readonly SingleValueSink _single = new();
    private CompiledAnnotations _pattern = CompiledAnnotations.None;
    private CompiledAnnotations _rule = CompiledAnnotations.None;
    private string? _patternRole;
    private string? _ruleRole;
    private string? _ruleFlags;

    /// <summary>Initializes a new instance of the <see cref="AnnotationEvaluator"/> class.</summary>
    /// <param name="run">The run that evaluates the variable references.</param>
    /// <param name="binder">Binds the variables a reference reads.</param>
    /// <param name="problems">Receives the problem of a value that fails.</param>
    /// <param name="maxTextLength">The most characters of one value.</param>
    internal AnnotationEvaluator(IQueryRun run, ISlotBinder binder, IProblemSink problems, int maxTextLength)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(binder);
        ArgumentNullException.ThrowIfNull(problems);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxTextLength, 0);

        _run = run;
        _binder = binder;
        _problems = problems;
        _text = new TextSink(maxTextLength);
    }

    /// <summary>Starts a pattern pass: the value of the pattern or group is read again at its first use.</summary>
    /// <param name="pattern">The annotations of the pattern or group.</param>
    internal void BeginPattern(CompiledAnnotations pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        _pattern = pattern;
        _patternRole = null;
    }

    /// <summary>Starts a firing: the values of the rule are read again at their first use.</summary>
    /// <param name="rule">The annotations of the rule.</param>
    internal void BeginRule(CompiledAnnotations rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        _rule = rule;
        _ruleRole = null;
        _ruleFlags = null;
    }

    /// <summary>Resolves the values of a finding; call it only for an assertion that fired.</summary>
    /// <param name="assertion">The annotations of the assertion.</param>
    /// <param name="constraint">The identifier of the assertion, which names it in the problem of a failed value.</param>
    /// <param name="focus">The context node of the fired rule.</param>
    /// <returns>The values, empty where none applies or the value failed.</returns>
    internal ResolvedAnnotations Resolve(CompiledAnnotations assertion, string constraint, in QueryFocus focus)
    {
        ArgumentNullException.ThrowIfNull(assertion);
        ArgumentNullException.ThrowIfNull(constraint);

        var role = Read(assertion.Role, constraint, in focus)
            ?? Inherit(_rule.Role, ref _ruleRole, constraint, in focus)
            ?? Inherit(_pattern.Role, ref _patternRole, constraint, in focus)
            ?? string.Empty;
        var flags = Read(assertion.Flags, constraint, in focus)
            ?? Inherit(_rule.Flags, ref _ruleFlags, constraint, in focus)
            ?? string.Empty;
        var severity = Read(assertion.Severity, constraint, in focus) ?? string.Empty;
        return new ResolvedAnnotations(role, flags, severity);
    }

    /// <inheritdoc />
    public void Dispose() => _text.Dispose();

    // Null when the attribute is absent, the empty string when it failed.
    private string? Read(CompiledAnnotation? annotation, string constraint, in QueryFocus focus)
    {
        if (annotation is null)
        {
            return null;
        }

        if (annotation.Text is { } text)
        {
            return text;
        }

        return TryEvaluate(annotation, constraint, in focus, out var value) ? value : string.Empty;
    }

    // The value of a rule or pattern is read once for as long as its memo lives; a failure is not memoized.
    private string? Inherit(CompiledAnnotation? annotation, ref string? memo, string constraint, in QueryFocus focus)
    {
        if (annotation is null)
        {
            return null;
        }

        if (annotation.Text is { } text)
        {
            return text;
        }

        if (memo is null)
        {
            if (!TryEvaluate(annotation, constraint, in focus, out var value))
            {
                return string.Empty;
            }

            memo = value;
        }

        return memo;
    }

    private bool TryEvaluate(CompiledAnnotation annotation, string constraint, in QueryFocus focus, out string value)
    {
        value = string.Empty;
        var expression = annotation.Expression!;
        if (_binder.EnsureBound(expression.ReferencedSlots) != QueryStatus.Ok)
        {
            return false;
        }

        _text.Clear();
        _single.Begin(_text);
        if (_run.TryEvaluate(expression, in focus, _single) != QueryStatus.Ok)
        {
            var problem = _run.GetProblem();
            _problems.Add(
                new SchematronProblem(
                    problem.Code,
                    Prefix(constraint) + problem.Message,
                    problem.Location,
                    problem.Constraint
                )
            );
            return false;
        }

        var failure = Failure(annotation);
        if (failure is not null)
        {
            _problems.Add(
                new SchematronProblem(
                    SchematronDiagnosticCodes.EvaluationFailed,
                    Prefix(constraint) + failure,
                    annotation.Location
                )
            );
            return false;
        }

        value = annotation.Kind == AnnotationKind.Flag ? JoinTokens(_text.Written) : _text.ToString();
        if (value.Length == 0 && annotation.Kind == AnnotationKind.Flag)
        {
            _problems.Add(
                new SchematronProblem(
                    SchematronDiagnosticCodes.EvaluationFailed,
                    Prefix(constraint)
                        + $"The flag attribute '{annotation.Source}' has no token; a flag needs at least one.",
                    annotation.Location
                )
            );
            return false;
        }

        return true;
    }

    private string? Failure(CompiledAnnotation annotation)
    {
        var name = annotation.Kind switch
        {
            AnnotationKind.Role => "role",
            AnnotationKind.Flag => "flag",
            _ => "severity",
        };
        if (_single.Count != 1)
        {
            return $"The {name} attribute '{annotation.Source}' must be exactly one value, but it is "
                + (
                    _single.Count == 0
                        ? "the empty sequence."
                        : _single.Count.ToString(CultureInfo.InvariantCulture) + " items."
                );
        }

        if (_text.Failure is { } failure)
        {
            return $"The {name} attribute '{annotation.Source}' could not be converted to text: {failure}";
        }

        return _text.IsTruncated
            ? $"The {name} attribute '{annotation.Source}' is longer than the limit of {_text.MaxLength.ToString(CultureInfo.InvariantCulture)} characters."
            : null;
    }

    private static string Prefix(string constraint) => string.Concat("[", constraint, "] ");

    // Splits at XML white space and joins the tokens with one space.
    private static string JoinTokens(ReadOnlySpan<char> text)
    {
        var buffer = new char[text.Length];
        var length = 0;
        var pending = false;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c is ' ' or '\t' or '\r' or '\n')
            {
                pending = length > 0;
                continue;
            }

            if (pending)
            {
                buffer[length++] = ' ';
                pending = false;
            }

            buffer[length++] = c;
        }

        return new string(buffer, 0, length);
    }
}
