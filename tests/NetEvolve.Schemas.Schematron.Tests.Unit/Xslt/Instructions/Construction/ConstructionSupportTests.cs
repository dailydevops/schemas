namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Construction;

using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The helpers of the construction tests read a tree the way the tests expect, so a failure points at the instruction.</summary>
public class ConstructionSupportTests
{
    [Test]
    public async Task PathQuery_SelectsNodesOfATree_AndNodeDumpWritesThem()
    {
        var root = ConstructionSheet.Source("<r xmlns:p=\"urn:p\"><a x=\"1\">t</a><p:a/><!--c--><?t d?></r>");
        var document = XdmItem.FromNode(new XPathNode(root.Node.Parent));

        var children = PathQuery.Select(document, "/r/*");
        var attribute = PathQuery.Select(document, "/r/a/@x");

        await Assert.That(children.Count).IsEqualTo(2);
        await Assert.That(PathQuery.Describe(children[0])).IsEqualTo("Element:a=t");
        await Assert.That(PathQuery.Describe(attribute[0])).IsEqualTo("Attribute:x=1");
        await Assert
            .That(NodeDump.Of(document))
            .IsEqualTo("<r xmlns:p='urn:p'><a x='1'>t</a><p:a/><!--c--><?t d?></r>");
    }

    [Test]
    public async Task NodeDump_WritesAnUndeclarationOfTheParent()
    {
        var root = ConstructionSheet.Source("<r xmlns=\"urn:d\"><e xmlns=\"\"/></r>");

        await Assert.That(NodeDump.Of(root.Node)).IsEqualTo("<r xmlns='urn:d'><e xmlns=''/></r>");
    }
}
