namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Library.Core;

using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Library.Core.CoreFixture;

/// <summary>
/// <c>generate-id()</c> (XSLT 2.0, 16.6.4) is implemented once, in the XPath package (#264). A Schematron binding enables it with the
/// XSLT 2.0 host flag; this package adds no copy (decision D1 of issue #285, design #150 ownership table).
/// </summary>
public class GenerateIdHostTests
{
    [Test]
    [Arguments("xslt")]
    [Arguments("xslt1")]
    [Arguments("xslt2")]
    [Arguments("xslt3")]
    public async Task GenerateId_UnderAnXsltBinding_IsCallable(string binding)
    {
        var nodes = Nodes(2);
        var scope = Scope(binding);

        var first = Run(scope, "generate-id", XdmItem.FromNode(nodes[0]));
        var second = Run(scope, "generate-id", XdmItem.FromNode(nodes[1]));

        await Assert.That(first).StartsWith("string:d");
        await Assert.That(second).StartsWith("string:d");
        await Assert.That(second).IsNotEqualTo(first);
        await Assert.That(Run(scope, "generate-id", XdmItem.FromNode(nodes[0]))).IsEqualTo(first);
    }

    [Test]
    [Arguments("xpath")]
    [Arguments("xpath2")]
    public async Task GenerateId_UnderAnXpathBinding_IsNotFound(string binding)
    {
        var nodes = Nodes(1);

        await Assert.That(Run(Scope(binding), "generate-id", XdmItem.FromNode(nodes[0]))).IsEqualTo("notfound");
    }

    [Test]
    [Arguments("xslt", true)]
    [Arguments("xslt1", true)]
    [Arguments("xslt2", true)]
    [Arguments("xslt3", true)]
    [Arguments("xpath", false)]
    [Arguments("xpath2", false)]
    public async Task ActiveHost_SetsTheXslt20FlagAndTheCurrentItemSlotForXsltBindingsOnly(string binding, bool xslt)
    {
        var host = Scope(binding).ActiveHost;

        await Assert.That((host & XPathHostRequirements.Xslt20Host) != 0).IsEqualTo(xslt);
        await Assert.That((host & XPathHostRequirements.CurrentItem) != 0).IsEqualTo(xslt);
    }
}
