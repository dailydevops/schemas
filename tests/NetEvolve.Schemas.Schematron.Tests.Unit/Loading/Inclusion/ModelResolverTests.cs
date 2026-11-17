namespace NetEvolve.Schemas.Schematron.Tests.Unit.Loading.Inclusion;

using System.Collections.Generic;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Schematron.Loading.Inclusion;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Loading.Inclusion.InclusionFixture;

public class ModelResolverTests
{
    private static List<string> Ids(IReadOnlyList<SchematronPatternBase> patterns)
    {
        var ids = new List<string>();
        foreach (var pattern in patterns)
        {
            ids.Add(pattern.Id ?? "?");
        }

        return ids;
    }

    [Test]
    public async Task Include_TwoLevels_MergesIntoOneModel()
    {
        var files = new FakeResources()
            .Add("file:///s/lib/a.sch", Library(Include("b.sch") + Pattern("a-pattern")))
            .Add("file:///s/lib/b.sch", Pattern("b-pattern"));

        var outcome = Resolve(Schema(Include("lib/a.sch") + Pattern("own"), Edition2025), files);

        await Assert.That(outcome.Ok).IsTrue();
        await Assert.That(outcome.Problems.Count).IsEqualTo(0);
        await Assert.That(Ids(outcome.Schema.Patterns)).IsEquivalentTo(["b-pattern", "a-pattern", "own"]);
        await Assert.That(outcome.Schema.Patterns[0].Location.Source).IsEqualTo("file:///s/lib/b.sch");
        await Assert.That(outcome.Schema.Patterns[2].Location.Source).IsEqualTo(RootId);
        await Assert.That(outcome.Schema.Includes.Count).IsEqualTo(0);
        await Assert.That(files.Requests).IsEquivalentTo(["file:///s/lib/a.sch", "file:///s/lib/b.sch"]);
    }

    [Test]
    public async Task Include_PatternAtSchemaLevel_SplicesAtThePositionOfTheInclude()
    {
        var files = new FakeResources().Add("file:///s/x.sch", Pattern("px"));

        var outcome = Resolve(Schema(Pattern("p1") + Include("x.sch") + Pattern("p2")), files);

        await Assert.That(outcome.Ok).IsTrue();
        await Assert.That(Ids(outcome.Schema.Patterns)).IsEquivalentTo(["p1", "px", "p2"]);
    }

    [Test]
    public async Task Include_RuleInPattern_SplicesAtThePositionOfTheInclude()
    {
        var files = new FakeResources().Add("file:///s/r.sch", Rule("included", AssertEl("1")));

        var outcome = Resolve(
            Schema(Pattern("p", Rule("first", AssertEl("1")) + Include("r.sch") + Rule("last", AssertEl("1")))),
            files
        );

        var rules = outcome.Schema.Patterns[0].Rules;
        await Assert.That(outcome.Ok).IsTrue();
        await Assert.That(rules.Count).IsEqualTo(3);
        await Assert.That(rules[0].Context).IsEqualTo("first");
        await Assert.That(rules[1].Context).IsEqualTo("included");
        await Assert.That(rules[1].Location.Source).IsEqualTo("file:///s/r.sch");
        await Assert.That(rules[2].Context).IsEqualTo("last");
        await Assert.That(outcome.Schema.Patterns[0].Includes.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Include_AssertInRule_ReplacesTheIncludeInTheItems()
    {
        var files = new FakeResources().Add("file:///s/a.sch", AssertEl("included"));

        var outcome = Resolve(
            Schema(Pattern("p", Rule("c", AssertEl("before") + Include("a.sch") + AssertEl("after")))),
            files
        );

        var items = outcome.Schema.Patterns[0].Rules[0].Items;
        await Assert.That(outcome.Ok).IsTrue();
        await Assert.That(items.Count).IsEqualTo(3);
        await Assert.That(((SchematronAssert)items[0]).Test).IsEqualTo("before");
        await Assert.That(((SchematronAssert)items[1]).Test).IsEqualTo("included");
        await Assert.That(((SchematronAssert)items[2]).Test).IsEqualTo("after");
    }

    [Test]
    public async Task Include_Library_MergesEveryPartAtItsPosition()
    {
        const string library = """
            <library xmlns="http://purl.oclc.org/dsdl/schematron" xmlns:x="urn:x">
              <param name="pp"/>
              <let name="b" value="3"/>
              <rules><rule abstract="true" id="ar"><assert test="t"/></rule></rules>
              <diagnostics><diagnostic id="d1">x</diagnostic></diagnostics>
              <properties><property id="pr1">y</property></properties>
              <x:decl/>
              <pattern id="lp"/>
            </library>
            """;
        var files = new FakeResources().Add("file:///s/lib.sch", library);
        var root = Schema(
            "<let name=\"a\" value=\"1\"/>"
                + Include("lib.sch")
                + "<let name=\"z\" value=\"2\"/>"
                + "<pattern id=\"own\"/>"
                + "<diagnostics><diagnostic id=\"d0\">o</diagnostic></diagnostics>"
                + "<properties><property id=\"pr0\">o</property></properties>",
            Edition2025
        );

        var outcome = Resolve(root, files);

        var schema = outcome.Schema;
        await Assert.That(outcome.Ok).IsTrue();
        await Assert.That(Ids(schema.Patterns)).IsEquivalentTo(["lp", "own"]);
        await Assert.That(schema.Lets[0].Name).IsEqualTo("a");
        await Assert.That(schema.Lets[1].Name).IsEqualTo("b");
        await Assert.That(schema.Lets[2].Name).IsEqualTo("z");
        await Assert.That(schema.Parameters.Count).IsEqualTo(1);
        await Assert.That(schema.Parameters[0].Name).IsEqualTo("pp");
        await Assert.That(schema.Rules.Count).IsEqualTo(1);
        await Assert.That(schema.Rules[0].Rules[0].Id).IsEqualTo("ar");
        await Assert.That(schema.Diagnostics!.Items.Count).IsEqualTo(2);
        await Assert.That(schema.Diagnostics!.Location.Source).IsEqualTo(RootId);
        await Assert.That(schema.Properties!.Items.Count).IsEqualTo(2);
        await Assert.That(schema.ForeignElements.Count).IsEqualTo(1);
        await Assert.That(schema.ForeignElements[0].LocalName).IsEqualTo("decl");
    }

    [Test]
    public async Task Include_LibraryWithoutOwnDiagnostics_AddsTheLibraryDiagnostics()
    {
        var files = new FakeResources().Add(
            "file:///s/lib.sch",
            Library(
                "<diagnostics><diagnostic id=\"d1\">x</diagnostic></diagnostics><properties><property id=\"p1\">y</property></properties>"
            )
        );

        var outcome = Resolve(Schema(Include("lib.sch") + Pattern("p"), Edition2025), files);

        await Assert.That(outcome.Schema.Diagnostics!.Items.Count).IsEqualTo(1);
        await Assert.That(outcome.Schema.Diagnostics!.Location.Source).IsEqualTo("file:///s/lib.sch");
        await Assert.That(outcome.Schema.Properties!.Items.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Include_Library_IsReadUnderTheEditionOfTheIncludingSchema()
    {
        var files = new FakeResources().Add("file:///s/lib.sch", Library(Pattern("p")));

        var outcome = Resolve(Schema(Include("lib.sch") + Pattern("own")), files);

        await Assert.That(outcome.Ok).IsFalse();
        await Assert.That(outcome.Problems.Count).IsEqualTo(1);
        await Assert.That(outcome.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.EditionMismatch);
        await Assert.That(outcome.Problems[0].Location.Source).IsEqualTo("file:///s/lib.sch");
    }

    [Test]
    public async Task Include_GroupRoot_In2025Schema_IsMerged()
    {
        var files = new FakeResources().Add("file:///s/g.sch", El("group", " id=\"g1\"", string.Empty));

        var outcome = Resolve(Schema(Include("g.sch"), Edition2025), files);

        await Assert.That(outcome.Ok).IsTrue();
        await Assert.That(outcome.Schema.Patterns[0].IsGroup).IsTrue();
    }

    [Test]
    public async Task Include_GroupRoot_In2016Schema_ReportsEditionMismatch()
    {
        var files = new FakeResources().Add("file:///s/g.sch", El("group", " id=\"g1\"", string.Empty));

        var outcome = Resolve(Schema(Include("g.sch")), files);

        await Assert.That(outcome.Ok).IsFalse();
        await Assert.That(outcome.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.EditionMismatch);
    }

    [Test]
    public async Task Include_GroupWithRuleInclude_ResolvesTheRuleInTheGroup()
    {
        var files = new FakeResources()
            .Add("file:///s/g.sch", El("group", " id=\"g1\"", Include("r.sch")))
            .Add("file:///s/r.sch", Rule("rc", AssertEl("1")));

        var outcome = Resolve(Schema(Include("g.sch"), Edition2025), files);

        await Assert.That(outcome.Ok).IsTrue();
        await Assert.That(outcome.Schema.Patterns[0].IsGroup).IsTrue();
        await Assert.That(outcome.Schema.Patterns[0].Rules[0].Context).IsEqualTo("rc");
    }

    [Test]
    public async Task Include_NestedInsideRulesContainerAndAbstractRule_IsResolved()
    {
        var files = new FakeResources().Add("file:///s/a.sch", AssertEl("viaInclude"));
        var root = Schema(
            "<rules><rule abstract=\"true\" id=\"ar\">" + Include("a.sch") + "</rule></rules>" + Pattern("p"),
            Edition2025
        );

        var outcome = Resolve(root, files);

        await Assert.That(outcome.Ok).IsTrue();
        await Assert.That(outcome.Schema.Rules[0].Rules[0].Items.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Extends_Href_MergesAtRulePosition()
    {
        const string library = """
            <library xmlns="http://purl.oclc.org/dsdl/schematron">
              <rules>
                <rule abstract="true" id="r1">
                  <let name="v" value="1"/>
                  <assert test="fromLibrary"/>
                </rule>
              </rules>
            </library>
            """;
        var files = new FakeResources().Add("file:///s/lib.sch", library);
        var root = Schema(
            Pattern(
                "p",
                Rule("x", AssertEl("before") + "<extends href=\"lib.sch\"/>" + "<report test=\"after\">r</report>")
            ),
            Edition2025
        );

        var outcome = Resolve(root, files);

        var rule = outcome.Schema.Patterns[0].Rules[0];
        await Assert.That(outcome.Ok).IsTrue();
        await Assert.That(rule.Items.Count).IsEqualTo(3);
        await Assert.That(((SchematronAssert)rule.Items[0]).Test).IsEqualTo("before");
        await Assert.That(((SchematronAssert)rule.Items[1]).Test).IsEqualTo("fromLibrary");
        await Assert.That(rule.Items[1].Location.Source).IsEqualTo("file:///s/lib.sch");
        await Assert.That(rule.Items[2]).IsTypeOf<SchematronReport>();
        await Assert.That(rule.Lets.Count).IsEqualTo(1);
        await Assert.That(rule.Lets[0].Name).IsEqualTo("v");
    }

    [Test]
    public async Task Extends_Href_ToAbstractRuleInPatternOfTheLibrary_IsMerged()
    {
        var files = new FakeResources().Add(
            "file:///s/lib.sch",
            Library(Pattern("lp", "<rule abstract=\"true\" id=\"ar\">" + AssertEl("fromPattern") + "</rule>"))
        );
        var root = Schema(Pattern("p", Rule("x", "<extends href=\"lib.sch\"/>" + AssertEl("own"))), Edition2025);

        var outcome = Resolve(root, files);

        var items = outcome.Schema.Patterns[0].Rules[0].Items;
        await Assert.That(outcome.Ok).IsTrue();
        await Assert.That(((SchematronAssert)items[0]).Test).IsEqualTo("fromPattern");
        await Assert.That(((SchematronAssert)items[1]).Test).IsEqualTo("own");
    }

    [Test]
    public async Task Extends_Href_TargetIsNotALibrary_ReportsModuleInvalidAtTheExtends()
    {
        var files = new FakeResources().Add("file:///s/p.sch", Pattern("p"));
        var root = Schema(Pattern("p", Rule("x", "\n<extends href=\"p.sch\"/>")), Edition2025);

        var outcome = Resolve(root, files);

        await Assert.That(outcome.Ok).IsFalse();
        await Assert.That(outcome.Problems.Count).IsEqualTo(1);
        await Assert.That(outcome.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.ModuleInvalid);
        await Assert.That(outcome.Problems[0].Location.Source).IsEqualTo(RootId);
        await Assert.That(outcome.Problems[0].Location.Line).IsGreaterThan(0);
    }

    [Test]
    [Arguments("<rules/>")]
    [Arguments(
        "<rules><rule abstract=\"true\" id=\"a\"><assert test=\"1\"/></rule><rule abstract=\"true\" id=\"b\"><assert test=\"1\"/></rule></rules>"
    )]
    public async Task Extends_Href_LibraryWithoutExactlyOneAbstractRule_ReportsModuleInvalid(string content)
    {
        var files = new FakeResources().Add("file:///s/lib.sch", Library(content));
        var root = Schema(Pattern("p", Rule("x", "<extends href=\"lib.sch\"/>")), Edition2025);

        var outcome = Resolve(root, files);

        await Assert.That(outcome.Ok).IsFalse();
        await Assert.That(outcome.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.ModuleInvalid);
    }

    [Test]
    public async Task Extends_Href_Cycle_ReportsAbstractTargetInvalid()
    {
        var files = new FakeResources().Add(
            "file:///s/lib.sch",
            Library("<rules><rule abstract=\"true\" id=\"a\"><extends href=\"lib.sch\"/></rule></rules>")
        );
        var root = Schema(Pattern("p", Rule("x", "<extends href=\"lib.sch\"/>")), Edition2025);

        var outcome = Resolve(root, files);

        await Assert.That(outcome.Ok).IsFalse();
        await Assert.That(outcome.Problems.Count).IsEqualTo(1);
        await Assert.That(outcome.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.AbstractTargetInvalid);
        await Assert.That(outcome.Problems[0].Location.Source).IsEqualTo("file:///s/lib.sch");
    }

    [Test]
    public async Task Extends_ByRule_IsPassedThroughUnchanged()
    {
        var root = Schema(Pattern("p", Rule("x", "<extends rule=\"r1\"/>")));

        var outcome = Resolve(root, new FakeResources());

        await Assert.That(outcome.Ok).IsTrue();
        await Assert.That(outcome.Schema.Patterns[0].Rules[0].Items[0]).IsTypeOf<SchematronExtends>();
    }

    [Test]
    public async Task Include_Cycle_ReportsIncludeCycleWithLocation()
    {
        var files = new FakeResources()
            .Add("file:///s/a.sch", Library(Include("b.sch")))
            .Add("file:///s/b.sch", Library("\n  " + Include("a.sch")));

        var outcome = Resolve(Schema(Include("a.sch"), Edition2025), files);

        await Assert.That(outcome.Ok).IsFalse();
        await Assert.That(outcome.Problems.Count).IsEqualTo(1);
        var problem = outcome.Problems[0];
        await Assert.That(problem.Code).IsEqualTo(SchematronDiagnosticCodes.IncludeCycle);
        await Assert.That(problem.Location.Source).IsEqualTo("file:///s/b.sch");
        await Assert.That(problem.Location.Line).IsEqualTo(2);
        await Assert.That(problem.Message).Contains("a.sch");
    }

    [Test]
    public async Task Include_OfThePrincipalSchemaItself_ReportsIncludeCycle()
    {
        var files = new FakeResources().Add(RootId, Schema(Include("root.sch")));

        var outcome = Resolve(Schema(Include("root.sch")), files);

        await Assert.That(outcome.Ok).IsFalse();
        await Assert.That(outcome.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.IncludeCycle);
        await Assert.That(outcome.Problems[0].Location.Source).IsEqualTo(RootId);
    }

    [Test]
    public async Task Include_SameDocumentTwice_IsNoCycle()
    {
        var files = new FakeResources().Add("file:///s/x.sch", Pattern("px"));

        var outcome = Resolve(Schema(Include("x.sch") + Include("x.sch")), files);

        await Assert.That(outcome.Ok).IsTrue();
        await Assert.That(outcome.Schema.Patterns.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Include_Missing_ReportsNes0102AtReference()
    {
        var root = Schema("\n" + Include("missing.sch"));

        var outcome = Resolve(root, new FakeResources());

        await Assert.That(outcome.Ok).IsFalse();
        await Assert.That(outcome.Problems.Count).IsEqualTo(1);
        var problem = outcome.Problems[0];
        await Assert.That(problem.Code).IsEqualTo(CoreDiagnosticCodes.ResourceNotFound);
        await Assert.That(problem.Location.Source).IsEqualTo(RootId);
        await Assert.That(problem.Location.Line).IsEqualTo(2);
    }

    [Test]
    public async Task Include_DefaultResolver_DeniesWithoutLoad()
    {
        var counting = new CountingResolver(DenyAllResourceResolver.Instance);

        var outcome = Resolve(Schema("\n" + Include("x.sch")), counting);

        await Assert.That(outcome.Ok).IsFalse();
        await Assert.That(outcome.Problems.Count).IsEqualTo(1);
        await Assert.That(outcome.Problems[0].Code).IsEqualTo(CoreDiagnosticCodes.ResourceDenied);
        await Assert.That(outcome.Problems[0].Location.Line).IsEqualTo(2);
        await Assert.That(counting.Calls).IsEqualTo(1);
        await Assert.That(counting.Found).IsEqualTo(0);
    }

    [Test]
    public async Task Include_NestingBeyondLimit_ReportsLimitCode()
    {
        var files = Chain(17);

        var outcome = Resolve(Schema(Include("n1.sch"), Edition2025), files);

        await Assert.That(outcome.Ok).IsFalse();
        await Assert.That(outcome.Problems.Count).IsEqualTo(1);
        await Assert.That(outcome.Problems[0].Code).IsEqualTo(CoreDiagnosticCodes.ExternalResourceLimitExceeded);
        await Assert.That(outcome.Problems[0].Location.Source).IsEqualTo("file:///s/n16.sch");
        await Assert.That(files.Requests.Count).IsEqualTo(16);
    }

    [Test]
    public async Task Include_NestingAtLimit_IsResolved() =>
        await Assert.That(Resolve(Schema(Include("n1.sch"), Edition2025), Chain(16)).Ok).IsTrue();

    [Test]
    public async Task Include_NestingBeyondRaisedLimit_ReportsLimitCode()
    {
        var limits = ValidationLimits.Default.WithMaxExternalResources(40);

        var beyond = Resolve(Schema(Include("n1.sch"), Edition2025), Chain(41), limits);
        var at = Resolve(Schema(Include("n1.sch"), Edition2025), Chain(40), limits);

        await Assert.That(beyond.Ok).IsFalse();
        await Assert.That(beyond.Problems[0].Code).IsEqualTo(CoreDiagnosticCodes.ExternalResourceLimitExceeded);
        await Assert.That(at.Ok).IsTrue();
    }

    [Test]
    public async Task Include_DeepChain_DoesNotOverflowTheStack()
    {
        var limits = ValidationLimits.Default.WithMaxExternalResources(5_000);

        var outcome = Resolve(Schema(Include("n1.sch"), Edition2025), Chain(2_000), limits);

        await Assert.That(outcome.Ok).IsTrue();
        await Assert.That(Ids(outcome.Schema.Patterns)).IsEquivalentTo(["leaf"]);
    }

    [Test]
    public async Task Include_ExceedsMaxExternalResources_ReportsNes0006()
    {
        var files = new FakeResources()
            .Add("file:///s/a.sch", Pattern("a"))
            .Add("file:///s/b.sch", Pattern("b"))
            .Add("file:///s/c.sch", Pattern("c"));
        var limits = ValidationLimits.Default.WithMaxExternalResources(2);

        var outcome = Resolve(Schema(Include("a.sch") + Include("b.sch") + "\n" + Include("c.sch")), files, limits);

        await Assert.That(outcome.Ok).IsFalse();
        await Assert.That(outcome.Problems.Count).IsEqualTo(1);
        await Assert.That(outcome.Problems[0].Code).IsEqualTo(CoreDiagnosticCodes.ExternalResourceLimitExceeded);
        await Assert.That(outcome.Problems[0].Location.Line).IsEqualTo(2);
    }

    [Test]
    public async Task Include_DiamondFanOut_CountsEveryOccurrence()
    {
        var files = new FakeResources();
        for (var k = 1; k < 5; k++)
        {
            files.Add($"file:///s/d{k}.sch", Library(Include($"d{k + 1}.sch") + Include($"d{k + 1}.sch")));
        }

        files.Add("file:///s/d5.sch", Library(Pattern("leaf")));
        var root = Schema(Include("d1.sch"), Edition2025);

        var refused = Resolve(root, files);
        var exact = Resolve(root, files, ValidationLimits.Default.WithMaxExternalResources(31));
        var short1 = Resolve(root, files, ValidationLimits.Default.WithMaxExternalResources(30));

        await Assert.That(refused.Ok).IsFalse();
        await Assert.That(refused.Problems.Count).IsEqualTo(1);
        await Assert.That(refused.Problems[0].Code).IsEqualTo(CoreDiagnosticCodes.ExternalResourceLimitExceeded);
        await Assert.That(exact.Ok).IsTrue();
        await Assert.That(exact.Schema.Patterns.Count).IsEqualTo(16);
        await Assert.That(short1.Ok).IsFalse();
    }

    [Test]
    public async Task Include_RelativeReferenceInIncludedFile_ResolvesAgainstThatFilesBaseUri()
    {
        var files = new FakeResources()
            .Add("file:///a/sub/lib.sch", Library(Include("p.sch")))
            .Add("file:///a/sub/p.sch", Pattern("deep"));

        var outcome = Resolve(Schema(Include("sub/lib.sch"), Edition2025), files, systemId: "file:///a/root.sch");

        await Assert.That(outcome.Ok).IsTrue();
        await Assert.That(files.Requests).IsEquivalentTo(["file:///a/sub/lib.sch", "file:///a/sub/p.sch"]);
        await Assert.That(outcome.Schema.Patterns[0].Location.Source).IsEqualTo("file:///a/sub/p.sch");
    }

    [Test]
    public async Task Loader_DoesNotChangeBaseUri_OfIncludedContent()
    {
        var library =
            $"<library xmlns=\"{Ns}\" xmlns:xml=\"http://www.w3.org/XML/1998/namespace\" xml:base=\"http://other.example/x/\">"
            + Include("p.sch")
            + "</library>";
        var files = new FakeResources().Add("file:///s/lib.sch", library).Add("file:///s/p.sch", Pattern("deep"));

        var outcome = Resolve(Schema(Include("lib.sch"), Edition2025), files);

        await Assert.That(outcome.Ok).IsTrue();
        await Assert.That(files.Requests).IsEquivalentTo(["file:///s/lib.sch", "file:///s/p.sch"]);
    }

    [Test]
    public async Task Include_RelativeReferenceWithoutAbsoluteBase_ReportsNes0101()
    {
        var outcome = Resolve(Schema(Include("x.sch")), new FakeResources(), systemId: null);

        await Assert.That(outcome.Problems[0].Code).IsEqualTo(CoreDiagnosticCodes.ResourceBaseUriMissing);
    }

    [Test]
    public async Task Include_InvalidReference_ReportsNes0103()
    {
        var outcome = Resolve(Schema(Include("%zz")), new FakeResources());

        await Assert.That(outcome.Problems[0].Code).IsEqualTo(CoreDiagnosticCodes.ResourceReferenceInvalid);
    }

    [Test]
    public async Task Include_FragmentIdentifier_ReportsUnsupportedWithoutLoading()
    {
        var files = new FakeResources().Add("file:///s/x.sch", Pattern("px"));

        var outcome = Resolve(Schema(Include("x.sch#px")), files);

        await Assert.That(outcome.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.Unsupported);
        await Assert.That(files.Requests.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Include_RootAllowedByTheGrammarButNotRead_ReportsUnsupported()
    {
        var files = new FakeResources().Add("file:///s/ph.sch", El("phase", " id=\"ph\"", string.Empty));

        var outcome = Resolve(Schema(Include("ph.sch")), files);

        await Assert.That(outcome.Problems.Count).IsEqualTo(1);
        await Assert.That(outcome.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.Unsupported);
        await Assert.That(outcome.Problems[0].Message).Contains("phase");
    }

    [Test]
    [Arguments("<foo xmlns=\"urn:x\"/>")]
    [Arguments("<rule xmlns=\"http://purl.oclc.org/dsdl/schematron\" context=\"c\"/>")]
    [Arguments("<schema xmlns=\"http://purl.oclc.org/dsdl/schematron\"/>")]
    public async Task Include_RootNotAllowedByTheGrammar_ReportsModuleInvalid(string content)
    {
        var files = new FakeResources().Add("file:///s/x.sch", content);

        var outcome = Resolve(Schema(Include("x.sch")), files);

        await Assert.That(outcome.Problems.Count).IsEqualTo(1);
        await Assert.That(outcome.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.ModuleInvalid);
    }

    [Test]
    public async Task Include_MalformedDocument_ReportsModuleInvalidAtTheReference()
    {
        var files = new FakeResources().Add("file:///s/x.sch", "<library");

        var outcome = Resolve(Schema("\n" + Include("x.sch")), files);

        await Assert.That(outcome.Problems.Count).IsEqualTo(1);
        await Assert.That(outcome.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.ModuleInvalid);
        await Assert.That(outcome.Problems[0].Message).Contains("not well formed");
        await Assert.That(outcome.Problems[0].Location.Line).IsEqualTo(2);
    }

    [Test]
    public async Task Include_ProblemInsideTheModule_IsReportedAtTheLocationInTheModule()
    {
        var files = new FakeResources().Add("file:///s/x.sch", El("pattern", string.Empty, "<assert test=\"1\"/>"));

        var outcome = Resolve(Schema(Include("x.sch")), files);

        await Assert.That(outcome.Ok).IsFalse();
        await Assert.That(outcome.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.ElementMisplaced);
        await Assert.That(outcome.Problems[0].Location.Source).IsEqualTo("file:///s/x.sch");
    }

    [Test]
    public async Task Include_DocumentWithoutRootElement_IsReported()
    {
        var files = new FakeResources().Add("file:///s/x.sch", "<!-- nothing -->");

        var outcome = Resolve(Schema(Include("x.sch")), files);

        await Assert.That(outcome.Ok).IsFalse();
    }

    [Test]
    [Arguments(false, false)]
    [Arguments(true, false)]
    [Arguments(true, true)]
    public async Task Include_ResourceOverTheSizeLimit_ReportsNes0007(bool asStream, bool forwardOnly)
    {
        var files = new FakeResources { AsStream = asStream, ForwardOnly = forwardOnly }.Add(
            "file:///s/x.sch",
            Pattern("px")
        );
        var limits = ValidationLimits.Default.WithMaxResourceSize(10);

        var outcome = Resolve(Schema(Include("x.sch")), files, limits);

        await Assert.That(outcome.Problems[0].Code).IsEqualTo(CoreDiagnosticCodes.ResourceSizeLimitExceeded);
    }

    [Test]
    public async Task Include_ResourceAsStream_IsRead()
    {
        var files = new FakeResources { AsStream = true }.Add("file:///s/x.sch", Pattern("px"));

        var outcome = Resolve(Schema(Include("x.sch")), files);

        await Assert.That(outcome.Ok).IsTrue();
        await Assert.That(Ids(outcome.Schema.Patterns)).IsEquivalentTo(["px"]);
    }

    [Test]
    public async Task Resolve_Success_LeavesNoIncludeOrHrefExtendsBehind()
    {
        var files = new FakeResources()
            .Add("file:///s/p.sch", Pattern("px", Include("r.sch")))
            .Add("file:///s/r.sch", Rule("rc", Include("a.sch")))
            .Add("file:///s/a.sch", AssertEl("deep"));

        var outcome = Resolve(Schema(Include("p.sch") + Pattern("own", Rule("c", Include("a.sch")))), files);

        await Assert.That(outcome.Ok).IsTrue();
        await Assert.That(outcome.Schema.Includes.Count).IsEqualTo(0);
        foreach (var pattern in outcome.Schema.Patterns)
        {
            await Assert.That(pattern.Includes.Count).IsEqualTo(0);
            foreach (var rule in pattern.Rules)
            {
                foreach (var item in rule.Items)
                {
                    await Assert.That(item).IsNotTypeOf<SchematronInclude>();
                }
            }
        }
    }

    [Test]
    public async Task Resolve_WithoutReferences_ReturnsAnEquivalentSchema()
    {
        var outcome = Resolve(Schema(Pattern("p", Rule("c", AssertEl("1"))), " id=\"s\""), new FakeResources());

        await Assert.That(outcome.Ok).IsTrue();
        await Assert.That(outcome.Schema.Id).IsEqualTo("s");
        await Assert.That(outcome.Schema.Patterns[0].Rules[0]).IsNotNull();
    }

    [Test]
    public async Task TryResolve_NullArguments_Throw()
    {
        var parsed = Parse(Schema(Pattern("p")));
        var loader = new FakeModuleLoader();
        var sink = new FakeProblemSink();
        var resolver = new ModelResolver();

        await Assert.That(() => resolver.TryResolve(null!, loader, sink, out _)).Throws<ArgumentNullException>();
        await Assert.That(() => resolver.TryResolve(parsed, null!, sink, out _)).Throws<ArgumentNullException>();
        await Assert.That(() => resolver.TryResolve(parsed, loader, null!, out _)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task TryResolve_OnFailure_ReturnsTheUnresolvedSchema()
    {
        var parsed = Parse(Schema(Include("missing.sch")));
        var loader = new ModuleLoader(new FakeResources(), ValidationLimits.Default);

        var ok = new ModelResolver().TryResolve(parsed, loader, new FakeProblemSink(), out var model);

        await Assert.That(ok).IsFalse();
        await Assert.That(model.Schema).IsSameReferenceAs(parsed.Schema);
    }

    private static FakeResources Chain(int count)
    {
        var files = new FakeResources();
        for (var k = 1; k < count; k++)
        {
            files.Add($"file:///s/n{k}.sch", Library(Include($"n{k + 1}.sch")));
        }

        files.Add($"file:///s/n{count}.sch", Library(Pattern("leaf")));
        return files;
    }

    private sealed class CountingResolver(IResourceResolver inner) : IResourceResolver
    {
        internal int Calls { get; private set; }

        internal int Found { get; private set; }

        public ResourceResult Resolve(in ResourceRequest request, System.Threading.CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            var result = inner.Resolve(in request, cancellationToken);
            if (result.Status == ResourceStatus.Found)
            {
                Found++;
            }

            return result;
        }
    }
}
