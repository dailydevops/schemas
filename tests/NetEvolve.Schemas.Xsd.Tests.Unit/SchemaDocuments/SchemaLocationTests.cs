namespace NetEvolve.Schemas.Xsd.Tests.Unit.SchemaDocuments;

using static NetEvolve.Schemas.Xsd.Tests.Unit.SchemaDocuments.SchemaFixture;

/// <summary>Locations of the nodes and the documented limitation for attributes (design #108 AP9).</summary>
public class SchemaLocationTests
{
    [Test]
    public async Task SchemaSyntax_Attribute_LocationIsTheStartTagOfItsElement()
    {
        var (document, issues) = Parse(
            Open + "  <xs:element\n      name=\"e\"\n      type=\"xs:string\"\n        minOccurs=\"0\"/>" + Close
        );

        var element = Find(document, "element");

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(element.AttributeCount).IsEqualTo(3);
        for (var i = 0; i < element.AttributeCount; i++)
        {
            var location = element.GetAttribute(i).Location;
            await Assert.That(location).IsEqualTo(element.Location);
            await Assert.That(location.Line).IsEqualTo(2);
            await Assert.That(location.Column).IsEqualTo(3);
        }
    }

    [Test]
    public async Task Error_OnAnAttribute_IsLocatedAtTheStartTagOfItsElement()
    {
        var (_, issues) = Parse(Open + "  <xs:element\n      name=\"e\"\n      bogus=\"1\"/>" + Close);

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Location.Line).IsEqualTo(2);
        await Assert.That(issues[0].Location.Column).IsEqualTo(3);
    }

    [Test]
    public async Task Location_CountsLinesAndColumnsAfterTheXmlDeclarationAndComments()
    {
        var (document, _) = Parse(
            "<?xml version=\"1.0\"?>\r\n<!-- c -->\r\n<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\">\r\n\t<xs:notation name=\"n\"/></xs:schema>"
        );

        await Assert.That(document.Root!.Location.Line).IsEqualTo(3);
        await Assert.That(document.Root.Location.Column).IsEqualTo(1);
        await Assert.That(Find(document, "notation").Location.Line).IsEqualTo(4);
        await Assert.That(Find(document, "notation").Location.Column).IsEqualTo(2);
    }
}
