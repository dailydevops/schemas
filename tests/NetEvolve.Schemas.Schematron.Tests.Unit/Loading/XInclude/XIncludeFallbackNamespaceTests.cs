namespace NetEvolve.Schemas.Schematron.Tests.Unit.Loading.XInclude;

using System.Collections.Generic;
using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Tests.Unit.Loading.Inclusion;
using NetEvolve.Schemas.Xml.Contracts;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Loading.XInclude.XIncludeFixture;

public sealed class XIncludeFallbackNamespaceTests
{
    [Test]
    public async Task PrefixDeclaredOnTheInclude_IsInScopeForTheFallbackContent()
    {
        var xml = Doc(
            "<xi:include href=\"none.xml\" xmlns:f=\"urn:f\">" + Fallback("<x a=\"f:y\"/>") + "</xi:include>"
        );

        var outcome = Run(xml, new FakeResources());

        await Assert.That(outcome.Ok).IsTrue();
        await Assert.That(Bindings(outcome.Document)).IsEqualTo("f=urn:f");
        await Assert.That(outcome.Text).IsEqualTo("<root><x a=\"f:y\"></x></root>");
    }

    [Test]
    public async Task PrefixDeclaredOnTheFallback_IsInScopeForTheFallbackContent()
    {
        var xml = Doc(
            "<xi:include href=\"none.xml\"><xi:fallback xmlns:g=\"urn:g\"><x/><y/></xi:fallback></xi:include>"
        );

        var outcome = Run(xml, new FakeResources());

        await Assert.That(Bindings(outcome.Document)).IsEqualTo("g=urn:g");
        await Assert.That(Bindings(outcome.Document, 1)).IsEqualTo("g=urn:g");
    }

    [Test]
    public async Task PrefixRedeclaredByTheContent_KeepsTheBindingOfTheContent()
    {
        var xml = Doc(
            "<xi:include href=\"none.xml\" xmlns:f=\"urn:outer\" xmlns:h=\"urn:h\">"
                + "<xi:fallback xmlns:f=\"urn:fallback\"><x xmlns:f=\"urn:content\" a=\"1\"/><y/></xi:fallback></xi:include>"
        );

        var outcome = Run(xml, new FakeResources());

        await Assert.That(Bindings(outcome.Document)).IsEqualTo("f=urn:content;h=urn:h");
        await Assert.That(Bindings(outcome.Document, 1)).IsEqualTo("f=urn:fallback;h=urn:h");
        await Assert.That(outcome.Text).IsEqualTo("<root><x a=\"1\"></x><y></y></root>");
    }

    [Test]
    public async Task NestedInclude_SeesTheBindingsOfTheOuterOne()
    {
        var xml = Doc(
            "<xi:include href=\"a.xml\" xmlns:o=\"urn:o\"><xi:fallback>"
                + "<xi:include href=\"b.xml\" xmlns:i=\"urn:i\"><xi:fallback><x/></xi:fallback></xi:include>"
                + "</xi:fallback></xi:include>"
        );

        var outcome = Run(xml, new FakeResources());

        await Assert.That(Bindings(outcome.Document)).IsEqualTo("i=urn:i;o=urn:o");
    }

    [Test]
    public async Task IncludeWithoutDeclarations_AddsNothing()
    {
        var outcome = Run(Doc(Inc("none.xml", string.Empty, Fallback("<x/>"))), new FakeResources());

        await Assert.That(Bindings(outcome.Document)).IsEqualTo(string.Empty);
    }

    // The prefix bindings of the nth element child of the document element, without the default and xml namespaces.
    private static string Bindings(XmlNode document, int index = 0)
    {
        var element = document.FirstChild.FirstChild;
        for (var i = 0; ; element = element.NextSibling)
        {
            if (element.Kind == XmlNodeKind.Element && i++ == index)
            {
                break;
            }
        }

        var names = element.Tree!.Names;
        var found = new List<string>();
        for (var ns = element.FirstNamespace; !ns.IsEmpty; ns = ns.NextNamespace)
        {
            var prefix = names.GetString(ns.Name.Local);
            if (prefix is not ("" or "xml" or "xi"))
            {
                found.Add(prefix + "=" + names.GetString(ns.Name.NamespaceUri));
            }
        }

        found.Sort(System.StringComparer.Ordinal);
        return string.Join(";", found);
    }
}
