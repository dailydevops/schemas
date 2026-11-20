namespace NetEvolve.Schemas.Schematron.Evaluation.Assertions;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Evaluation.Annotations;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>
/// An <c>assert</c> or <c>report</c> compiled once in the scope of its rule. Immutable and shared by every run: it holds no
/// buffer and no per-run state.
/// </summary>
internal sealed class CompiledAssertion
{
    internal CompiledAssertion(
        bool isReport,
        string id,
        string ruleId,
        string constraint,
        string test,
        DocumentLocation location,
        QueryExpression testExpression,
        QueryExpression? subject,
        CompiledMessage message,
        CompiledMessage[] diagnostics,
        CompiledMessage[] properties,
        CompiledAnnotations? annotations = null
    )
    {
        Argument.ThrowIfNull(id);
        Argument.ThrowIfNull(ruleId);
        Argument.ThrowIfNull(constraint);
        Argument.ThrowIfNull(test);
        Argument.ThrowIfNull(testExpression);
        Argument.ThrowIfNull(message);
        Argument.ThrowIfNull(diagnostics);
        Argument.ThrowIfNull(properties);

        IsReport = isReport;
        Id = id;
        RuleId = ruleId;
        Constraint = constraint;
        Test = test;
        Location = location;
        TestExpression = testExpression;
        Subject = subject;
        Message = message;
        Diagnostics = diagnostics;
        Properties = properties;
        Annotations = annotations ?? CompiledAnnotations.None;
    }

    /// <summary>Gets a value indicating whether the assertion is a <c>report</c>, which fires when its test is true.</summary>
    internal bool IsReport { get; }

    /// <summary>Gets the author id with the expansion suffix, empty when the assertion has none.</summary>
    internal string Id { get; }

    /// <summary>Gets the id of the rule, empty when it has none.</summary>
    internal string RuleId { get; }

    /// <summary>Gets the stable assertion identifier that goes into <see cref="ValidationIssue.Constraint"/>.</summary>
    internal string Constraint { get; }

    /// <summary>Gets the raw <c>test</c> expression.</summary>
    internal string Test { get; }

    internal DocumentLocation Location { get; }

    internal QueryExpression TestExpression { get; }

    /// <summary>Gets the <c>subject</c> of the assertion, else of the rule; <see langword="null"/> when neither has one.</summary>
    internal QueryExpression? Subject { get; }

    internal CompiledMessage Message { get; }

    internal CompiledMessage[] Diagnostics { get; }

    internal CompiledMessage[] Properties { get; }

    /// <summary>Gets the <c>role</c>, <c>flag</c>, and <c>severity</c> of the assertion itself, before inheritance.</summary>
    internal CompiledAnnotations Annotations { get; }
}
