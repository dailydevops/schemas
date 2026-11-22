namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Tree;

using System;
using System.Text;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.Xml.Model.Loading;
using NetEvolve.Schemas.Xml.Model.Traversal;
using NetEvolve.Schemas.Xsd.Compilation;
using NetEvolve.Schemas.Xsd.Validation.Assembly;
using NetEvolve.Schemas.Xsd.Validation.Tree;
using XsdVersion = NetEvolve.Schemas.Xml.Datatypes.Contracts.XsdVersion;

/// <summary>A schema without a namespace, a streaming validator, a validation core, and trees of instances, for the tests of tree validation.</summary>
internal static class TreeFixture
{
    public const string InstanceId = "file:///i.xml";

    /// <summary>A root with integer items and an optional group; the group has an ID and an IDREF attribute, and the root a defaulted attribute.</summary>
    public const string Schema = """
        <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
          <xs:element name="root">
            <xs:complexType>
              <xs:sequence>
                <xs:element name="item" type="xs:integer" maxOccurs="unbounded"/>
                <xs:element ref="group" minOccurs="0"/>
              </xs:sequence>
              <xs:attribute name="a" type="xs:int" default="7"/>
            </xs:complexType>
          </xs:element>
          <xs:element name="group">
            <xs:complexType>
              <xs:sequence>
                <xs:element name="inner" type="xs:int" maxOccurs="unbounded"/>
              </xs:sequence>
              <xs:attribute name="id" type="xs:ID"/>
              <xs:attribute name="ref" type="xs:IDREF"/>
            </xs:complexType>
          </xs:element>
        </xs:schema>
        """;

    /// <summary>Instances that the streaming path judges differently: valid, a bad value, missing content, an unknown child, an unknown attribute, an unresolved IDREF, and an undeclared root.</summary>
    public static readonly string[] Instances =
    [
        "<root><item>1</item><group id=\"g\" ref=\"g\"><inner>2</inner></group></root>",
        "<root>\n  <item>x</item>\n  <item>2</item>\n</root>",
        "<root/>",
        "<root><item>1</item>\n<bad/></root>",
        "<root b=\"1\"><item>1</item></root>",
        "<root><item>1</item><group ref=\"nope\"><inner>1</inner></group></root>",
        "<other/>",
    ];

    public static IXsdValidator Streaming(string schema = Schema) =>
        SchemaValidator.ForXsd().WithVersion(XsdVersion.V11).AddSchema(schema.AsMemory(), "file:///s.xsd").Build();

    public static ValidationCore Core(string schema = Schema) =>
        new(
            new SchemaCompiler(XsdVersion.V11, DenyAllResourceResolver.Instance, ValidationLimits.Default).Compile([
                SchemaInput.FromChars(schema.AsMemory(), "file:///s.xsd"),
            ]),
            XsdVersion.V11,
            false,
            ValidationLimits.Default
        );

    public static TreeValidator Validator(string schema = Schema) => new(Core(schema));

    public static XmlDocumentTree Load(string xml) => XmlDocumentLoader.Load(xml.AsSpan(), null, new Uri(InstanceId));

    /// <summary>Copies an element subtree into a document of its own, which keeps the positions of the elements.</summary>
    public static XmlDocumentTree OwnDocument(XmlNode element)
    {
        var builder = new XmlTreeBuilder(element.Tree!.Names, element.DocumentUri);
        try
        {
            builder.StartDocument(default);
            XmlTreePump.Pump(element, builder);
            builder.EndDocument();
            return builder.Complete();
        }
        finally
        {
            builder.Dispose();
        }
    }

    /// <summary>Gets the n-th element with the local name, in document order, <see langword="default"/> for none.</summary>
    public static XmlNode Element(XmlTree tree, string local, int skip = 0)
    {
        var node = tree.Document;
        while (true)
        {
            if (
                node.Kind == XmlNodeKind.Element
                && string.Equals(tree.Names.GetString(node.Name.Local), local, StringComparison.Ordinal)
                && skip-- == 0
            )
            {
                return node;
            }

            var child = node.FirstChild;
            if (!child.IsEmpty)
            {
                node = child;
                continue;
            }

            while (node.NextSibling.IsEmpty)
            {
                node = node.Parent;
                if (node.IsEmpty)
                {
                    return default;
                }
            }

            node = node.NextSibling;
        }
    }

    public static string Describe(ValidationResult result)
    {
        var text = new StringBuilder();
        foreach (var issue in result.Issues)
        {
            _ = text.Append(issue.Code)
                .Append('|')
                .Append(issue.Severity)
                .Append('|')
                .Append(issue.Constraint)
                .Append('|')
                .Append(issue.Message)
                .Append('|')
                .Append(issue.Location.Source)
                .Append('|')
                .Append(issue.Location.Line)
                .Append(':')
                .Append(issue.Location.Column)
                .Append('\n');
        }

        return text.Append(result.IsValid).Append(result.IsTruncated).ToString();
    }
}
