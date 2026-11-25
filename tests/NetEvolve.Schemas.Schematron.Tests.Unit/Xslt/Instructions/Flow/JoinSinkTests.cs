namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Flow;

using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Evaluation.Assertions;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Construction;
using NetEvolve.Schemas.Schematron.Xslt.Construction;
using NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;

/// <summary>The join sink of <c>xsl:value-of</c> (#821): its kind is the kind of the sink that it wraps when that sink is text only.</summary>
public class JoinSinkTests
{
    [Test]
    public async Task Kind_OverATextSink_IsText()
    {
        using var text = new TextSink(100);
        using var join = new JoinSink(text, " ");

        await Assert.That(join.Kind).IsEqualTo(SequenceSinkKind.Text);
    }

    [Test]
    public async Task Kind_OverAnItemSink_IsItems()
    {
        using var join = new JoinSink(new ItemSequenceSink(SinkTestSupport.NewTable()), " ");

        await Assert.That(join.Kind).IsEqualTo(SequenceSinkKind.Items);
    }
}
