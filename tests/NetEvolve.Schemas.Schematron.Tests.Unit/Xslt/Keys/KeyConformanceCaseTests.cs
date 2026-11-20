namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Keys;

using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Keys.KeyFixture;

/// <summary>
/// The two cases of the Schematron conformance suite for <c>xsl:key</c> (<c>iso-rule-based-validation/xsl-key.xml</c> and
/// <c>xsl-key-element-content.xml</c>), as far as this package reaches them: the key declaration of the case is compiled and its
/// lookup counts exactly one node in the instance of the case, <c>&lt;root/&gt;</c>. The end-to-end run of the cases needs the XPath
/// evaluator and the compile pipeline, which are not on main (#297), so the conformance baseline is not changed.
/// </summary>
public class KeyConformanceCaseTests
{
    private static XPathNode Root()
    {
        using var builder = new XmlTreeBuilder();
        builder.StartDocument(new XmlDeclaration("1.0", "utf-8", null));
        var root = builder.Names.Intern(string.Empty, "root", string.Empty);
        builder.StartElement(root, default);
        builder.EndAttributes();
        builder.EndElement(root);
        builder.EndDocument();
        return new XPathNode(builder.Complete().Document);
    }

    [Test]
    public async Task XslKey_TheDeclarationBeforeThePattern_IndexesTheRootElementOnce()
    {
        var fixture = new KeyFixture();
        var (_, provider) = fixture.CreateProvider(Kx.Key("index", "*", "'1'", 3));

        var status = provider.TryLookup(Key("index"), Str("1"), Root(), false, out var nodes);

        await Assert.That(fixture.Problems.Problems).IsEmpty();
        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        await Assert.That(nodes.Count).IsEqualTo(1);
    }

    [Test]
    public async Task XslKeyElementContent_TheTextOfTheElement_IsTheKeyValue()
    {
        var fixture = new KeyFixture();
        var (_, provider) = fixture.CreateProvider(Kx.Key("index", "*", null, 3, Kx.Text("key", 4)));

        var status = provider.TryLookup(Key("index"), Str("key"), Root(), false, out var nodes);

        await Assert.That(fixture.Problems.Problems).IsEmpty();
        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        await Assert.That(nodes.Count).IsEqualTo(1);
    }
}
