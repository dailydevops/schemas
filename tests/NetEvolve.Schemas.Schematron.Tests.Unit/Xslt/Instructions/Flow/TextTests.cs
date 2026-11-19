namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Flow;

using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Flow.Fl;

/// <summary><c>xsl:text</c> (XSLT 2.0 section 11.4.2): its text, with its white space, becomes a text node.</summary>
public class TextTests
{
    [Test]
    public async Task Text_KeepsWhitespace()
    {
        var harness = new FlowHarness(Text("  a \t b\n", 10));

        var status = harness.Execute();

        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        await Assert.That(harness.Sink.All()).IsEqualTo("text:  a \t b\n");
    }

    [Test]
    public async Task Text_OfOnlyWhitespace_IsKept()
    {
        var harness = new FlowHarness(Text("   ", 10));

        _ = harness.Execute();

        await Assert.That(harness.Sink.All()).IsEqualTo("text:   ");
    }

    [Test]
    public async Task Text_Empty_PushesNothingVisible()
    {
        var harness = new FlowHarness(Text(null, 10));

        var status = harness.Execute();

        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        await Assert.That(harness.Sink.Joined()).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Text_WithAnElementChild_IsStaticErrorXtse0010()
    {
        var harness = new FlowHarness(FunctionTree.X("text", 10, Seq("1", 11)));

        await Assert.That(harness.Executable).IsNull();
        await Assert.That(harness.CompileCodes).IsEqualTo("NES3003/XTSE0010");
    }

    [Test]
    [Arguments("yes")]
    [Arguments("no")]
    public async Task Text_DisableOutputEscaping_IsAcceptedAndChangesNothing(string value)
    {
        var harness = new FlowHarness(Text("a<b", 10, value));

        _ = harness.Execute();

        await Assert.That(harness.Sink.Joined()).IsEqualTo("a<b");
    }

    [Test]
    public async Task Text_DisableOutputEscaping_WithAnotherValue_IsStaticErrorXtse0020()
    {
        var harness = new FlowHarness(Text("a", 10, "maybe"));

        await Assert.That(harness.Executable).IsNull();
        await Assert.That(harness.CompileCodes).IsEqualTo("NES3006/XTSE0020");
    }
}
