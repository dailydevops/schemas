namespace NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Patterns;

using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Assertions;

/// <summary>Builds the small schema models of the pattern evaluation tests.</summary>
internal static class PatternModel
{
    /// <summary>An assert whose test is false, so it fires with <paramref name="message"/>.</summary>
    internal static SchematronAssert Fails(string message) =>
        AssertionModel.MakeAssert("false", AssertionModel.Message(message));

    /// <summary>A report whose test is true, so it fires with <paramref name="message"/>.</summary>
    internal static SchematronReport Reports(string message) =>
        AssertionModel.MakeReport("true", AssertionModel.Message(message));

    /// <summary>An assert with the given test; <see langword="true"/> never fires.</summary>
    internal static SchematronAssert Holds(string test = "true") =>
        AssertionModel.MakeAssert(test, AssertionModel.Message("unused"));

    internal static SchematronLet Let(string name, string value) => new(AssertionModel.Next(), name, null, value);

    internal static SchematronRule RuleWithLets(
        string? id,
        string context,
        SchematronAssertion[] assertions,
        SchematronLet[]? lets = null
    ) => new(AssertionModel.Next(), id, context, null, [], null, null, false, null, lets ?? [], assertions, null);

    internal static SchematronRule Rule(string? id, string context, params SchematronAssertion[] assertions) =>
        RuleWithLets(id, context, assertions, null);

    internal static SchematronPattern PatternWith(
        string? id,
        SchematronRule[] rules,
        SchematronLet[]? lets = null,
        bool isAbstract = false,
        string? title = null
    ) =>
        new(
            AssertionModel.Next(),
            id,
            isAbstract,
            null,
            null,
            null,
            title is null ? null : new SchematronTitle(AssertionModel.Next(), [AssertionModel.Text(title)]),
            [],
            [],
            lets ?? [],
            rules,
            null
        );

    internal static SchematronPattern Pattern(string? id, params SchematronRule[] rules) =>
        PatternWith(id, rules, null);

    internal static SchematronGroup Group(string id, params SchematronRule[] rules) =>
        new(AssertionModel.Next(), id, false, null, null, null, null, [], [], [], rules, null);

    internal static SchematronSchema Schema(params SchematronPatternBase[] patterns) =>
        new(
            AssertionModel.Next(),
            null,
            [],
            [],
            [],
            new List<SchematronPatternBase>(patterns),
            null,
            null,
            "s",
            null,
            null,
            SchematronEdition.Iso2025,
            null,
            null,
            [],
            [],
            [],
            []
        );
}
