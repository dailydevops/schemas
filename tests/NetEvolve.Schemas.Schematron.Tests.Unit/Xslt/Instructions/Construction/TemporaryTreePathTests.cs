namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Construction;

using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>A path expression over the temporary tree that the construction instructions built returns the constructed nodes.</summary>
public class TemporaryTreePathTests
{
    [Test]
    public async Task Path_OverTheTemporaryTree_ReturnsTheConstructedNodesInDocumentOrder()
    {
        var harness = new ConstructionHarness(
            "<out><item n=\"1\">a</item><item n=\"2\">b</item><xsl:element name=\"item\"><xsl:attribute name=\"n\">3</xsl:attribute>c</xsl:element><other><item n=\"4\"/></other></out>"
        );
        using var sink = ConstructionHarness.NewTree();

        var status = harness.Execute(sink);
        var items = PathQuery.Select(sink.Document, "/out/item");
        var attributes = PathQuery.Select(sink.Document, "/out/item/@n");
        var deep = PathQuery.Select(sink.Document, "//item");
        var children = PathQuery.Select(sink.Document, "/out/*");

        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        await Assert.That(items.Count).IsEqualTo(3);
        await Assert.That(PathQuery.Describe(items[0])).IsEqualTo("Element:item=a");
        await Assert.That(PathQuery.Describe(items[1])).IsEqualTo("Element:item=b");
        await Assert.That(PathQuery.Describe(items[2])).IsEqualTo("Element:item=c");
        await Assert.That(attributes.Count).IsEqualTo(3);
        await Assert.That(PathQuery.Describe(attributes[2])).IsEqualTo("Attribute:n=3");
        await Assert.That(deep.Count).IsEqualTo(4);
        await Assert.That(children.Count).IsEqualTo(4);
    }
}
