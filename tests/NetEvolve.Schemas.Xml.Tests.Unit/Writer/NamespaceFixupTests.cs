namespace NetEvolve.Schemas.Xml.Tests.Unit.Writer;

using static NetEvolve.Schemas.Xml.Tests.Unit.Writer.WriterHarness;

public class NamespaceFixupTests
{
    [Test]
    public async Task UndeclaredPrefix_IsDeclaredOnTheElement()
    {
        var xml = Chars(
            (w, _) =>
            {
                w.Start("a", "p", "urn:x");
                w.End("a", "p", "urn:x");
            }
        );

        await Assert.That(xml).IsEqualTo("<p:a xmlns:p=\"urn:x\"/>");
    }

    [Test]
    public async Task UndeclaredDefaultNamespace_IsDeclaredOnTheElement()
    {
        var xml = Chars(
            (w, _) =>
            {
                w.Start("a", "", "urn:x");
                w.End("a", "", "urn:x");
            }
        );

        await Assert.That(xml).IsEqualTo("<a xmlns=\"urn:x\"/>");
    }

    [Test]
    public async Task UnqualifiedChildInDefaultNamespace_UndeclaresIt()
    {
        var xml = Chars(
            (w, _) =>
            {
                w.Start("a", "", "urn:x");
                w.Start("b");
                w.End("b");
                w.End("a", "", "urn:x");
            }
        );

        await Assert.That(xml).IsEqualTo("<a xmlns=\"urn:x\"><b xmlns=\"\"/></a>");
    }

    [Test]
    public async Task PrefixBoundToAnotherUriInScope_IsReplacedByAFreshPrefix()
    {
        var xml = Chars(
            (w, _) =>
            {
                w.Start("a", "p", "urn:x");
                w.Start("b", "p", "urn:y");
                w.End("b", "p", "urn:y");
                w.End("a", "p", "urn:x");
            }
        );

        await Assert.That(xml).IsEqualTo("<p:a xmlns:p=\"urn:x\"><ns1:b xmlns:ns1=\"urn:y\"/></p:a>");
    }

    [Test]
    public async Task PrefixBoundToAnotherUriInScope_ReusesAnExistingPrefixOfTheUri()
    {
        var xml = Chars(
            (w, _) =>
            {
                w.Start("a", "p", "urn:x", decls: [("q", "urn:y")]);
                w.Start("c", "p", "urn:y");
                w.End("c", "p", "urn:y");
                w.End("a", "p", "urn:x");
            }
        );

        await Assert.That(xml).IsEqualTo("<p:a xmlns:q=\"urn:y\" xmlns:p=\"urn:x\"><q:c/></p:a>");
    }

    [Test]
    public async Task RedundantDeclaration_IsNotWrittenTwice()
    {
        var xml = Chars(
            (w, _) =>
            {
                w.Start("a", "p", "urn:x", decls: [("p", "urn:x")]);
                w.Start("b", "p", "urn:x", decls: [("p", "urn:x")]);
                w.End("b", "p", "urn:x");
                w.End("a", "p", "urn:x");
            }
        );

        await Assert.That(xml).IsEqualTo("<p:a xmlns:p=\"urn:x\"><p:b/></p:a>");
    }

    [Test]
    public async Task ExplicitDeclaration_IsKeptEvenWhenUnused()
    {
        var xml = Chars(
            (w, _) =>
            {
                w.Start("a", decls: [("p", "urn:x")]);
                w.End("a");
            }
        );

        await Assert.That(xml).IsEqualTo("<a xmlns:p=\"urn:x\"/>");
    }

    [Test]
    public async Task NamespacedAttribute_GetsADeclaration_AndPrefixlessOneAFreshPrefix()
    {
        var xml = Chars(
            (w, _) =>
            {
                w.Start("a", attrs: [new Att("b", "1", "p", "urn:x"), new Att("c", "2", "", "urn:y")]);
                w.End("a");
            }
        );

        await Assert.That(xml).IsEqualTo("<a xmlns:p=\"urn:x\" xmlns:ns1=\"urn:y\" p:b=\"1\" ns1:c=\"2\"/>");
    }

    [Test]
    public async Task XmlNamespaceAttribute_UsesThePredefinedPrefixWithoutDeclaration()
    {
        var xml = Chars(
            (w, _) =>
            {
                w.Start("a", attrs: [new Att("lang", "en", "xml", "http://www.w3.org/XML/1998/namespace")]);
                w.End("a");
            }
        );

        await Assert.That(xml).IsEqualTo("<a xml:lang=\"en\"/>");
    }
}
