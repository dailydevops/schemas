namespace NetEvolve.Schemas.Schematron.Tests.Unit.Parsing.Core;

using NetEvolve.Schemas.Schematron.Model;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Parsing.Core.TestTree;

public class CoreParserElementTests
{
    private static SchematronPattern Pattern(SchematronSchema schema) => (SchematronPattern)schema.Patterns[0];

    private static SchematronRule Rule(SchematronSchema schema) => Pattern(schema).Rules[0];

    private static SchematronAssert FirstAssert(SchematronSchema schema) => (SchematronAssert)Rule(schema).Items[0];

    private static async Task AssertLocation(SchematronNode node, int line, int column)
    {
        await Assert.That(node.Location.Source).IsEqualTo(CoreParserFixture.SystemId);
        await Assert.That(node.Location.Line).IsEqualTo(line);
        await Assert.That(node.Location.Column).IsEqualTo(column);
    }

    [Test]
    public async Task Schema_Attributes_AreModeled()
    {
        var schema = CoreParserFixture.Full();

        await Assert.That(schema.Id).IsEqualTo("s1");
        await Assert.That(schema.SchemaVersion).IsEqualTo("1.0");
        await Assert.That(schema.EditionText).IsEqualTo("2025");
        await Assert.That(schema.Edition).IsEqualTo(SchematronEdition.Iso2025);
        await Assert.That(schema.DefaultPhase).IsEqualTo("ph1");
        await Assert.That(schema.QueryBinding).IsEqualTo("xslt2");
        await Assert.That(schema.Patterns.Count).IsEqualTo(1);
        await Assert.That(schema.Phases.Count).IsEqualTo(1);
        await Assert.That(schema.Namespaces.Count).IsEqualTo(1);
        await Assert.That(schema.Lets.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Schema_WithoutEdition_Is2016()
    {
        var (schema, _, _) = CoreParserFixture.Parse(E("schema", 1, 1, E("pattern", 2, 2)));

        await Assert.That(schema!.Edition).IsEqualTo(SchematronEdition.Iso2016);
        await Assert.That(schema.EditionText).IsNull();
    }

    [Test]
    public async Task Schema_ExplicitEdition2016_Parses()
    {
        var (schema, _, _) = CoreParserFixture.Parse(
            E("schema", 1, 1, A("schematronEdition", "2016"), E("pattern", 2, 2))
        );

        await Assert.That(schema!.Edition).IsEqualTo(SchematronEdition.Iso2016);
        await Assert.That(schema.EditionText).IsEqualTo("2016");
    }

    [Test]
    public async Task Schema_Location_IsElementStart() => await AssertLocation(CoreParserFixture.Full(), 1, 1);

    [Test]
    public async Task Ns_Attributes_AreModeled()
    {
        var ns = CoreParserFixture.Full().Namespaces[0];

        await Assert.That(ns.Prefix).IsEqualTo("x");
        await Assert.That(ns.NamespaceName).IsEqualTo("urn:x");
    }

    [Test]
    public async Task Ns_Location_IsElementStart() =>
        await AssertLocation(CoreParserFixture.Full().Namespaces[0], 3, 3);

    [Test]
    public async Task Pattern_Attributes_AreModeled()
    {
        var pattern = Pattern(CoreParserFixture.Full());

        await Assert.That(pattern.Id).IsEqualTo("pat1");
        await Assert.That(pattern.Role).IsEqualTo("r");
        await Assert.That(pattern.IsAbstract).IsFalse();
        await Assert.That(pattern.IsGroup).IsFalse();
        await Assert.That(pattern.Rules.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Pattern_Location_IsElementStart() =>
        await AssertLocation(Pattern(CoreParserFixture.Full()), 8, 3);

    [Test]
    public async Task Rule_Attributes_AreModeled()
    {
        var rule = Rule(CoreParserFixture.Full());

        await Assert.That(rule.Id).IsEqualTo("r1");
        await Assert.That(rule.Context).IsEqualTo("//a");
        await Assert.That(rule.Flags).IsEquivalentTo(["f1", "f2"]);
        await Assert.That(rule.Role).IsEqualTo("rr");
        await Assert.That(rule.Subject).IsEqualTo(".");
        await Assert.That(rule.IsAbstract).IsFalse();
        await Assert.That(rule.Lets.Count).IsEqualTo(1);
        await Assert.That(rule.Items.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Rule_Location_IsElementStart() => await AssertLocation(Rule(CoreParserFixture.Full()), 9, 5);

    [Test]
    public async Task Assert_Attributes_AreModeled()
    {
        var assertion = FirstAssert(CoreParserFixture.Full());

        await Assert.That(assertion.Test).IsEqualTo("x");
        await Assert.That(assertion.Id).IsEqualTo("a1");
        await Assert.That(assertion.Flags).IsEquivalentTo(["A", "B"]);
        await Assert.That(assertion.Role).IsEqualTo("ar");
        await Assert.That(assertion.Severity).IsEqualTo("high");
        await Assert.That(assertion.Subject).IsEqualTo("@s");
        await Assert.That(assertion.DiagnosticIds).IsEquivalentTo(["d1", "d2"]);
        await Assert.That(assertion.PropertyIds).IsEquivalentTo(["pr1"]);
    }

    [Test]
    public async Task Assert_Message_KeepsTextAndInlineParts()
    {
        var parts = FirstAssert(CoreParserFixture.Full()).Message.Parts;

        await Assert.That(parts.Count).IsEqualTo(4);
        await Assert.That(((SchematronText)parts[0]).Text).IsEqualTo("Msg ");
        await Assert.That(((SchematronName)parts[1]).Path).IsEqualTo(".");
        await Assert.That(((SchematronValueOf)parts[2]).Select).IsEqualTo("@x");
        await Assert.That(((SchematronText)((SchematronEmphasis)parts[3]).Parts[0]).Text).IsEqualTo("e");
    }

    [Test]
    public async Task Assert_Location_IsElementStart() =>
        await AssertLocation(FirstAssert(CoreParserFixture.Full()), 11, 7);

    [Test]
    public async Task Report_Attributes_AreModeled()
    {
        var report = (SchematronReport)Rule(CoreParserFixture.Full()).Items[1];

        await Assert.That(report.Test).IsEqualTo("y");
        await Assert.That(((SchematronText)report.Message.Parts[0]).Text).IsEqualTo("Rep");
    }

    [Test]
    public async Task Report_Location_IsElementStart() =>
        await AssertLocation(Rule(CoreParserFixture.Full()).Items[1], 15, 7);

    [Test]
    public async Task Let_Attributes_AreModeled()
    {
        var let = CoreParserFixture.Full().Lets[0];

        await Assert.That(let.Name).IsEqualTo("a");
        await Assert.That(let.Value).IsEqualTo("1");
        await Assert.That(let.DeclaredType).IsEqualTo("xs:integer");
    }

    [Test]
    public async Task Let_Location_IsElementStart()
    {
        await AssertLocation(CoreParserFixture.Full().Lets[0], 5, 3);
        await AssertLocation(Rule(CoreParserFixture.Full()).Lets[0], 10, 7);
    }

    [Test]
    public async Task Phase_Attributes_AreModeled()
    {
        var phase = CoreParserFixture.Full().Phases[0];

        await Assert.That(phase.Id).IsEqualTo("ph1");
        await Assert.That(phase.Actives.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Phase_Location_IsElementStart() => await AssertLocation(CoreParserFixture.Full().Phases[0], 6, 3);

    [Test]
    public async Task Active_Attributes_AreModeled()
    {
        var active = CoreParserFixture.Full().Phases[0].Actives[0];

        await Assert.That(active.Pattern).IsEqualTo("pat1");
        await Assert.That(((SchematronText)active.Parts[0]).Text).IsEqualTo("Act");
    }

    [Test]
    public async Task Active_Location_IsElementStart() =>
        await AssertLocation(CoreParserFixture.Full().Phases[0].Actives[0], 7, 5);

    [Test]
    public async Task Diagnostics_Items_AreModeled()
    {
        var diagnostics = CoreParserFixture.Full().Diagnostics!;
        var diagnostic = diagnostics.Items[0];

        await Assert.That(diagnostics.Items.Count).IsEqualTo(1);
        await Assert.That(diagnostic.Id).IsEqualTo("d1");
        await Assert.That(diagnostic.Role).IsEqualTo("dr");
        await Assert.That(((SchematronText)diagnostic.Message.Parts[0]).Text).IsEqualTo("Diag");
    }

    [Test]
    public async Task Diagnostics_Location_IsElementStart()
    {
        var diagnostics = CoreParserFixture.Full().Diagnostics!;

        await AssertLocation(diagnostics, 16, 3);
        await AssertLocation(diagnostics.Items[0], 17, 5);
    }

    [Test]
    public async Task Title_Text_IsModeled()
    {
        var title = CoreParserFixture.Full().Title!;

        await Assert.That(((SchematronText)title.Parts[0]).Text).IsEqualTo("Title");
    }

    [Test]
    public async Task Title_Location_IsElementStart() => await AssertLocation(CoreParserFixture.Full().Title!, 2, 3);

    [Test]
    public async Task P_Attributes_AreModeled()
    {
        var p = CoreParserFixture.Full().Paragraphs[0];

        await Assert.That(p.Id).IsEqualTo("p1");
        await Assert.That(p.StyleClass).IsEqualTo("c");
        await Assert.That(p.Icon).IsEqualTo("i");
        await Assert.That(((SchematronText)p.Parts[0]).Text).IsEqualTo("Para");
    }

    [Test]
    public async Task P_Location_IsElementStart() => await AssertLocation(CoreParserFixture.Full().Paragraphs[0], 4, 3);

    [Test]
    public async Task AbstractRule_WithoutContext_IsAccepted()
    {
        var (schema, problems, result) = CoreParserFixture.Parse(
            E("schema", 1, 1, E("pattern", 2, 2, E("rule", 3, 3, A("abstract", "true"), A("id", "ar"))))
        );

        await Assert.That(result).IsTrue();
        await Assert.That(problems.Count).IsEqualTo(0);
        await Assert.That(((SchematronPattern)schema!.Patterns[0]).Rules[0].IsAbstract).IsTrue();
    }

    [Test]
    public async Task ForeignElements_AreSkipped()
    {
        var (schema, problems, result) = CoreParserFixture.Parse(
            E("schema", 1, 1, Foreign("note", 2, 2), E("pattern", 3, 2))
        );

        await Assert.That(result).IsTrue();
        await Assert.That(problems.Count).IsEqualTo(0);
        await Assert.That(schema!.Patterns.Count).IsEqualTo(1);
    }
}
