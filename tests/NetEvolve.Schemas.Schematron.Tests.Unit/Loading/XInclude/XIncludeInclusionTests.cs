namespace NetEvolve.Schemas.Schematron.Tests.Unit.Loading.XInclude;

using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Schematron.Tests.Unit.Loading.Inclusion;
using NetEvolve.Schemas.Xml.Model;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Loading.XInclude.XIncludeFixture;

public class XIncludeInclusionTests
{
    private const string Base = " base=\"";

    [Test]
    public async Task Xml_ReplacesTheElementWithTheIncludedDocumentElement()
    {
        var files = new FakeResources().Add("file:///a/part.xml", "<p/>");

        var outcome = Run(Doc("a" + Inc("part.xml", " accept=\"text/xml\"") + "b"), files);

        await Assert.That(outcome.Ok).IsTrue();
        await Assert.That(outcome.Problems.Count).IsEqualTo(0);
        await Assert.That(outcome.Text).IsEqualTo("<root>a<p" + Base + "part.xml\"></p>b</root>");
        await Assert.That(files.Requests).IsEquivalentTo(["file:///a/part.xml"]);
    }

    [Test]
    public async Task Xml_ExplicitParseXml_WorksLikeTheDefault()
    {
        var files = new FakeResources().Add("file:///a/part.xml", "<p/>");

        var outcome = Run(Doc(Inc("part.xml", " parse=\"xml\"")), files);

        await Assert.That(outcome.Text).IsEqualTo("<root><p" + Base + "part.xml\"></p></root>");
    }

    [Test]
    public async Task Xml_KeepsCommentsAndInstructionsOfTheIncludedDocument()
    {
        var files = new FakeResources().Add("file:///a/part.xml", "<!--c--><p><q/></p><?pi d?>");

        var outcome = Run(Doc(Inc("part.xml")), files);

        await Assert.That(outcome.Text).IsEqualTo("<root><!--c--><p" + Base + "part.xml\"><q></q></p><?pi d?></root>");
    }

    [Test]
    public async Task Xml_KeepsTheNamespaceOfTheIncludedElement()
    {
        var files = new FakeResources().Add("file:///a/part.xml", "<l:p xmlns:l=\"urn:l\"/>");

        var outcome = Run(Doc(Inc("part.xml")), files);
        var element = FirstChildElement(outcome.Document);

        await Assert.That(outcome.Ok).IsTrue();
        await Assert.That(element.Tree!.Names.GetString(element.Name.NamespaceUri)).IsEqualTo("urn:l");
    }

    [Test]
    public async Task Document_WithoutIncludes_IsCopiedWithCommentsAndInstructions()
    {
        var outcome = Run("<!--top--><root a=\"1\"><!--c--><?p d?>x<e/></root>", new FakeResources());

        await Assert.That(outcome.Ok).IsTrue();
        await Assert.That(outcome.Text).IsEqualTo("<!--top--><root a=\"1\"><!--c--><?p d?>x<e></e></root>");
    }

    [Test]
    public async Task Text_InsertsATextNode()
    {
        var files = new FakeResources().Add("file:///a/t.txt", "hello <not-xml>");

        var outcome = Run(Doc("a" + Inc("t.txt", " parse=\"text\"") + "b"), files);

        await Assert.That(outcome.Ok).IsTrue();
        await Assert.That(outcome.Text).IsEqualTo("<root>ahello <not-xml>b</root>");
        await Assert.That(FirstChildElement(outcome.Document).IsEmpty).IsTrue();
    }

    [Test]
    public async Task Text_EmptyResource_AddsNoNode()
    {
        var files = new FakeResources().Add("file:///a/t.txt", string.Empty);

        var outcome = Run(Doc(Inc("t.txt", " parse=\"text\"")), files);

        await Assert.That(outcome.Text).IsEqualTo("<root></root>");
    }

    [Test]
    public async Task Fallback_IsIgnoredWhenTheResourceLoads()
    {
        var files = new FakeResources().Add("file:///a/part.xml", "<p/>");
        var fallback = Fallback("<f>t<!--c--><?p d?></f>text");

        var outcome = Run(Doc(Inc("part.xml", string.Empty, fallback) + "z"), files);

        await Assert.That(outcome.Text).IsEqualTo("<root><p" + Base + "part.xml\"></p>z</root>");
    }

    [Test]
    public async Task Fallback_IsUsedWhenTheResourceIsMissing()
    {
        var content = "<x/>text" + Fallback("<f a=\"1\">t</f>tail") + "<y/>";

        var outcome = Run(Doc(Inc("none.xml", string.Empty, content)), new FakeResources());

        await Assert.That(outcome.Ok).IsTrue();
        await Assert.That(outcome.Problems.Count).IsEqualTo(0);
        await Assert.That(outcome.Text).IsEqualTo("<root><f a=\"1\">t</f>tail</root>");
    }

    [Test]
    public async Task Fallback_ContentIsProcessedAndMayIncludeAnotherResource()
    {
        var files = new FakeResources().Add("file:///a/other.xml", "<o/>");

        var outcome = Run(Doc(Inc("none.xml", string.Empty, Fallback(Inc("other.xml")))), files);

        await Assert.That(outcome.Ok).IsTrue();
        await Assert.That(outcome.Text).IsEqualTo("<root><o" + Base + "other.xml\"></o></root>");
    }

    [Test]
    public async Task Fallback_NestedIncludeWithoutFallback_ReportsItsOwnError()
    {
        var outcome = Run(Doc(Inc("none.xml", string.Empty, Fallback(Inc("gone.xml")))), new FakeResources());

        await Assert.That(outcome.Ok).IsFalse();
        await Assert.That(outcome.Problems.Count).IsEqualTo(1);
        await Assert.That(outcome.Problems[0].Code).IsEqualTo(CoreDiagnosticCodes.ResourceNotFound);
    }

    [Test]
    public async Task Fallback_IsUsedWhenTheResolverDenies()
    {
        var outcome = Run(Doc(Inc("p.xml", string.Empty, Fallback("<f/>"))), DenyAllResourceResolver.Instance);

        await Assert.That(outcome.Ok).IsTrue();
        await Assert.That(outcome.Text).IsEqualTo("<root><f></f></root>");
    }

    [Test]
    public async Task Fallback_IsUsedForMalformedAndUnresolvableReferences()
    {
        var files = new FakeResources().Add("file:///a/bad.xml", "<p>");
        var fallback = Fallback("<f/>");

        var malformed = Run(Doc(Inc("bad.xml", string.Empty, fallback)), files);
        var noBase = Run(Doc(Inc("x.xml", string.Empty, fallback)), files, systemId: null);
        var invalid = Run(Doc(Inc("%zz", string.Empty, fallback)), files);

        await Assert.That(malformed.Text).IsEqualTo("<root><f></f></root>");
        await Assert.That(noBase.Text).IsEqualTo("<root><f></f></root>");
        await Assert.That(invalid.Text).IsEqualTo("<root><f></f></root>");
    }

    [Test]
    public async Task Base_RelativeReferenceInIncludedContent_ResolvesAgainstTheIncludedDocument()
    {
        // The fixture of the include resolution of issue 202, with the inclusion done by XInclude.
        var files = new FakeResources()
            .Add("file:///a/sub/lib.xml", "<lib xmlns:xi=\"" + Xi + "\"><xi:include href=\"p.xml\"/></lib>")
            .Add("file:///a/sub/p.xml", "<p/>");

        var outcome = Run(Doc(Inc("sub/lib.xml")), files, systemId: "file:///a/root.xml");
        var lib = FirstChildElement(outcome.Document);
        var p = lib.FirstChild;

        await Assert.That(outcome.Ok).IsTrue();
        await Assert.That(files.Requests).IsEquivalentTo(["file:///a/sub/lib.xml", "file:///a/sub/p.xml"]);
        await Assert.That(lib.BaseUri!.AbsoluteUri).IsEqualTo("file:///a/sub/lib.xml");
        await Assert.That(p.BaseUri!.AbsoluteUri).IsEqualTo("file:///a/sub/p.xml");
        await Assert.That(outcome.Document.FirstChild.BaseUri!.AbsoluteUri).IsEqualTo("file:///a/root.xml");
    }

    [Test]
    public async Task Base_AnXmlBaseOfTheIncludedElement_IsResolvedAgainstTheIncludedDocument()
    {
        var files = new FakeResources().Add("file:///a/sub/p.xml", "<p xml:base=\"x/\"/>");

        var outcome = Run(Doc(Inc("sub/p.xml")), files);
        var p = FirstChildElement(outcome.Document);

        await Assert.That(outcome.Ok).IsTrue();
        await Assert.That(p.BaseUri!.AbsoluteUri).IsEqualTo("file:///a/sub/x/");
        await Assert.That(Dump(p)).IsEqualTo("<p base=\"sub/x/\"></p>");
    }

    [Test]
    public async Task Base_OfNestedIncludedContent_IsRelativeToTheIncludingDocument()
    {
        var files = new FakeResources()
            .Add("file:///a/sub/lib.xml", "<lib xmlns:xi=\"" + Xi + "\"><xi:include href=\"../p.xml\"/></lib>")
            .Add("file:///a/p.xml", "<p/>");

        var outcome = Run(Doc(Inc("sub/lib.xml")), files);

        await Assert
            .That(Dump(FirstChildElement(outcome.Document)))
            .IsEqualTo("<lib base=\"sub/lib.xml\"><p base=\"../p.xml\"></p></lib>");
    }

    [Test]
    public async Task Base_AnAbsoluteXmlBaseOfTheIncludedElement_IsKept()
    {
        var files = new FakeResources().Add("file:///a/p.xml", "<p xml:base=\"http://other.example/y/\"/>");

        var outcome = Run(Doc(Inc("p.xml")), files);

        await Assert
            .That(FirstChildElement(outcome.Document).BaseUri!.AbsoluteUri)
            .IsEqualTo("http://other.example/y/");
    }

    [Test]
    public async Task Positions_OfIncludedElements_AreThePositionOfTheXIncludeElement()
    {
        var files = new FakeResources()
            .Add("file:///a/lib.xml", "\n\n<lib xmlns:xi=\"" + Xi + "\">\n<xi:include href=\"p.xml\"/></lib>")
            .Add("file:///a/p.xml", "\n\n\n<p/>");

        var outcome = Run(Doc("\n  " + Inc("lib.xml")), files);
        var tree = (XmlDocumentTree)outcome.Document.Tree!;
        var lib = FirstChildElement(outcome.Document);

        await Assert.That(tree.GetPosition(lib.Index).Line).IsEqualTo(2);
        await Assert.That(tree.GetPosition(lib.Index).Column).IsEqualTo(3);
        await Assert.That(tree.GetPosition(lib.FirstChild.NextSibling.Index).Line).IsEqualTo(2);
        await Assert.That(tree.GetPosition(lib.FirstChild.NextSibling.Index).Column).IsEqualTo(3);
    }

    [Test]
    public async Task Document_Uri_IsTheSystemIdWhenItIsAbsolute()
    {
        var absolute = Run("<root/>", new FakeResources());
        var none = Run("<root/>", new FakeResources(), systemId: null);

        await Assert.That(absolute.Document.DocumentUri!.AbsoluteUri).IsEqualTo(RootUri);
        await Assert.That(none.Document.DocumentUri).IsNull();
    }

    [Test]
    public async Task Attributes_OfIncludeAndFallbackThatXIncludeDoesNotReadAreIgnored()
    {
        var files = new FakeResources().Add("file:///a/p.xml", "<p/>");
        var fallback = "<xi:fallback id=\"x\">" + Inc("never.xml") + "</xi:fallback>";

        var loaded = Run(Doc(Inc("p.xml", " xml:lang=\"en\"", fallback)), files);
        var missing = Run(Doc(Inc("none.xml", " xml:lang=\"en\"", "<xi:fallback id=\"x\"><f/></xi:fallback>")), files);

        await Assert.That(loaded.Text).IsEqualTo("<root><p" + Base + "p.xml\"></p></root>");
        await Assert.That(files.Requests).IsEquivalentTo(["file:///a/p.xml", "file:///a/none.xml"]);
        await Assert.That(missing.Text).IsEqualTo("<root><f></f></root>");
    }
}
