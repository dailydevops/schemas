namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Modules;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Resources.Default;
using NetEvolve.Schemas.Schematron.Loading.Inclusion;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using NetEvolve.Schemas.Schematron.Tests.Unit.Loading.Inclusion;
using NetEvolve.Schemas.Schematron.Xslt.Modules;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Modules.ModuleHarness;

/// <summary>
/// The import-tree linker (XSLT 2.0 section 3.10, design #150 EP-14): the loading of modules through the core resolver, the
/// resolution of references against the containing module, cycles by the identity the resolver returns, the cache, the placement
/// of <c>xsl:import</c>, and the iteration without recursion.
/// </summary>
public class ModuleLinkerTests
{
    [Test]
    public async Task Link_WithoutReferences_ReturnsThePrincipalDeclarationsInOrderAtTheFirstRank(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var harness = new ModuleHarness();

        var ok = harness.Link(Sheet(Decl("a"), Decl("b")));

        await Assert.That(ok).IsTrue();
        await Assert.That(Describe(harness.Set)).IsEqualTo("a@1,b@1");
        await Assert.That(harness.Files.Requests.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Link_Import_LoadsTheModuleThroughTheResolverAtALowerRank(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var harness = new ModuleHarness();
        _ = harness.Files.Add("file:///m/lib.xsl", Sheet(Decl("l")));

        var ok = harness.Link(Sheet(Import("lib.xsl"), Decl("p")));

        await Assert.That(ok).IsTrue();
        await Assert.That(Describe(harness.Set)).IsEqualTo("l@1,p@2");
        await Assert.That(harness.Files.Requests).IsEquivalentTo(["file:///m/lib.xsl"]);
        await Assert.That(harness.Set.Declarations[0].SystemId).IsEqualTo("file:///m/lib.xsl");
        await Assert.That(harness.Set.Declarations[1].SystemId).IsEqualTo(Principal);
    }

    [Test]
    public async Task Precedence_Include_SharesTheRankOfTheIncluderAndSplicesAtTheIncludePosition(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var harness = new ModuleHarness();
        _ = harness.Files.Add("file:///m/x.xsl", Sheet(Decl("x1"), Decl("x2")));

        var ok = harness.Link(Sheet(Decl("p1"), Include("x.xsl"), Decl("p2")));

        await Assert.That(ok).IsTrue();
        await Assert.That(Describe(harness.Set)).IsEqualTo("p1@1,x1@1,x2@1,p2@1");
    }

    [Test]
    public async Task Precedence_WorkedTree_RanksAre_C1_A2_B3_P4(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var harness = new ModuleHarness();
        _ = harness
            .Files.Add("file:///m/a.xsl", Sheet(Import("c.xsl"), Decl("af"), Decl("ag")))
            .Add("file:///m/b.xsl", Sheet(Decl("bg")))
            .Add("file:///m/c.xsl", Sheet(Decl("cf")))
            .Add("file:///m/d.xsl", Sheet(Decl("dh")));

        var ok = harness.Link(Sheet(Import("a.xsl"), Import("b.xsl"), Include("d.xsl"), Decl("pf")));

        await Assert.That(ok).IsTrue();
        await Assert.That(Describe(harness.Set)).IsEqualTo("cf@1,af@2,ag@2,bg@3,dh@4,pf@4");
    }

    [Test]
    public async Task Precedence_ImportInsideIncludedModule_BecomesImportOfTheIncludingLevel(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var harness = new ModuleHarness();
        _ = harness
            .Files.Add("file:///m/x.xsl", Sheet(Import("y.xsl"), Decl("x1")))
            .Add("file:///m/y.xsl", Sheet(Decl("y1")));

        var ok = harness.Link(Sheet(Include("x.xsl"), Decl("p")));

        await Assert.That(ok).IsTrue();
        await Assert.That(Describe(harness.Set)).IsEqualTo("y1@1,x1@2,p@2");
    }

    [Test]
    public async Task Precedence_Diamond_LoadsTheSharedModuleOnceAndPlacesItTwice(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var harness = new ModuleHarness();
        _ = harness
            .Files.Add("file:///m/a.xsl", Sheet(Import("c.xsl"), Decl("af")))
            .Add("file:///m/b.xsl", Sheet(Import("c.xsl"), Decl("bf")))
            .Add("file:///m/c.xsl", Sheet(Decl("cf")));

        var ok = harness.Link(Sheet(Import("a.xsl"), Import("b.xsl")));

        await Assert.That(ok).IsTrue();
        await Assert.That(Describe(harness.Set)).IsEqualTo("cf@1,af@2,cf@3,bf@4");
        await Assert.That(harness.Files.Requests.Count).IsEqualTo(3);
        await Assert.That(harness.Problems.Problems.Count).IsEqualTo(0);
        await Assert.That(harness.Set.Declarations[0].Element).IsEqualTo(harness.Set.Declarations[2].Element);
    }

    [Test]
    public async Task Link_AliasThatTheResolverCanonicalizes_IsOneModule(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolver = new AliasResolver("file:///m/alias.xsl", "file:///m/real.xsl");
        var harness = new ModuleHarness(resolver);
        _ = resolver.Inner.Add("file:///m/real.xsl", Sheet(Decl("r")));

        var ok = harness.Link(Sheet(Import("real.xsl"), Import("alias.xsl")));

        await Assert.That(ok).IsTrue();
        await Assert.That(Describe(harness.Set)).IsEqualTo("r@1,r@2");
        await Assert.That(harness.Set.Declarations[0].Element).IsEqualTo(harness.Set.Declarations[1].Element);
    }

    [Test]
    [Arguments("main.xsl")]
    [Arguments("./main.xsl")]
    public async Task Import_OfThePrincipalModule_IsACycleWithTheCodedErrorAndTheModuleIsNotLoadedAgain(
        string href,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var harness = new ModuleHarness();
        _ = harness.Files.Add("file:///m/b.xsl", Sheet(Import(href)));

        var ok = harness.Link(Sheet(Import("b.xsl")));

        await Assert.That(ok).IsFalse();
        var problem = harness.Problems.Problems[0];
        await Assert.That(problem.Code).IsEqualTo(SchematronDiagnosticCodes.IncludeCycle);
        await Assert.That(problem.Constraint).IsEqualTo("XTSE0210");
        await Assert.That(problem.Location.Source).IsEqualTo("file:///m/b.xsl");
        await Assert.That(problem.Location.Line).IsEqualTo(2);
        await Assert.That(problem.Message).Contains(Principal);
        await Assert.That(harness.Files.Requests).IsEquivalentTo(["file:///m/b.xsl"]);
    }

    [Test]
    public async Task Include_Cycle_IsXtse0180AndTheModuleIsNotLoadedAgain(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var harness = new ModuleHarness();
        _ = harness
            .Files.Add("file:///m/a.xsl", Sheet(Include("b.xsl")))
            .Add("file:///m/b.xsl", Sheet(Decl("b"), Include("a.xsl")));

        var ok = harness.Link(Sheet(Include("a.xsl")));

        await Assert.That(ok).IsFalse();
        await Assert.That(Codes(harness.Problems)).IsEqualTo("NES3100/XTSE0180");
        await Assert.That(harness.Problems.Problems[0].Location.Source).IsEqualTo("file:///m/b.xsl");
        await Assert.That(harness.Problems.Problems[0].Location.Line).IsEqualTo(3);
        await Assert.That(harness.Files.Requests.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Import_ModuleThatImportsItself_IsACycle(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var harness = new ModuleHarness();
        _ = harness.Files.Add("file:///m/a.xsl", Sheet(Import("a.xsl")));

        var ok = harness.Link(Sheet(Import("a.xsl")));

        await Assert.That(ok).IsFalse();
        await Assert.That(Codes(harness.Problems)).IsEqualTo("NES3100/XTSE0210");
        await Assert.That(harness.Files.Requests.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Import_Aliases_AreOneModuleAndACycleThroughAnAliasIsTheCodedErrorNotTheResourceLimit(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var limits = ValidationLimits.Default.WithMaxExternalResources(2);
        var harness = new ModuleHarness(limits: limits);
        _ = harness
            .Files.Add("file:///m/a.xsl", Sheet(Import("./a.xsl")))
            .Add("file:///m/sub/b.xsl", Sheet(Import("../sub/./b.xsl")));

        var viaDot = harness.Link(Sheet(Import("a.xsl")));
        var second = new ModuleHarness(limits: limits);
        _ = second.Files.Add("file:///m/sub/b.xsl", Sheet(Import("../sub/./b.xsl")));
        var viaParent = second.Link(Sheet(Import("sub/../sub/b.xsl")));

        await Assert.That(viaDot).IsFalse();
        await Assert.That(Codes(harness.Problems)).IsEqualTo("NES3100/XTSE0210");
        await Assert.That(viaParent).IsFalse();
        await Assert.That(Codes(second.Problems)).IsEqualTo("NES3100/XTSE0210");
    }

    [Test]
    public async Task Import_DefaultResolver_RefusesAndReportsTheCoreCodeAtTheImportElement(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var harness = new ModuleHarness(DenyAllResourceResolver.Instance);

        var ok = harness.Link(Sheet(Decl("p"), "<!-- -->", Include("lib.xsl")));

        await Assert.That(ok).IsFalse();
        var problem = harness.Problems.Problems[0];
        await Assert.That(problem.Code).IsEqualTo(CoreDiagnosticCodes.ResourceDenied);
        await Assert.That(problem.Location.Source).IsEqualTo(Principal);
        await Assert.That(problem.Location.Line).IsEqualTo(4);
        await Assert.That(problem.Location.Column).IsGreaterThan(0);
        await Assert.That(Describe(harness.Set)).IsEqualTo("p@1");
    }

    [Test]
    public async Task Import_MissingModule_ReportsNotFoundAtTheImportElement(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var harness = new ModuleHarness();

        var ok = harness.Link(Sheet(Import("none.xsl")));

        await Assert.That(ok).IsFalse();
        await Assert.That(harness.Problems.Problems[0].Code).IsEqualTo(CoreDiagnosticCodes.ResourceNotFound);
        await Assert.That(harness.Problems.Problems[0].Location.Line).IsEqualTo(2);
    }

    [Test]
    public async Task Import_InvalidReference_IsReportedByTheLoaderWithoutTheCache(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var harness = new ModuleHarness();

        var ok = harness.Link(Sheet(Import("%zz")));

        await Assert.That(ok).IsFalse();
        await Assert.That(harness.Problems.Problems[0].Code).IsEqualTo(CoreDiagnosticCodes.ResourceReferenceInvalid);
    }

    [Test]
    public async Task Import_RootIsNotStylesheetOrTransform_IsModuleInvalidAtTheImportElement(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var harness = new ModuleHarness();
        _ = harness.Files.Add("file:///m/lib.xsl", "<doc xmlns=\"urn:x\"/>");

        var ok = harness.Link(Sheet(Import("lib.xsl")));

        await Assert.That(ok).IsFalse();
        var problem = harness.Problems.Problems[0];
        await Assert.That(problem.Code).IsEqualTo(SchematronDiagnosticCodes.ModuleInvalid);
        await Assert.That(problem.Message).Contains("file:///m/lib.xsl");
        await Assert.That(problem.Location.Line).IsEqualTo(2);
    }

    [Test]
    public async Task Import_TransformRoot_IsAStylesheetModule(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var harness = new ModuleHarness();
        _ = harness.Files.Add(
            "file:///m/lib.xsl",
            $"<!-- c --><xsl:transform xmlns:xsl=\"{Xslt}\" version=\"2.0\">{Decl("t")}</xsl:transform>"
        );

        var ok = harness.Link(Sheet(Import("lib.xsl")));

        await Assert.That(ok).IsTrue();
        await Assert.That(Describe(harness.Set)).IsEqualTo("t@1");
    }

    [Test]
    public async Task Import_WithoutHref_IsAMissingAttributeAtTheElement(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var harness = new ModuleHarness();

        var ok = harness.Link(Sheet("<xsl:import/>", "<xsl:include/>"));

        await Assert.That(ok).IsFalse();
        await Assert.That(Codes(harness.Problems)).IsEqualTo("NES3005/XTSE0010,NES3005/XTSE0010");
        await Assert.That(harness.Problems.Problems[0].Message).Contains("xsl:import");
        await Assert.That(harness.Problems.Problems[1].Message).Contains("xsl:include");
    }

    [Test]
    public async Task Precedence_ImportAfterADeclarationInAModuleRoot_IsMisplacedXtse0200(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var harness = new ModuleHarness();
        _ = harness
            .Files.Add("file:///m/lib.xsl", Sheet(Decl("l"), Import("other.xsl")))
            .Add("file:///m/other.xsl", Sheet(Decl("o")));

        var ok = harness.Link(Sheet(Import("lib.xsl")));

        await Assert.That(ok).IsFalse();
        var problem = harness.Problems.Problems[0];
        await Assert.That(Codes(harness.Problems)).IsEqualTo("NES3003/XTSE0200");
        await Assert.That(problem.Location.Source).IsEqualTo("file:///m/lib.xsl");
        await Assert.That(problem.Location.Line).IsEqualTo(3);
    }

    [Test]
    public async Task Precedence_ImportAfterAnIncludeInAModuleRoot_IsMisplacedXtse0200(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var harness = new ModuleHarness();
        _ = harness.Files.Add("file:///m/x.xsl", Sheet(Decl("x"))).Add("file:///m/y.xsl", Sheet(Decl("y")));

        var ok = harness.Link(Sheet(Include("x.xsl"), Import("y.xsl")));

        await Assert.That(ok).IsFalse();
        await Assert.That(Codes(harness.Problems)).IsEqualTo("NES3003/XTSE0200");
    }

    [Test]
    public async Task Precedence_PrincipalModule_ImportAfterSchematronChildren_IsAccepted(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var harness = new ModuleHarness();
        _ = harness.Files.Add("file:///m/lib.xsl", Sheet(Decl("l")));
        var principal =
            $"<sch:schema xmlns:sch=\"http://purl.oclc.org/dsdl/schematron\" xmlns:xsl=\"{Xslt}\">"
            + "<sch:title>t</sch:title><sch:ns prefix=\"a\" uri=\"urn:a\"/><sch:let name=\"x\" value=\"1\"/>"
            + "<xsl:import href=\"lib.xsl\"/><sch:pattern/></sch:schema>";

        var ok = harness.Link(principal);

        await Assert.That(ok).IsTrue();
        await Assert.That(Describe(harness.Set)).IsEqualTo("l@1");
    }

    [Test]
    public async Task Precedence_PrincipalModule_ImportAfterAnXslVariable_IsMisplacedXtse0200(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var harness = new ModuleHarness();
        _ = harness.Files.Add("file:///m/lib.xsl", Sheet(Decl("l")));
        var principal =
            $"<sch:schema xmlns:sch=\"http://purl.oclc.org/dsdl/schematron\" xmlns:xsl=\"{Xslt}\">\n"
            + "<sch:title>t</sch:title>\n<xsl:variable name=\"v\" select=\"1\"/>\n<xsl:import href=\"lib.xsl\"/></sch:schema>";

        var ok = harness.Link(principal);

        await Assert.That(ok).IsFalse();
        await Assert.That(Codes(harness.Problems)).IsEqualTo("NES3003/XTSE0200");
        await Assert.That(harness.Problems.Problems[0].Location.Line).IsEqualTo(4);
    }

    [Test]
    public async Task Link_ElementWithoutNamespaceAtTheTopOfAModule_IsMisplacedXtse0130(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var harness = new ModuleHarness();
        _ = harness.Files.Add("file:///m/lib.xsl", Sheet("<data/>", Decl("l")));

        var ok = harness.Link(Sheet(Import("lib.xsl")));

        await Assert.That(ok).IsFalse();
        await Assert.That(Codes(harness.Problems)).IsEqualTo("NES3003/XTSE0130");
        await Assert.That(harness.Problems.Problems[0].Location.Source).IsEqualTo("file:///m/lib.xsl");
        await Assert.That(harness.Problems.Problems[0].Location.Line).IsEqualTo(2);
        await Assert.That(Describe(harness.Set)).IsEqualTo("l@1");
    }

    [Test]
    public async Task Link_TextAtTheTopOfAModule_IsMisplacedXtse0120_WhiteSpaceIsNot(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var harness = new ModuleHarness();
        _ = harness
            .Files.Add(
                "file:///m/text.xsl",
                Sheet(Decl("t")).Replace("\n</xsl:stylesheet>", " stray\n</xsl:stylesheet>", StringComparison.Ordinal)
            )
            .Add("file:///m/clean.xsl", Sheet(Decl("c")));

        var ok = harness.Link(Sheet(Import("clean.xsl"), Import("text.xsl")));

        await Assert.That(ok).IsFalse();
        await Assert.That(Codes(harness.Problems)).IsEqualTo("NES3003/XTSE0120");
        await Assert.That(harness.Problems.Problems[0].Location.Source).IsEqualTo("file:///m/text.xsl");
        await Assert.That(Describe(harness.Set)).IsEqualTo("c@1,t@2");
    }

    [Test]
    public async Task Link_UserDefinedDataElementAtTheTopOfAModule_IsIgnored(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var harness = new ModuleHarness();
        _ = harness.Files.Add("file:///m/lib.xsl", Sheet("<d:data xmlns:d=\"urn:d\"/>", Decl("l"), "<!-- c -->"));

        var ok = harness.Link(Sheet(Import("lib.xsl")));

        await Assert.That(ok).IsTrue();
        await Assert.That(Describe(harness.Set)).IsEqualTo("l@1");
    }

    [Test]
    public async Task Import_ExceedsMaxExternalResources_ReportsTheLimitAtTheSecondReference(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var harness = new ModuleHarness(limits: ValidationLimits.Default.WithMaxExternalResources(1));
        _ = harness.Files.Add("file:///m/a.xsl", Sheet(Decl("a"))).Add("file:///m/b.xsl", Sheet(Decl("b")));

        var ok = harness.Link(Sheet(Import("a.xsl"), Import("b.xsl")));

        await Assert.That(ok).IsFalse();
        await Assert.That(harness.Problems.Problems.Count).IsEqualTo(1);
        await Assert
            .That(harness.Problems.Problems[0].Code)
            .IsEqualTo(CoreDiagnosticCodes.ExternalResourceLimitExceeded);
        await Assert.That(harness.Problems.Problems[0].Location.Line).IsEqualTo(3);
    }

    [Test]
    public async Task Import_HrefResolvesAgainstTheContainingModule_AndThePassedRequesterIsTheContainingModule(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolver = new RecordingResolver();
        var harness = new ModuleHarness(resolver);
        _ = resolver
            .Inner.Add("file:///m/sub/a.xsl", Sheet(Import("b.xsl")))
            .Add("file:///m/sub/b.xsl", Sheet(Decl("b")));

        var ok = harness.Link(Sheet(Import("sub/a.xsl")));

        await Assert.That(ok).IsTrue();
        await Assert.That(resolver.Requests).IsEquivalentTo(["file:///m/sub/a.xsl", "file:///m/sub/b.xsl"]);
        await Assert.That(resolver.BaseUris).IsEquivalentTo([Principal, "file:///m/sub/a.xsl"]);
    }

    [Test]
    public async Task Link_SameModuleIncludedTwice_IsLoadedOnceAndPlacedTwice(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var harness = new ModuleHarness();
        _ = harness.Files.Add("file:///m/x.xsl", Sheet(Decl("x")));

        var ok = harness.Link(Sheet(Include("x.xsl"), Include("./x.xsl")));

        await Assert.That(ok).IsTrue();
        await Assert.That(Describe(harness.Set)).IsEqualTo("x@1,x@1");
        await Assert.That(harness.Files.Requests.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Link_DeepChainAtARaisedLimit_CompletesOnASmallStackWithoutRecursion(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        const int depth = 4000;
        var resolver = new ChainResolver(depth);
        var harness = new ModuleHarness(resolver, ValidationLimits.Default.WithMaxExternalResources(depth + 10));
        var ok = false;
        var thread = new Thread(() => ok = harness.Link(Sheet(Import("m0.xsl"))), 256 * 1024);

        thread.Start();
        thread.Join();

        await Assert.That(ok).IsTrue();
        await Assert.That(harness.Set.Declarations.Count).IsEqualTo(depth);
        await Assert.That(harness.Set.Declarations[0].Precedence).IsEqualTo(1);
        await Assert.That(harness.Set.Declarations[depth - 1].Precedence).IsEqualTo(depth);
    }

    [Test]
    public async Task Link_PrincipalWithoutSystemId_LinksWithoutReferencesAndReportsARelativeReferenceWithoutBase(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var plain = new ModuleHarness();
        var relative = new ModuleHarness();

        var linked = plain.Link(Sheet(Decl("a")), systemId: null);
        var refused = relative.Link(Sheet(Import("lib.xsl")), systemId: null);

        await Assert.That(linked).IsTrue();
        await Assert.That(Describe(plain.Set)).IsEqualTo("a@1");
        await Assert.That(refused).IsFalse();
        await Assert.That(relative.Problems.Problems[0].Code).IsEqualTo(CoreDiagnosticCodes.ResourceBaseUriMissing);
    }

    [Test]
    public async Task Link_CancelledToken_StopsTheLoad(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var harness = new ModuleHarness();
        _ = harness.Files.Add("file:///m/lib.xsl", Sheet(Decl("l")));
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        await Assert
            .That(() =>
            {
                var loader = new ModuleLoader(harness.Files, ValidationLimits.Default);
                return new XsltModuleLinker(loader, harness.Problems, source.Token).TryLink(
                    TopLevel(Sheet(Import("lib.xsl"))),
                    Principal,
                    out _
                );
            })
            .Throws<OperationCanceledException>();
    }

    [Test]
    public async Task Link_RejectsNullArguments(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var loader = new ModuleLoader(new FakeResources(), ValidationLimits.Default);
        var problems = new FakeProblemSink();
        var linker = new XsltModuleLinker(loader, problems, CancellationToken.None);

        await Assert
            .That(() => new XsltModuleLinker(null!, problems, CancellationToken.None))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => new XsltModuleLinker(loader, null!, CancellationToken.None))
            .Throws<ArgumentNullException>();
        await Assert.That(() => linker.TryLink(null!, Principal, out _)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task ModuleSet_And_Declaration_RejectInvalidArguments(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var element = TopLevel(Sheet(Decl("a")))[0];

        await Assert.That(() => new XsltModuleSet(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => new XsltModuleDeclaration(default, Principal, 1)).Throws<ArgumentNullException>();
        await Assert.That(() => new XsltModuleDeclaration(element, Principal, 0)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(XsltModuleSet.Empty.Declarations.Count).IsEqualTo(0);
        await Assert.That(new XsltModuleDeclaration(element, null, 3).SystemId).IsNull();
    }

    private sealed class RecordingResolver : IResourceResolver
    {
        internal FakeResources Inner { get; } = new();

        internal List<string> Requests => Inner.Requests;

        internal List<string> BaseUris { get; } = [];

        public ResourceResult Resolve(in ResourceRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            BaseUris.Add(request.BaseUri ?? string.Empty);
            return Inner.Resolve(in request, cancellationToken);
        }
    }

    // Serves a document under an alias with the system identifier of another one, as a resolver that canonicalizes does.
    private sealed class AliasResolver(string alias, string canonical) : IResourceResolver
    {
        internal FakeResources Inner { get; } = new();

        public ResourceResult Resolve(in ResourceRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!string.Equals(request.ResolvedUri, alias, StringComparison.Ordinal))
            {
                return Inner.Resolve(in request, cancellationToken);
            }

            var target = new ResourceRequest(request.Kind, canonical, request.BaseUri, canonical);
            var found = Inner.Resolve(in target, cancellationToken);
            return found.Status == ResourceStatus.Found ? found : ResourceResult.NotFound("no document");
        }
    }

    // Serves m0.xsl to m{n-1}.xsl, each of which imports the next and declares one variable.
    private sealed class ChainResolver(int depth) : IResourceResolver
    {
        public ResourceResult Resolve(in ResourceRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var uri = request.ResolvedUri ?? request.Reference;
            var name = uri.AsSpan(uri.LastIndexOf('/') + 1);
            var index = int.Parse(name.Slice(1, name.Length - 5), System.Globalization.CultureInfo.InvariantCulture);
            var body =
                index + 1 < depth
                    ? Sheet(Import("m" + (index + 1) + ".xsl"), Decl("v" + index))
                    : Sheet(Decl("v" + index));
            return ResourceResult.Found(new ReadOnlyMemory<byte>(System.Text.Encoding.UTF8.GetBytes(body)), uri);
        }
    }
}
