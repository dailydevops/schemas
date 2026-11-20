namespace NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Annotations;

using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Assertions;
using NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Patterns;

/// <summary>Builds the model elements of the annotation tests: assertions, rules, patterns, and groups with <c>flag</c>, <c>role</c>, and <c>severity</c>.</summary>
internal static class AnnotationModel
{
    /// <summary>An assert whose test is false, so it fires.</summary>
    internal static SchematronAssert Failing(
        string? role = null,
        string[]? flags = null,
        string? severity = null,
        string message = "m"
    ) =>
        new(
            AssertionModel.Next(),
            "false",
            null,
            flags ?? [],
            role,
            severity,
            null,
            [],
            [],
            AssertionModel.Message(message)
        );

    /// <summary>An assert whose test is true, so it never fires.</summary>
    internal static SchematronAssert Passing(string? role = null, string[]? flags = null, string? severity = null) =>
        new(
            AssertionModel.Next(),
            "true",
            null,
            flags ?? [],
            role,
            severity,
            null,
            [],
            [],
            AssertionModel.Message("unused")
        );

    /// <summary>A report whose test is true, so it fires.</summary>
    internal static SchematronReport Reporting(
        string? role = null,
        string[]? flags = null,
        string? severity = null,
        string message = "m"
    ) =>
        new(
            AssertionModel.Next(),
            "true",
            null,
            flags ?? [],
            role,
            severity,
            null,
            [],
            [],
            AssertionModel.Message(message)
        );

    internal static SchematronSchema Schema(params SchematronPatternBase[] patterns) => PatternModel.Schema(patterns);

    internal static SchematronAssert Holds() => PatternModel.Holds();

    internal static SchematronLet Let(string name, string value = "'x'") =>
        new(AssertionModel.Next(), name, null, value);

    /// <summary>A rule with the given <c>role</c>, <c>flag</c> tokens, and variables.</summary>
    internal static SchematronRule RuleWith(
        string id,
        string context,
        string? role,
        string[]? flags,
        SchematronLet[]? lets,
        params SchematronAssertion[] assertions
    ) =>
        new(
            AssertionModel.Next(),
            id,
            context,
            null,
            flags ?? [],
            role,
            null,
            false,
            null,
            lets ?? [],
            assertions,
            null
        );

    internal static SchematronRule Rule(string id, string context, params SchematronAssertion[] assertions) =>
        RuleWith(id, context, null, null, null, assertions);

    /// <summary>A pattern with the given <c>role</c> and variables.</summary>
    internal static SchematronPattern PatternWith(
        string id,
        string? role,
        SchematronLet[]? lets,
        params SchematronRule[] rules
    ) => new(AssertionModel.Next(), id, false, null, null, role, null, [], [], lets ?? [], rules, null);

    internal static SchematronPattern Pattern(string id, params SchematronRule[] rules) =>
        PatternWith(id, null, null, rules);

    /// <summary>A group with the given <c>role</c> and variables.</summary>
    internal static SchematronGroup GroupWith(
        string id,
        string? role,
        SchematronLet[]? lets,
        params SchematronRule[] rules
    ) => new(AssertionModel.Next(), id, false, null, null, role, null, [], [], lets ?? [], rules, null);
}
