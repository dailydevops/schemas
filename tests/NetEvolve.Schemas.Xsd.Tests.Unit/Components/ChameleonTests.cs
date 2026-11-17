namespace NetEvolve.Schemas.Xsd.Tests.Unit.Components;

using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using static NetEvolve.Schemas.Xsd.Tests.Unit.SchemaDocuments.SchemaFixture;

public class ChameleonTests
{
    private const string Xs = "http://www.w3.org/2001/XMLSchema";

    private static NetEvolve.Schemas.Xsd.SchemaDocuments.SchemaSyntaxNode Element(string attributes)
    {
        var (document, issues) = Parse(Open.Replace("<xs:schema ", "<xs:schema " + attributes + " ") + "<xs:element name=\"e\" type=\"t\"/>" + Close);
        if (issues.Count > 0)
        {
            throw new InvalidOperationException(issues[0].Message);
        }

        return Find(document, "element");
    }

    [Test]
    public async Task Chameleon_DeclaredAndUnprefixedNames_TakeTheIncludingNamespace()
    {
        var scope = new SchemaNamespaceScope("urn:inc", isChameleon: true);
        var element = Element(string.Empty);

        await Assert.That(scope.Declare("e").NamespaceName).IsEqualTo("urn:inc");
        await Assert.That(scope.TryResolve(element, "t", out var reference)).IsTrue();
        await Assert.That(reference.NamespaceName).IsEqualTo("urn:inc");
        await Assert.That(reference.LocalName).IsEqualTo("t");
    }

    [Test]
    public async Task Chameleon_PrefixedName_KeepsItsNamespace()
    {
        var scope = new SchemaNamespaceScope("urn:inc", isChameleon: true);

        await Assert.That(scope.TryResolve(Element(string.Empty), "xs:string", out var reference)).IsTrue();
        await Assert.That(reference.NamespaceName).IsEqualTo(Xs);
    }

    [Test]
    public async Task Chameleon_DefaultNamespaceDeclaration_Wins()
    {
        var scope = new SchemaNamespaceScope("urn:inc", isChameleon: true);

        await Assert.That(scope.TryResolve(Element("xmlns=\"urn:default\""), "t", out var reference)).IsTrue();
        await Assert.That(reference.NamespaceName).IsEqualTo("urn:default");
    }

    [Test]
    public async Task NonChameleon_UnprefixedNameWithoutDefault_HasNoNamespace()
    {
        var scope = new SchemaNamespaceScope("urn:own", isChameleon: false);

        await Assert.That(scope.Declare("e").NamespaceName).IsEqualTo("urn:own");
        await Assert.That(scope.TryResolve(Element(string.Empty), "t", out var reference)).IsTrue();
        await Assert.That(reference.NamespaceName).IsEmpty();
    }

    [Test]
    [Arguments("nope:t")]
    [Arguments(":t")]
    [Arguments("xs:")]
    [Arguments("")]
    public async Task UnboundOrMalformedName_DoesNotResolve(string lexical)
    {
        var scope = new SchemaNamespaceScope("urn:own", isChameleon: false);

        await Assert.That(scope.TryResolve(Element(string.Empty), lexical, out _)).IsFalse();
    }
}
