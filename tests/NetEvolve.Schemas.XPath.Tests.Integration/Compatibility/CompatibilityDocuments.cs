namespace NetEvolve.Schemas.XPath.Tests.Integration.Compatibility;

using System.Text;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.Xml.Reader.Pipeline;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>
/// The fixed documents of the differential tests, by key. They carry no whitespace between elements, so that the library and
/// System.Xml.XPath see the same text nodes, and no DTD.
/// </summary>
internal static class CompatibilityDocuments
{
    /// <summary>Three items with attributes, an empty element, and a total.</summary>
    public const string Shop =
        "<shop xml:lang=\"en\">"
        + "<item id=\"i1\" price=\"10\" stock=\"5\"><name>Pen</name><tags>a b</tags></item>"
        + "<item id=\"i2\" price=\"25.5\" stock=\"0\"><name>Book</name></item>"
        + "<item id=\"i3\" price=\"7\"><name>Cup</name><note>  spaced   text </note></item>"
        + "<empty/><total>42.5</total></shop>";

    /// <summary>Four values that read as numbers, one of them with surrounding white space.</summary>
    public const string Clean = "<c><v>1</v><v>2.5</v><v> 3 </v><v>-4</v></c>";

    /// <summary>Values that XPath 1.0 and XPath 2.0 read differently, and strings of numbers and booleans.</summary>
    public const string Numbers =
        "<n><v>1</v><v>x</v><v>+4</v><v>1e2</v><v/><w>0</w><w>1</w><w>true</w><w>false</w><s>abc</s><s>10</s><s>9</s></n>";

    /// <summary>A namespace declaration, text around a comment and a processing instruction, and nested empty elements.</summary>
    public const string Mixed =
        "<doc xmlns:p=\"urn:p\" id=\"d1\"><p:a>1</p:a><p:a>2</p:a><b attr=\"3\">x<!--c-->y<?pi data?></b><c><d/><d/></c></doc>";

    /// <summary>Gets the text of a document by key.</summary>
    /// <param name="key">One of <c>shop</c>, <c>clean</c>, <c>numbers</c>, and <c>mixed</c>.</param>
    /// <returns>The document text.</returns>
    public static string Text(string key) =>
        key switch
        {
            "shop" => Shop,
            "clean" => Clean,
            "numbers" => Numbers,
            "mixed" => Mixed,
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, "Unknown document key."),
        };

    /// <summary>Builds the tree of a document by the library reader.</summary>
    /// <param name="key">The document key.</param>
    /// <returns>The document node.</returns>
    public static XPathNode Load(string key)
    {
        using var reader = XmlPipelineReader.Create(Encoding.UTF8.GetBytes(Text(key)));
        using var builder = new XmlTreeBuilder();
        var sink = builder;
        return reader.Pump(ref sink) == XmlReadStatus.Completed
            ? new XPathNode(builder.Complete().Document)
            : throw new InvalidOperationException("The test document '" + key + "' is not well formed.");
    }
}
