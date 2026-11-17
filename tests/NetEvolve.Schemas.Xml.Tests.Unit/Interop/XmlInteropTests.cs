namespace NetEvolve.Schemas.Xml.Tests.Unit.Interop;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Interop;
using NetEvolve.Schemas.Xml.Model;
using XmlNode = NetEvolve.Schemas.Xml.Contracts.XmlNode;

public class XmlInteropTests
{
    // Line 2 starts with the root, line 3 with the child, and line 4 has the comment, the processing instruction, text, and e.
    private const string Source =
        "<?xml version=\"1.0\"?>\n"
        + "<root xmlns=\"urn:d\" xmlns:p=\"urn:p\" id=\"1\" p:lang=\"en\" b=\"2\">\n"
        + "  <p:child xmlns:p=\"urn:q\" a=\"x\"/>\n"
        + "  <!--c--><?pi data?>t<e/>\n"
        + "</root>";

    private const string ExpectedDump =
        "D[<root{urn:d} ns[=urn:d,p=urn:p] id=\"1\" p:lang{urn:p}=\"en\" b=\"2\">T(\n  )"
        + "<p:child{urn:q} ns[=urn:d,p=urn:q] a=\"x\"></>T(\n  )C(c)P(pi,data)T(t)"
        + "<e{urn:d} ns[=urn:d,p=urn:p]></>T(\n)</>]";

    private const string DoctypeSource = "<!DOCTYPE a [<!ELEMENT a EMPTY>]><a/>";

    private static readonly XmlInteropOptions AllowDoctype = new(allowDocumentType: true);

    private static XmlReader Reader(string xml) =>
        XmlReader.Create(new StringReader(xml), new XmlReaderSettings { DtdProcessing = DtdProcessing.Parse });

    private static XDocument LoadX(string xml) =>
        XDocument.Parse(xml, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

    private static XmlDocument LoadDom(string xml)
    {
        var document = new XmlDocument { PreserveWhitespace = true, XmlResolver = null };
        document.LoadXml(xml);
        return document;
    }

    private static string Dump(XmlTree tree)
    {
        var builder = new StringBuilder();
        Append(builder, tree.Document, tree);
        return builder.ToString();
    }

    private static string Q(XmlName name, XmlTree tree)
    {
        var prefix = tree.Names.GetString(name.Prefix);
        var ns = tree.Names.GetString(name.NamespaceUri);
        return (prefix.Length == 0 ? string.Empty : prefix + ":")
            + tree.Names.GetString(name.Local)
            + (ns.Length == 0 ? string.Empty : "{" + ns + "}");
    }

    private static void Append(StringBuilder sb, XmlNode node, XmlTree tree)
    {
        switch (node.Kind)
        {
            case XmlNodeKind.Document:
                _ = sb.Append("D[");
                AppendChildren(sb, node, tree);
                _ = sb.Append(']');
                break;
            case XmlNodeKind.Element:
                _ = sb.Append('<').Append(Q(node.Name, tree)).Append(" ns[");
                var bindings = new List<string>();
                for (var ns = node.FirstNamespace; !ns.IsEmpty; ns = ns.NextNamespace)
                {
                    bindings.Add(tree.Names.GetString(ns.Name.Local) + "=" + ns.StringValue);
                }

                bindings.Sort(StringComparer.Ordinal);
                _ = sb.Append(string.Join(",", bindings)).Append(']');
                for (var a = node.FirstAttribute; !a.IsEmpty; a = a.NextAttribute)
                {
                    _ = sb.Append(' ').Append(Q(a.Name, tree)).Append("=\"").Append(a.StringValue).Append('"');
                }

                _ = sb.Append('>');
                AppendChildren(sb, node, tree);
                _ = sb.Append("</>");
                break;
            case XmlNodeKind.Text:
                _ = sb.Append("T(").Append(node.StringValue).Append(')');
                break;
            case XmlNodeKind.Comment:
                _ = sb.Append("C(").Append(node.StringValue).Append(')');
                break;
            case XmlNodeKind.ProcessingInstruction:
                _ = sb.Append("P(")
                    .Append(tree.Names.GetString(node.Name.Local))
                    .Append(',')
                    .Append(node.StringValue)
                    .Append(')');
                break;
            default:
                break;
        }
    }

    private static void AppendChildren(StringBuilder sb, XmlNode node, XmlTree tree)
    {
        for (var child = node.FirstChild; !child.IsEmpty; child = child.NextSibling)
        {
            Append(sb, child, tree);
        }
    }

    private static XmlPosition PositionOf(XmlTree tree, string local)
    {
        var found = FindElement(tree.Document, tree, local);
        return ((XmlDocumentTree)tree).GetPosition(found.Index);
    }

    private static XmlNode FindElement(XmlNode node, XmlTree tree, string local)
    {
        for (var child = node.FirstChild; !child.IsEmpty; child = child.NextSibling)
        {
            if (child.Kind != XmlNodeKind.Element)
            {
                continue;
            }

            if (string.Equals(tree.Names.GetString(child.Name.Local), local, StringComparison.Ordinal))
            {
                return child;
            }

            var nested = FindElement(child, tree, local);
            if (!nested.IsEmpty)
            {
                return nested;
            }
        }

        return default;
    }

    private static async Task AssertPositions(XmlTree tree)
    {
        await Assert.That(PositionOf(tree, "root")).IsEqualTo(new XmlPosition(2, 1, 0));
        await Assert.That(PositionOf(tree, "child")).IsEqualTo(new XmlPosition(3, 3, 0));
        await Assert.That(PositionOf(tree, "e")).IsEqualTo(new XmlPosition(4, 23, 0));

        // The sink contract delivers positions on StartElement only (#108), so an attribute has no position of its own.
        var root = FindElement(tree.Document, tree, "root");
        await Assert
            .That(((XmlDocumentTree)tree).GetPosition(root.FirstAttribute.Index))
            .IsEqualTo(default(XmlPosition));
    }

    [Test]
    public async Task FromXmlReader_MapsNamespacesAttributesAndNodesInOrder()
    {
        using var reader = Reader(Source);

        var tree = XmlInterop.FromXmlReader(reader);

        await Assert.That(Dump(tree)).IsEqualTo(ExpectedDump);
    }

    [Test]
    public async Task FromXDocument_MapsNamespacesAttributesAndNodesInOrder()
    {
        var tree = XmlInterop.FromXDocument(LoadX(Source));

        await Assert.That(Dump(tree)).IsEqualTo(ExpectedDump);
    }

    [Test]
    public async Task FromXmlDocument_MapsNamespacesAttributesAndNodesInOrder()
    {
        var tree = XmlInterop.FromXmlDocument(LoadDom(Source));

        await Assert.That(Dump(tree)).IsEqualTo(ExpectedDump);
    }

    [Test]
    public async Task XmlReader_RoundTrip_IsNodeForNodeEqual()
    {
        using var reader = Reader(Source);
        var tree = XmlInterop.FromXmlReader(reader);

        using var back = XmlInterop.ToXmlReader(tree.Document);
        var again = XDocument.Load(back, LoadOptions.PreserveWhitespace);
        var expected = LoadX(Source);

        await Assert.That(XNode.DeepEquals(again.Root, expected.Root)).IsTrue();
        await Assert
            .That(again.Root!.ToString(SaveOptions.DisableFormatting))
            .IsEqualTo(expected.Root!.ToString(SaveOptions.DisableFormatting));
    }

    [Test]
    public async Task XDocument_RoundTrip_IsNodeForNodeEqual()
    {
        var source = LoadX(Source);

        var again = XmlInterop.ToXDocument(XmlInterop.FromXDocument(source).Document);

        await Assert.That(XNode.DeepEquals(again.Root, source.Root)).IsTrue();
        await Assert
            .That(again.Root!.ToString(SaveOptions.DisableFormatting))
            .IsEqualTo(source.Root!.ToString(SaveOptions.DisableFormatting));
    }

    [Test]
    public async Task XmlDocument_RoundTrip_IsNodeForNodeEqual()
    {
        var source = LoadDom(Source);

        var again = XmlInterop.ToXmlDocument(XmlInterop.FromXmlDocument(source).Document);

        await Assert.That(again.DocumentElement!.OuterXml).IsEqualTo(source.DocumentElement!.OuterXml);
    }

    [Test]
    public async Task ModelAsXmlReader_LoadsEqualInXDocumentAndXmlDocument()
    {
        var tree = XmlInterop.FromXDocument(LoadX(Source));

        using var forX = XmlInterop.ToXmlReader(tree.Document);
        var x = XDocument.Load(forX, LoadOptions.PreserveWhitespace);
        var dom = new XmlDocument { PreserveWhitespace = true };
        using var forDom = XmlInterop.ToXmlReader(tree.Document);
        dom.Load(forDom);

        await Assert.That(Dump(XmlInterop.FromXDocument(x))).IsEqualTo(ExpectedDump);
        await Assert.That(Dump(XmlInterop.FromXmlDocument(dom))).IsEqualTo(ExpectedDump);
    }

    [Test]
    public async Task ToXDocument_OfElementNode_CarriesTheNamespacesInScope()
    {
        var tree = XmlInterop.FromXDocument(LoadX("<r xmlns:p=\"urn:p\" xmlns=\"urn:d\"><p:a><b/></p:a></r>"));
        var a = FindElement(tree.Document, tree, "a");

        var document = XmlInterop.ToXDocument(a);

        await Assert.That(document.Root!.Name).IsEqualTo(XNamespace.Get("urn:p") + "a");
        await Assert.That(document.Root!.GetNamespaceOfPrefix("p")).IsEqualTo(XNamespace.Get("urn:p"));
        await Assert.That(document.Root!.GetDefaultNamespace()).IsEqualTo(XNamespace.Get("urn:d"));
        await Assert.That(document.Root!.Element(XNamespace.Get("urn:d") + "b")).IsNotNull();
    }

    [Test]
    public async Task FromXDocument_ElementInNamespaceWithoutDeclaration_GetsADeclaration()
    {
        XNamespace ns = "urn:n";
        var document = new XDocument(new XElement(ns + "a", new XAttribute(ns + "x", "1"), new XElement(ns + "b")));

        var tree = XmlInterop.FromXDocument(document);
        var again = XmlInterop.ToXDocument(tree.Document);

        await Assert.That(again.Root!.Name).IsEqualTo(ns + "a");
        await Assert.That(again.Root!.Attribute(ns + "x")!.Value).IsEqualTo("1");
        await Assert.That(again.Root!.Element(ns + "b")).IsNotNull();
    }

    [Test]
    public async Task FromXmlReader_RecordsElementPositionsAtTheStartTag()
    {
        using var reader = Reader(Source);

        var tree = XmlInterop.FromXmlReader(reader);

        await AssertPositions(tree);
    }

    [Test]
    public async Task FromXDocument_RecordsElementPositionsAtTheStartTag()
    {
        var tree = XmlInterop.FromXDocument(LoadX(Source));

        await AssertPositions(tree);
    }

    [Test]
    public async Task FromXDocument_WithoutLineInformation_HasNoPositions()
    {
        var tree = XmlInterop.FromXDocument(XDocument.Parse(Source));

        await Assert.That(PositionOf(tree, "root")).IsEqualTo(default(XmlPosition));
    }

    [Test]
    public async Task FromXmlDocument_HasNoPositions()
    {
        var tree = XmlInterop.FromXmlDocument(LoadDom(Source));

        await Assert.That(PositionOf(tree, "root")).IsEqualTo(default(XmlPosition));
    }

    [Test]
    public async Task FromXmlReader_WithDocumentType_IsRejectedByDefault()
    {
        using var reader = Reader(DoctypeSource);

        var failure = Assert.Throws<XmlParseException>(() => XmlInterop.FromXmlReader(reader));

        await Assert.That(failure.Diagnostic!.Code).IsEqualTo(XmlErrorCodes.XML2001);
    }

    [Test]
    public async Task FromXDocument_WithDocumentType_IsRejectedByDefault()
    {
        var failure = Assert.Throws<XmlParseException>(() => XmlInterop.FromXDocument(LoadX(DoctypeSource)));

        await Assert.That(failure.Diagnostic!.Code).IsEqualTo(XmlErrorCodes.XML2001);
    }

    [Test]
    public async Task FromXmlDocument_WithDocumentType_IsRejectedByDefault()
    {
        var failure = Assert.Throws<XmlParseException>(() => XmlInterop.FromXmlDocument(LoadDom(DoctypeSource)));

        await Assert.That(failure.Diagnostic!.Code).IsEqualTo(XmlErrorCodes.XML2001);
    }

    [Test]
    public async Task FromXmlReader_WithDocumentType_IsAcceptedWithTheOption()
    {
        using var reader = Reader(DoctypeSource);

        var tree = XmlInterop.FromXmlReader(reader, AllowDoctype);

        await Assert.That(Dump(tree)).IsEqualTo("D[<a ns[]></>]");
    }

    [Test]
    public async Task FromXDocument_WithDocumentType_IsAcceptedWithTheOption()
    {
        var tree = XmlInterop.FromXDocument(LoadX(DoctypeSource), AllowDoctype);

        await Assert.That(Dump(tree)).IsEqualTo("D[<a ns[]></>]");
    }

    [Test]
    public async Task FromXmlDocument_WithDocumentType_IsAcceptedWithTheOption()
    {
        var tree = XmlInterop.FromXmlDocument(LoadDom(DoctypeSource), AllowDoctype);

        await Assert.That(Dump(tree)).IsEqualTo("D[<a ns[]></>]");
    }

    [Test]
    public async Task CDataAndDeclaration_AreMergedIntoTextInEverySource()
    {
        const string xml = "<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"no\"?><a><![CDATA[x<y]]>z<?p?></a>";
        using var reader = Reader(xml);

        await Assert.That(Dump(XmlInterop.FromXmlReader(reader))).IsEqualTo("D[<a ns[]>T(x<yz)P(p,)</>]");
        await Assert.That(Dump(XmlInterop.FromXDocument(LoadX(xml)))).IsEqualTo("D[<a ns[]>T(x<yz)P(p,)</>]");
        await Assert.That(Dump(XmlInterop.FromXmlDocument(LoadDom(xml)))).IsEqualTo("D[<a ns[]>T(x<yz)P(p,)</>]");
    }

    [Test]
    public async Task ToXmlDocument_OfElementNode_CarriesTheNamespacesInScope()
    {
        var tree = XmlInterop.FromXmlDocument(LoadDom("<r xmlns:p=\"urn:p\" xmlns=\"urn:d\"><p:a><b/></p:a></r>"));

        var document = XmlInterop.ToXmlDocument(FindElement(tree.Document, tree, "a"));

        await Assert
            .That(document.DocumentElement!.OuterXml)
            .IsEqualTo("<p:a xmlns:p=\"urn:p\" xmlns=\"urn:d\"><b /></p:a>");
    }

    [Test]
    public async Task FromXmlReader_FragmentWithoutDocumentElement_IsAccepted()
    {
        using var reader = XmlReader.Create(
            new StringReader("<a/><b/>"),
            new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Fragment }
        );

        await Assert.That(Dump(XmlInterop.FromXmlReader(reader))).IsEqualTo("D[<a ns[]></><b ns[]></>]");
    }

    [Test]
    public async Task Adapters_RejectNullArguments()
    {
        _ = Assert.Throws<ArgumentNullException>(() => XmlInterop.FromXmlReader(null!));
        _ = Assert.Throws<ArgumentNullException>(() => XmlInterop.FromXDocument(null!));
        _ = Assert.Throws<ArgumentNullException>(() => XmlInterop.FromXmlDocument(null!));
        _ = Assert.Throws<ArgumentNullException>(() => XmlInterop.ToXmlReader(default));
        _ = Assert.Throws<ArgumentNullException>(() => XmlInterop.ToXDocument(default));
        _ = Assert.Throws<ArgumentNullException>(() => XmlInterop.ToXmlDocument(default));
        await Task.CompletedTask;
    }
}
