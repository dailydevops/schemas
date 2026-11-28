namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Flow;

using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Seams;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions.FunctionTree;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Flow.Fl;

/// <summary>
/// <c>xsl:fallback</c> (XSLT 2.0 section 18.2): the fallback children of an instruction element that is unknown to XSLT 2.0 and 3.0
/// run in its place; a known but deferred instruction and an unknown one without fallback stay the unsupported construct error (#39).
/// </summary>
public class FallbackTests
{
    private static string Run(FlowHarness harness)
    {
        var status = harness.Execute();
        return status == QueryStatus.Ok ? harness.Sink.Joined() : "problem";
    }

    [Test]
    public async Task Unknown_WithFallback_RunsTheFallback()
    {
        var harness = new FlowHarness(X("future", 10, X("fallback", 11, ValueOf("'fb'", null, 12))));

        await Assert.That(harness.CompileCodes).IsEqualTo(string.Empty);
        await Assert.That(Run(harness)).IsEqualTo("fb");
    }

    [Test]
    public async Task Unknown_WithSeveralFallbacks_RunsThemInOrder()
    {
        var harness = new FlowHarness(
            X("future", 10, X("fallback", 11, ValueOf("'a'", null, 12)), X("fallback", 13, ValueOf("'b'", null, 14)))
        );

        await Assert.That(Run(harness)).IsEqualTo("ab");
    }

    [Test]
    public async Task Unknown_WithFallbackInsideAnIf_RunsTheFallback()
    {
        var harness = new FlowHarness(If("1 = 1", 10, X("future", 11, X("fallback", 12, ValueOf("'fb'", null, 13)))));

        await Assert.That(Run(harness)).IsEqualTo("fb");
    }

    [Test]
    public async Task Unknown_WithoutFallback_IsTheUnsupportedConstructError()
    {
        var harness = new FlowHarness(X("future", 10, ValueOf("'x'", null, 11)));

        await Assert.That(harness.Executable).IsNull();
        await Assert.That(harness.CompileCodes).IsEqualTo("NES3204");
    }

    [Test]
    [Arguments("iterate")]
    [Arguments("try")]
    [Arguments("merge")]
    public async Task KnownButDeferred_WithFallback_IsTheUnsupportedConstructError(string local)
    {
        ArgumentNullException.ThrowIfNull(local);
        var harness = new FlowHarness(X(local, 10, X("fallback", 11, ValueOf("'fb'", null, 12))));

        await Assert.That(harness.Executable).IsNull();
        await Assert.That(harness.CompileCodes).IsEqualTo("NES3204");
    }

    [Test]
    public async Task Unknown_WithFailingFallback_ReportsTheProblemOfTheFallback()
    {
        var harness = new FlowHarness(X("future", 10, X("fallback", 11, ValueOf("'x", null, 12))));

        await Assert.That(harness.Executable).IsNull();
        await Assert.That(harness.CompileCodes).IsEqualTo("NES3202/XPST0003");
    }

    [Test]
    public async Task KnownInstruction_IgnoresItsFallbackChildren()
    {
        var harness = new FlowHarness(
            If("1 = 1", 10, ValueOf("'a'", null, 11), X("fallback", 12, ValueOf("'fb'", null, 13)))
        );

        await Assert.That(harness.CompileCodes).IsEqualTo(string.Empty);
        await Assert.That(Run(harness)).IsEqualTo("a");
    }

    [Test]
    public async Task ForEach_IgnoresItsFallbackChildren()
    {
        var harness = new FlowHarness(
            ForEach("(1, 2)", 10, ValueOf("'x'", null, 11), X("fallback", 12, ValueOf("'fb'", null, 13)))
        );

        await Assert.That(harness.CompileCodes).IsEqualTo(string.Empty);
        await Assert.That(Run(harness)).IsEqualTo("xx");
    }
}
