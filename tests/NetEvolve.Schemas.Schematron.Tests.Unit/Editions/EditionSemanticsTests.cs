namespace NetEvolve.Schemas.Schematron.Tests.Unit.Editions;

using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Patterns;
using NetEvolve.Schemas.Schematron.Tests.Unit.Parsing.Core;
using NetEvolve.Schemas.Schematron.Tests.Unit.Scopes;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Parsing.Core.TestTree;

/// <summary>
/// The edition of a parsed schema reaches the stages that differ by edition, and one schema gives different results per edition
/// (design #106 Q1 and Q11, design #150 EP-2 examples E4 to E6).
/// </summary>
public class EditionSemanticsTests
{
    private static XmlElem Schema(string? edition, params object[] content) =>
        edition is null ? E("schema", 1, 1, content) : E("schema", 1, 1, [A("schematronEdition", edition), .. content]);

    private static XmlElem Rule(string id, string context, string message, int line) =>
        E("rule", line, 5, A("id", id), A("context", context), E("assert", line + 1, 7, A("test", "false"), message));

    /// <summary>The two rules of example E5; the node a matches both.</summary>
    private static XmlElem TwoRules(string container) =>
        E(container, 2, 3, A("id", "g"), Rule("r1", "a", "x", 3), Rule("r2", "*", "y", 5));

    /// <summary>Parses the schema, evaluates it over the document of the examples, and returns the fired rule ids.</summary>
    private static string Fired(XmlElem root)
    {
        var (schema, _, result) = CoreParserFixture.Parse(root);
        if (!result || schema is null)
        {
            return "rejected";
        }

        using var harness = new PatternHarness(schema, PatternTree.E("r", PatternTree.E("a"), PatternTree.E("b")));
        _ = harness.Evaluate();
        return harness.Log.Fired;
    }

    [Test]
    public async Task Edition2025_SelectsTheSemantics()
    {
        var (schema, _, result) = CoreParserFixture.Parse(Schema("2025", E("pattern", 2, 3)));
        var scope = ScopeFixture.Create(schema!).Plan.GlobalScope;

        await Assert.That(result).IsTrue();
        await Assert.That(schema!.Edition).IsEqualTo(SchematronEdition.Iso2025);
        await Assert.That(scope.Edition).IsEqualTo(SchematronEdition.Iso2025);
    }

    [Test]
    public async Task AbsentEdition_SelectsThePre2025Semantics()
    {
        var (schema, _, result) = CoreParserFixture.Parse(Schema(null, E("pattern", 2, 3)));
        var scope = ScopeFixture.Create(schema!).Plan.GlobalScope;

        await Assert.That(result).IsTrue();
        await Assert.That(schema!.Edition).IsEqualTo(SchematronEdition.Iso2016);
        await Assert.That(schema.EditionText).IsNull();
        await Assert.That(scope.Edition).IsEqualTo(SchematronEdition.Iso2016);
    }

    [Test]
    [Arguments("2030")]
    [Arguments("")]
    [Arguments("2025 ")]
    public async Task UnknownEdition_ReportsTheCodedErrorAtTheSchemaElement(string edition)
    {
        var (schema, problems, result) = CoreParserFixture.Parse(Schema(edition, E("pattern", 2, 3)));

        await Assert.That(result).IsFalse();
        await Assert.That(schema).IsNull();
        await Assert.That(problems.Count).IsEqualTo(1);
        await Assert.That(problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.EditionUnknown);
        await Assert.That(problems[0].Message).Contains("'" + edition + "'");
        await Assert.That(problems[0].Location.Source).IsEqualTo(CoreParserFixture.SystemId);
        await Assert.That(problems[0].Location.Line).IsEqualTo(1);
        await Assert.That(problems[0].Location.Column).IsEqualTo(1);
    }

    [Test]
    public async Task GroupSchema_In2025_FiresEveryMatchingRule() =>
        await Assert.That(Fired(Schema("2025", TwoRules("group")))).IsEqualTo("r2,r1,r2,r2");

    [Test]
    public async Task GroupSchema_WithoutEdition_IsRejectedAndNothingFires() =>
        await Assert.That(Fired(Schema(null, TwoRules("group")))).IsEqualTo("rejected");

    [Test]
    [Arguments(null)]
    [Arguments("2016")]
    [Arguments("2025")]
    public async Task PatternSchema_InEveryEdition_FiresTheFirstMatchingRuleOnly(string? edition) =>
        await Assert.That(Fired(Schema(edition, TwoRules("pattern")))).IsEqualTo("r2,r1,r2");
}
