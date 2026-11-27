namespace NetEvolve.Schemas.Xsd.Tests.Unit.Compilation;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.SchemaDocuments;

public class ImportScopeTests
{
    private const string Doc = "file:///d.xsd";

    private static DocumentLocation At(string? source) => new(source, 1, 1);

    [Test]
    public async Task IsVisible_DocumentWithoutSource_IsNotRestricted()
    {
        var scope = new ImportScope();

        await Assert.That(scope.IsVisible(At(null), new XPathQName("urn:x", "n"))).IsTrue();
    }

    [Test]
    public async Task Allow_DocumentWithoutSource_IsIgnored()
    {
        var scope = new ImportScope();
        scope.Allow(null, "urn:x");

        await Assert.That(scope.IsVisible(At(Doc), new XPathQName("urn:y", "n"))).IsTrue();
    }

    [Test]
    public async Task IsVisible_SchemaAndXmlNamespaceWithoutImport_AreVisible()
    {
        var scope = new ImportScope();
        scope.Allow(Doc, "urn:x");

        await Assert.That(scope.IsVisible(At(Doc), new XPathQName(SchemaGrammar.Namespace, "n"))).IsTrue();
        await Assert
            .That(scope.IsVisible(At(Doc), new XPathQName("http://www.w3.org/XML/1998/namespace", "lang")))
            .IsTrue();
        await Assert.That(scope.IsVisible(At(Doc), new XPathQName("urn:y", "n"))).IsFalse();
    }

    [Test]
    public async Task IsVisible_NamespaceAllowedEverywhere_IsVisibleToEveryDocument()
    {
        var scope = new ImportScope();
        scope.Allow(Doc, "urn:x");
        scope.AllowEverywhere("urn:y");

        await Assert.That(scope.IsVisible(At(Doc), new XPathQName("urn:y", "n"))).IsTrue();
        await Assert.That(scope.IsVisible(At(Doc), new XPathQName("urn:z", "n"))).IsFalse();
    }
}
