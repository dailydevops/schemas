namespace NetEvolve.Schemas.Schematron.Tests.Unit.Parsing.Edition2025;

using NetEvolve.Schemas.Schematron.Model;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Parsing.Core.TestTree;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Parsing.Edition2025.Edition2025Fixture;

public class Edition2025ElementTests
{
    [Test]
    public async Task Group_InSchema_IsModeled()
    {
        var schema = ParseValid(
            Schema2025(
                E(
                    "group",
                    2,
                    3,
                    A("id", "g1"),
                    A("role", "r"),
                    A("documents", "doc('x')"),
                    A("abstract", "true"),
                    A("is-a", "base"),
                    E("title", 3, 5, "T"),
                    E("p", 4, 5, "P"),
                    E("param", 5, 5, A("name", "n"), A("value", "'v'")),
                    E("let", 6, 5, A("name", "l"), A("value", "1")),
                    E("rule", 7, 5, A("context", "a"), E("report", 8, 7, A("test", "t"), "m"))
                )
            )
        );

        var group = (SchematronGroup)schema.Patterns[0];

        await Assert.That(schema.Patterns.Count).IsEqualTo(1);
        await Assert.That(group.IsGroup).IsTrue();
        await Assert.That(group.Id).IsEqualTo("g1");
        await Assert.That(group.Role).IsEqualTo("r");
        await Assert.That(group.Documents).IsEqualTo("doc('x')");
        await Assert.That(group.IsAbstract).IsTrue();
        await Assert.That(group.IsA).IsEqualTo("base");
        await Assert.That(group.Title).IsNotNull();
        await Assert.That(group.Paragraphs.Count).IsEqualTo(1);
        await Assert.That(group.Parameters[0].Name).IsEqualTo("n");
        await Assert.That(group.Lets[0].Name).IsEqualTo("l");
        await Assert.That(group.Rules[0].Context).IsEqualTo("a");
        await AssertLocation(group, 2, 3);
        await AssertLocation(group.Rules[0], 7, 5);
    }

    [Test]
    public async Task Group_KeepsDocumentOrderWithPatterns()
    {
        var schema = ParseValid(Schema2025(E("pattern", 2, 3), E("group", 3, 3), E("pattern", 4, 3)));

        await Assert.That(schema.Patterns.Count).IsEqualTo(3);
        await Assert.That(schema.Patterns[0].IsGroup).IsFalse();
        await Assert.That(schema.Patterns[1].IsGroup).IsTrue();
        await Assert.That(schema.Patterns[2].IsGroup).IsFalse();
    }

    [Test]
    public async Task Rules_InSchema_IsModeled()
    {
        var schema = ParseValid(
            Schema2025(
                E(
                    "rules",
                    2,
                    3,
                    A("id", "rs1"),
                    E("title", 3, 5, "T"),
                    E("p", 4, 5, "P"),
                    E("rule", 5, 5, A("id", "ar"), A("abstract", "true"), E("report", 6, 7, A("test", "t"), "m"))
                )
            )
        );

        var rules = schema.Rules[0];

        await Assert.That(schema.Rules.Count).IsEqualTo(1);
        await Assert.That(rules.Id).IsEqualTo("rs1");
        await Assert.That(rules.Title).IsNotNull();
        await Assert.That(rules.Paragraphs.Count).IsEqualTo(1);
        await Assert.That(rules.Rules.Count).IsEqualTo(1);
        await Assert.That(rules.Rules[0].IsAbstract).IsTrue();
        await AssertLocation(rules, 2, 3);
        await AssertLocation(rules.Rules[0], 5, 5);
    }

    [Test]
    public async Task Param_InSchema_IsModeled()
    {
        var schema = ParseValid(
            Schema2025(E("param", 2, 3, A("name", "a"), A("value", "1")), E("param", 3, 3, A("name", "b")))
        );

        await Assert.That(schema.Parameters.Count).IsEqualTo(2);
        await Assert.That(schema.Parameters[0].Name).IsEqualTo("a");
        await Assert.That(schema.Parameters[0].Value).IsEqualTo("1");
        await Assert.That(schema.Parameters[1].Value).IsNull();
        await AssertLocation(schema.Parameters[0], 2, 3);
        await AssertLocation(schema.Parameters[1], 3, 3);
    }

    [Test]
    [Arguments("2016")]
    [Arguments("2025")]
    [Arguments(null)]
    public async Task Param_InPattern_IsModeledInEveryEdition(string? edition)
    {
        var schema = ParseValid(
            Schema(
                edition,
                E("pattern", 2, 3, A("abstract", "true"), E("param", 3, 5, A("name", "p"), A("value", "v")))
            )
        );

        var pattern = (SchematronPattern)schema.Patterns[0];

        await Assert.That(pattern.Parameters.Count).IsEqualTo(1);
        await Assert.That(pattern.Parameters[0].Name).IsEqualTo("p");
        await Assert.That(pattern.Parameters[0].Value).IsEqualTo("v");
        await AssertLocation(pattern.Parameters[0], 3, 5);
    }

    [Test]
    [Arguments("2016")]
    [Arguments("2025")]
    public async Task Properties_InSchema_AreModeled(string edition)
    {
        var schema = ParseValid(
            Schema(
                edition,
                E(
                    "properties",
                    2,
                    3,
                    E(
                        "property",
                        3,
                        5,
                        A("id", "pr1"),
                        A("fpi", "pf"),
                        A("icon", "pi"),
                        A("see", "ps"),
                        A("role", "r"),
                        A("scheme", "s"),
                        "Text ",
                        E("value-of", 4, 7, A("select", "@x")),
                        E("emph", 5, 7, "e")
                    ),
                    E("property", 6, 5, A("id", "pr2"))
                ),
                E("pattern", 7, 3)
            )
        );

        var properties = schema.Properties!;
        var first = properties.Items[0];

        await Assert.That(properties.Items.Count).IsEqualTo(2);
        await Assert.That(first.Id).IsEqualTo("pr1");
        await Assert.That(first.Role).IsEqualTo("r");
        await Assert.That(first.Scheme).IsEqualTo("s");
        await Assert.That(first.Fpi).IsEqualTo("pf");
        await Assert.That(first.Icon).IsEqualTo("pi");
        await Assert.That(first.See).IsEqualTo("ps");
        await Assert.That(properties.Items[1].Fpi).IsNull();
        await Assert.That(first.Message.Parts.Count).IsEqualTo(3);
        await Assert.That(((SchematronText)first.Message.Parts[0]).Text).IsEqualTo("Text ");
        await Assert.That(((SchematronValueOf)first.Message.Parts[1]).Select).IsEqualTo("@x");
        await Assert.That(first.Message.Parts[2]).IsTypeOf<SchematronEmphasis>();
        await Assert.That(properties.Items[1].Message.Parts.Count).IsEqualTo(0);
        await AssertLocation(properties, 2, 3);
        await AssertLocation(first, 3, 5);
        await AssertLocation(first.Message, 3, 5);
    }

    [Test]
    public async Task Severity_OnAssertAndReport_IsRawText()
    {
        var schema = ParseValid(
            Schema2025(
                E(
                    "pattern",
                    2,
                    3,
                    E(
                        "rule",
                        3,
                        5,
                        A("context", "a"),
                        E("assert", 4, 7, A("test", "t"), A("severity", "high"), "m"),
                        E("report", 5, 7, A("test", "t"), A("severity", "$level"), "m")
                    )
                )
            )
        );

        var items = ((SchematronPattern)schema.Patterns[0]).Rules[0].Items;

        await Assert.That(((SchematronAssert)items[0]).Severity).IsEqualTo("high");
        await Assert.That(((SchematronReport)items[1]).Severity).IsEqualTo("$level");
    }

    [Test]
    public async Task VisitEach_OnRule_IsRawText()
    {
        var schema = ParseValid(
            Schema2025(E("pattern", 2, 3, E("rule", 3, 5, A("context", "a"), A("visit-each", "descendant::b[1]"))))
        );

        var rule = ((SchematronPattern)schema.Patterns[0]).Rules[0];

        await Assert.That(rule.VisitEach).IsEqualTo("descendant::b[1]");
        await Assert.That(rule.Context).IsEqualTo("a");
    }

    [Test]
    public async Task Phase_WhenAndFrom_AreRawText()
    {
        var schema = ParseValid(
            Schema2025(
                E("phase", 2, 3, A("id", "p1"), A("from", "ph0"), A("when", "$on = 'y'")),
                E("phase", 3, 3, A("id", "p2")),
                E("pattern", 4, 3)
            )
        );

        await Assert.That(schema.Phases[0].From).IsEqualTo("ph0");
        await Assert.That(schema.Phases[0].When).IsEqualTo("$on = 'y'");
        await Assert.That(schema.Phases[1].From).IsNull();
        await Assert.That(schema.Phases[1].When).IsNull();
    }

    [Test]
    public async Task SchemaVersion_IsReadOnSchemaNode()
    {
        var schema = ParseValid(Schema2025(A("schemaVersion", "2.1"), E("pattern", 2, 3)));

        await Assert.That(schema.SchemaVersion).IsEqualTo("2.1");
        await Assert.That(schema.Edition).IsEqualTo(SchematronEdition.Iso2025);
    }

    [Test]
    public async Task SchemaVersion_Absent_IsNull()
    {
        var schema = ParseValid(Schema2025(E("pattern", 2, 3)));

        await Assert.That(schema.SchemaVersion).IsNull();
    }

    [Test]
    public async Task Library_AllParts_AreModeled()
    {
        var (library, problems, result) = ParseLibrary(
            E(
                "library",
                1,
                1,
                E("title", 2, 3, "T"),
                E("p", 3, 3, "P"),
                E("param", 4, 3, A("name", "n"), A("value", "1")),
                E("let", 5, 3, A("name", "l"), A("value", "2")),
                E("pattern", 6, 3),
                E("group", 7, 3),
                E("rules", 8, 3, A("id", "rs"), E("rule", 9, 5, A("id", "ar"), A("abstract", "true"))),
                E("diagnostics", 10, 3, E("diagnostic", 11, 5, A("id", "d1"), "D")),
                E("properties", 12, 3, E("property", 13, 5, A("id", "pr1")))
            )
        );

        await Assert.That(result).IsTrue();
        await Assert.That(problems.Count).IsEqualTo(0);
        await Assert.That(library).IsNotNull();
        await Assert.That(library!.Title).IsNotNull();
        await Assert.That(library.Paragraphs.Count).IsEqualTo(1);
        await Assert.That(library.Parameters[0].Name).IsEqualTo("n");
        await Assert.That(library.Lets[0].Name).IsEqualTo("l");
        await Assert.That(library.Patterns.Count).IsEqualTo(2);
        await Assert.That(library.Patterns[0].IsGroup).IsFalse();
        await Assert.That(library.Patterns[1].IsGroup).IsTrue();
        await Assert.That(library.Rules[0].Id).IsEqualTo("rs");
        await Assert.That(library.Diagnostics!.Items[0].Id).IsEqualTo("d1");
        await Assert.That(library.Properties!.Items[0].Id).IsEqualTo("pr1");
        await AssertLocation(library, 1, 1);
        await AssertLocation(library.Parameters[0], 4, 3);
        await AssertLocation(library.Rules[0], 8, 3);
    }

    [Test]
    public async Task Library_WithoutParts_IsEmpty()
    {
        var (library, _, result) = ParseLibrary(E("library", 1, 1));

        await Assert.That(result).IsTrue();
        await Assert.That(library!.Patterns.Count).IsEqualTo(0);
        await Assert.That(library.Rules.Count).IsEqualTo(0);
        await Assert.That(library.ForeignElements.Count).IsEqualTo(0);
    }
}
