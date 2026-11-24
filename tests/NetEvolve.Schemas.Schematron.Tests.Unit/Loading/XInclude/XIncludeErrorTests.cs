namespace NetEvolve.Schemas.Schematron.Tests.Unit.Loading.XInclude;

using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Schematron.Loading.XInclude;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using NetEvolve.Schemas.Schematron.Tests.Unit.Loading.Inclusion;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Loading.XInclude.XIncludeFixture;

public class XIncludeErrorTests
{
    private static async Task AssertSingle(Outcome outcome, DiagnosticCode code, int line = 0, int column = 0)
    {
        await Assert.That(outcome.Ok).IsFalse();
        await Assert.That(outcome.Problems.Count).IsEqualTo(1);
        await Assert.That(outcome.Problems[0].Code).IsEqualTo(code);
        if (line > 0)
        {
            await Assert.That(outcome.Problems[0].Location.Line).IsEqualTo(line);
            await Assert.That(outcome.Problems[0].Location.Column).IsEqualTo(column);
            await Assert.That(outcome.Problems[0].Location.Source).IsEqualTo(RootUri);
        }
    }

    [Test]
    public async Task MissingResource_WithoutFallback_ReportsNes0102AtTheInclude()
    {
        var outcome = Run(Doc("\n  " + Inc("none.xml")), new FakeResources());

        await AssertSingle(outcome, CoreDiagnosticCodes.ResourceNotFound, 2, 3);
    }

    [Test]
    public async Task ResolverDenied_ReportsNes0100AtTheInclude()
    {
        var outcome = Run(Doc("\n  " + Inc("p.xml")), DenyAllResourceResolver.Instance);

        await AssertSingle(outcome, CoreDiagnosticCodes.ResourceDenied, 2, 3);
    }

    [Test]
    public async Task ResolverDenied_ForTextIncludes_ReportsNes0100()
    {
        var outcome = Run(Doc("\n  " + Inc("p.txt", " parse=\"text\"")), DenyAllResourceResolver.Instance);

        await AssertSingle(outcome, CoreDiagnosticCodes.ResourceDenied, 2, 3);
    }

    [Test]
    public async Task RelativeReferenceWithoutBase_ReportsNes0101()
    {
        var outcome = Run(Doc(Inc("x.xml")), new FakeResources(), systemId: null);

        await AssertSingle(outcome, CoreDiagnosticCodes.ResourceBaseUriMissing);
    }

    [Test]
    public async Task InvalidReference_ReportsNes0103()
    {
        var outcome = Run(Doc(Inc("%zz")), new FakeResources());

        await AssertSingle(outcome, CoreDiagnosticCodes.ResourceReferenceInvalid);
    }

    [Test]
    public async Task MalformedResource_ReportsModuleInvalid()
    {
        var files = new FakeResources().Add("file:///a/bad.xml", "<p>");

        var outcome = Run(Doc(Inc("bad.xml")), files);

        await AssertSingle(outcome, SchematronDiagnosticCodes.ModuleInvalid);
    }

    [Test]
    public async Task DirectCycle_ReportsTheCycleError()
    {
        var files = new FakeResources().Add(RootUri, Doc(Inc("root.xml")));

        var outcome = Run(Doc("\n  " + Inc("root.xml")), files);

        await AssertSingle(outcome, SchematronDiagnosticCodes.IncludeCycle, 2, 3);
    }

    [Test]
    public async Task IncludeOfTheDocumentItself_ByEmptyReference_ReportsTheCycleError()
    {
        var files = new FakeResources().Add(RootUri, "<root/>");

        var outcome = Run(Doc(Inc(string.Empty)), files);

        await AssertSingle(outcome, SchematronDiagnosticCodes.IncludeCycle);
    }

    [Test]
    public async Task IndirectCycle_ReportsTheCycleError_EvenWithAFallback()
    {
        var files = new FakeResources()
            .Add("file:///a/a.xml", Doc(Inc("b.xml")))
            .Add("file:///a/b.xml", Doc(Inc("a.xml")));

        var outcome = Run(Doc(Inc("a.xml", string.Empty, Fallback("<f/>"))), files);

        await AssertSingle(outcome, SchematronDiagnosticCodes.IncludeCycle);
    }

    [Test]
    public async Task SameDocumentIncludedTwiceInSequence_IsNoCycle()
    {
        var files = new FakeResources().Add("file:///a/p.xml", "<p/>");

        var outcome = Run(Doc(Inc("p.xml") + Inc("p.xml")), files);

        await Assert.That(outcome.Ok).IsTrue();
        await Assert.That(files.Requests.Count).IsEqualTo(2);
    }

    private static FakeResources Chain(int length)
    {
        var files = new FakeResources();
        for (var i = 1; i < length; i++)
        {
            files.Add($"file:///a/c{i}.xml", Doc(Inc($"c{i + 1}.xml")));
        }

        return files.Add($"file:///a/c{length}.xml", "<end/>");
    }

    [Test]
    public async Task Depth_ChainBeyondTheLimit_ReportsNes0006Once_EvenWithAFallback()
    {
        var limits = ValidationLimits.Default.WithMaxExternalResources(3);

        var outcome = Run(Doc(Inc("c1.xml", string.Empty, Fallback("<f/>"))), Chain(5), limits);

        await AssertSingle(outcome, CoreDiagnosticCodes.ExternalResourceLimitExceeded);
    }

    [Test]
    public async Task Depth_LongChainWithAHighResourceLimit_StopsAtMaxDepthInsteadOfOverflowingTheStack()
    {
        var limits = ValidationLimits.Default.WithMaxExternalResources(100_000);

        var outcome = Run(Doc(Inc("c1.xml")), Chain(20_000), limits);

        await AssertSingle(outcome, CoreDiagnosticCodes.DepthLimitExceeded);
    }

    [Test]
    public async Task Depth_ChainNearMaxDepthWithAHighResourceLimit_Succeeds()
    {
        var limits = ValidationLimits.Default.WithMaxExternalResources(100_000);

        var outcome = Run(Doc(Inc("c1.xml")), Chain(250), limits);

        await Assert.That(outcome.Ok).IsTrue();
    }

    [Test]
    public async Task Depth_ChainBeyondTheIncludeNestingCap_ReportsNes0002_EvenWithAHigherMaxDepth()
    {
        var limits = ValidationLimits.Default.WithMaxExternalResources(5_000).WithMaxDepth(5_000);

        var outcome = Run(Doc(Inc("c1.xml")), Chain(300), limits);

        await AssertSingle(outcome, CoreDiagnosticCodes.DepthLimitExceeded);
    }

    [Test]
    public async Task Depth_ChainAtTheLimit_Succeeds()
    {
        var limits = ValidationLimits.Default.WithMaxExternalResources(3);

        var outcome = Run(Doc(Inc("c1.xml")), Chain(3), limits);

        await Assert.That(outcome.Ok).IsTrue();
    }

    [Test]
    public async Task Depth_EveryOccurrenceCounts_WithoutDeduplication()
    {
        var files = new FakeResources().Add("file:///a/p.xml", "<p/>");
        var body = Inc("p.xml") + Inc("p.xml") + Inc("p.xml");

        var refused = Run(Doc(body), files, ValidationLimits.Default.WithMaxExternalResources(2));
        var allowed = Run(Doc(body), files, ValidationLimits.Default.WithMaxExternalResources(3));

        await AssertSingle(refused, CoreDiagnosticCodes.ExternalResourceLimitExceeded);
        await Assert.That(allowed.Ok).IsTrue();
    }

    [Test]
    public async Task Depth_TheCountIncludesRefusedAndMissingResources()
    {
        var outcome = Run(
            Doc(
                Inc("a.xml", string.Empty, Fallback(string.Empty)) + Inc("b.xml", string.Empty, Fallback(string.Empty))
            ),
            new FakeResources(),
            ValidationLimits.Default.WithMaxExternalResources(1)
        );

        await AssertSingle(outcome, CoreDiagnosticCodes.ExternalResourceLimitExceeded);
    }

    [Test]
    public async Task Depth_SplicedElementDepthBeyondMaxDepth_ReportsNes0002()
    {
        var files = new FakeResources().Add("file:///a/deep.xml", "<a><b><c/></b></a>");

        var refused = Run(Doc(Inc("deep.xml")), files, ValidationLimits.Default.WithMaxDepth(3));
        var allowed = Run(Doc(Inc("deep.xml")), files, ValidationLimits.Default.WithMaxDepth(4));

        await AssertSingle(refused, CoreDiagnosticCodes.DepthLimitExceeded);
        await Assert.That(allowed.Ok).IsTrue();
    }

    [Test]
    public async Task Size_ResourceBeyondTheLimit_ReportsNes0007_EvenWithAFallback()
    {
        var files = new FakeResources().Add("file:///a/p.xml", "<p>0123456789</p>");

        var outcome = Run(
            Doc(Inc("p.xml", string.Empty, Fallback("<f/>"))),
            files,
            ValidationLimits.Default.WithMaxResourceSize(8)
        );

        await AssertSingle(outcome, CoreDiagnosticCodes.ResourceSizeLimitExceeded);
    }

    [Test]
    [Arguments(" xpointer=\"element(/1)\"")]
    [Arguments(" href=\"p.xml\" xpointer=\"x\"")]
    public async Task Xpointer_ReportsTheExplicitUnsupportedError_EvenWithAFallback(string attributes)
    {
        var files = new FakeResources().Add("file:///a/p.xml", "<p/>");

        var outcome = Run(Doc("\n  <xi:include" + attributes + ">" + Fallback("<f/>") + "</xi:include>"), files);

        await AssertSingle(outcome, SchematronDiagnosticCodes.Unsupported, 2, 3);
        await Assert.That(files.Requests.Count).IsEqualTo(0);
    }

    [Test]
    public async Task FragmentIdentifier_ReportsUnsupportedWithoutLoading()
    {
        var files = new FakeResources().Add("file:///a/p.xml", "<p/>");

        var outcome = Run(Doc(Inc("p.xml#frag")), files);

        await AssertSingle(outcome, SchematronDiagnosticCodes.Unsupported);
        await Assert.That(files.Requests.Count).IsEqualTo(0);
    }

    [Test]
    public async Task MissingHref_ReportsAttributeMissing()
    {
        var outcome = Run(Doc("\n  <xi:include/>"), new FakeResources());

        await AssertSingle(outcome, SchematronDiagnosticCodes.AttributeMissing, 2, 3);
    }

    [Test]
    public async Task UnknownParseValue_ReportsAttributeInvalid()
    {
        var outcome = Run(Doc("\n  " + Inc("p.xml", " parse=\"binary\"")), new FakeResources());

        await AssertSingle(outcome, SchematronDiagnosticCodes.AttributeInvalid, 2, 3);
    }

    [Test]
    public async Task IncludeAsTheDocumentElement_ReportsElementMisplaced()
    {
        var outcome = Run(
            $"<xi:include xmlns:xi=\"{Xi}\" href=\"p.xml\"/>",
            new FakeResources().Add("file:///a/p.xml", "<p/>")
        );

        await AssertSingle(outcome, SchematronDiagnosticCodes.ElementMisplaced, 1, 1);
    }

    [Test]
    public async Task IncludeAsTheDocumentElementOfAnIncludedDocument_ReportsElementMisplaced()
    {
        var files = new FakeResources()
            .Add("file:///a/lib.xml", $"<xi:include xmlns:xi=\"{Xi}\" href=\"p.xml\"/>")
            .Add("file:///a/p.xml", "<p/>");

        var outcome = Run(Doc(Inc("lib.xml")), files);

        await AssertSingle(outcome, SchematronDiagnosticCodes.ElementMisplaced);
        await Assert.That(outcome.Problems[0].Location.Source).IsEqualTo("file:///a/lib.xml");
    }

    [Test]
    public async Task FallbackOutsideAnInclude_ReportsElementMisplaced()
    {
        var outcome = Run(Doc("\n  " + Fallback("<f/>")), new FakeResources());

        await AssertSingle(outcome, SchematronDiagnosticCodes.ElementMisplaced, 2, 3);
    }

    [Test]
    public async Task SecondFallback_ReportsElementMisplaced()
    {
        var outcome = Run(Doc(Inc("none.xml", string.Empty, Fallback("<f/>") + Fallback("<g/>"))), new FakeResources());

        await AssertSingle(outcome, SchematronDiagnosticCodes.ElementMisplaced);
    }

    [Test]
    public async Task IncludeDirectlyInsideAnInclude_ReportsElementMisplaced()
    {
        var outcome = Run(Doc(Inc("a.xml", string.Empty, Inc("b.xml"))), new FakeResources());

        await AssertSingle(outcome, SchematronDiagnosticCodes.ElementMisplaced);
    }

    [Test]
    public async Task UnknownElementInTheXIncludeNamespace_ReportsElementUnknown()
    {
        var outcome = Run(Doc("\n  <xi:other/>"), new FakeResources());

        await AssertSingle(outcome, SchematronDiagnosticCodes.ElementUnknown, 2, 3);
    }

    [Test]
    public async Task OtherChildrenOfAnInclude_AreIgnored()
    {
        var files = new FakeResources().Add("file:///a/p.xml", "<p/>");

        var outcome = Run(Doc(Inc("p.xml", string.Empty, "text<!--c--><?p d?><ext><deep/></ext>")), files);

        await Assert.That(outcome.Ok).IsTrue();
        await Assert.That(outcome.Text).IsEqualTo("<root><p base=\"file:///a/p.xml\"></p></root>");
    }

    [Test]
    public async Task OtherChildrenOfAnInclude_AreIgnoredWhenTheFallbackIsUsed()
    {
        var outcome = Run(
            Doc(Inc("none.xml", string.Empty, "t<ext/><!--c--><?p d?>" + Fallback("<f/>"))),
            new FakeResources()
        );

        await Assert.That(outcome.Text).IsEqualTo("<root><f></f></root>");
    }

    [Test]
    public async Task FirstFatalError_StopsTheRun()
    {
        var outcome = Run(Doc("<xi:other/><xi:other/>" + Inc("none.xml")), new FakeResources());

        await AssertSingle(outcome, SchematronDiagnosticCodes.ElementUnknown);
    }

    [Test]
    public async Task Cancellation_StopsBeforeTheResolverIsAsked()
    {
        var files = new FakeResources().Add("file:///a/p.xml", "<p/>");
        using var source = new CancellationTokenSource();
#if NET10_0_OR_GREATER
        await source.CancelAsync();
#else
        source.Cancel();
#endif
        var resolver = new XIncludeResolver(files, ValidationLimits.Default);
        var document = Load(Doc(Inc("p.xml")));
        var problems = new FakeProblemSink();

        await Assert
            .That(() => resolver.TryResolve(document, RootUri, problems, source.Token, out _))
            .Throws<OperationCanceledException>();
        await Assert.That(files.Requests.Count).IsEqualTo(0);
    }
}
