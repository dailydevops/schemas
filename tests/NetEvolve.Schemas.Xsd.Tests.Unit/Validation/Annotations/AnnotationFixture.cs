namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Annotations;

using System;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.Xml.Reader.Pipeline;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Values;
using NetEvolve.Schemas.Xsd.Attributes.Declarations;
using NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Elements;
using NetEvolve.Schemas.Xsd.SimpleTypes.Definitions;
using NetEvolve.Schemas.Xsd.Tests.Unit.SimpleTypes.Definitions;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Ids;
using NetEvolve.Schemas.Xsd.Validation.Annotations;
using NetEvolve.Schemas.Xsd.Validation.Core;
using NetEvolve.Schemas.Xsd.Wildcards.Core;

/// <summary>
/// A root <c>r</c> that holds any number of children, each with another type: <c>i</c> (xs:integer), <c>l</c> (a list of
/// integers), <c>u</c> (a union of integer and boolean), <c>c</c> (a named type with element-only content, child <c>n</c>),
/// <c>m</c> (mixed), <c>e</c> (empty), <c>sc</c> (simple content of integer), <c>w</c> (empty with the attributes <c>a</c>
/// integer, <c>s</c> string, and <c>d</c> integer with the default 9), <c>ri</c> (an anonymous restriction of integer),
/// <c>rn</c> (the named restriction <c>urn:t:small</c> of integer), and the built-in lists <c>nm</c>, <c>ids</c>, and
/// <c>ents</c>, plus <c>weird</c> (a datatype that no built-in knows) and <c>any</c> (xs:anySimpleType).
/// </summary>
internal static class AnnotationFixture
{
    public const string Xs = SchemaFixture.Xs;

    public static SchemaFixture Schema()
    {
        var schema = new SchemaFixture();
        schema.Global(
            "r",
            schema.ElementOnly(SchemaFixture.Group(ModelGroupCompositor.Choice, 0, long.MaxValue, Parts(schema)))
        );
        return schema;
    }

    private static Particle[] Parts(SchemaFixture schema)
    {
        var named = ComplexTypeDefinition.Implicit(
            SchemaFixture.Header("Named"),
            false,
            SchemaFixture.Seq(SchemaFixture.Part("n", SchemaFixture.Simple("integer"))),
            [],
            null,
            schema.Reporter
        );
        var small = SimpleTypeDefinition.Restrict(
            new XPathQName("urn:t", "small"),
            SchemaFixture.Where,
            XsdDerivationMethod.None,
            SchemaFixture.Simple("integer"),
            [],
            schema.Reporter
        )!;
        var attributes = schema.Empty(
            schema.Use("a", "integer"),
            schema.Use("s", "string"),
            schema.Use(
                "d",
                "integer",
                constraint: new AttributeValueConstraint(AttributeValueConstraintKind.Default, "9")
            )
        );
        var weird = SimpleTypeDefinition.FromDatatype(
            new XPathQName("urn:t", "weird"),
            SchemaFixture.Where,
            new FakeDatatype("weird", DatatypeVariety.Atomic)
        );
        return
        [
            SchemaFixture.Part("i", SchemaFixture.Simple("integer")),
            SchemaFixture.Part("l", IdFixture.ListOf(SchemaFixture.Simple("integer"), schema)),
            SchemaFixture.Part(
                "u",
                IdFixture.UnionOf(schema, SchemaFixture.Simple("integer"), SchemaFixture.Simple("boolean"))
            ),
            SchemaFixture.Part("c", named),
            SchemaFixture.Part("m", schema.Mixed(SchemaFixture.Seq())),
            SchemaFixture.Part("e", schema.Empty()),
            SchemaFixture.Part("sc", schema.SimpleContent("integer")),
            SchemaFixture.Part("w", attributes),
            SchemaFixture.Part("ri", IdFixture.Restriction(SchemaFixture.Simple("integer"), schema)),
            SchemaFixture.Part("rn", small),
            SchemaFixture.Part("nm", BuiltInList("NMTOKENS")),
            SchemaFixture.Part("ids", BuiltInList("IDREFS")),
            SchemaFixture.Part("ents", BuiltInList("ENTITIES")),
            SchemaFixture.Part("weird", weird),
            SchemaFixture.Part("any", SchemaFixture.Simple("anySimpleType")),
        ];
    }

    /// <summary>A root <c>r</c> that holds any number of elements of any declared or undeclared name, assessed lax; <c>k</c> is an integer.</summary>
    public static SchemaFixture LaxSchema()
    {
        var schema = new SchemaFixture();
        schema.Global(
            "r",
            schema.ElementOnly(
                SchemaFixture.Seq(
                    SchemaFixture.Part(SchemaFixture.Wild("##any", XsdProcessContents.Lax), 0, long.MaxValue)
                )
            )
        );
        schema.Global("k", SchemaFixture.Simple("integer"));
        return schema;
    }

    public static SimpleTypeDefinition BuiltInList(string name) =>
        SimpleTypeDefinition.FromDatatype(
            new XPathQName(Xs, name),
            SchemaFixture.Where,
            new FakeDatatype(name, DatatypeVariety.List)
        );

    public static AnnotatedRun Run(SchemaFixture schema, string xml) => AnnotatedRun.Create(schema, xml);

    /// <summary>The same document as a tree, with the table that a validation run filled.</summary>
    internal sealed class AnnotatedRun
    {
        private AnnotatedRun(AnnotationTable table, XmlDocumentTree tree, AnnotationSource source)
        {
            Table = table;
            Tree = tree;
            Source = source;
        }

        public AnnotationTable Table { get; }

        public XmlDocumentTree Tree { get; }

        public AnnotationSource Source { get; }

        public static AnnotatedRun Create(SchemaFixture schema, string xml)
        {
            var assessor = new DescriptorValueAssessor(XsdVersion.V11);
            var table = new AnnotationTable();
            _ = ValidationHarness.Run(schema, xml, assessor, new AnnotationListener(table));
            var tree = Build(xml);
            return new AnnotatedRun(table, tree, new AnnotationSource(table, tree, assessor));
        }

        public static XmlDocumentTree Build(string xml)
        {
            var builder = new XmlTreeBuilder();
            try
            {
                using var reader = XmlPipelineReader.Create(xml.AsSpan());
                var status = reader.Pump(ref builder);
                return status == XmlReadStatus.Completed
                    ? builder.Complete()
                    : throw new InvalidOperationException("The instance is not well-formed.");
            }
            finally
            {
                builder.Dispose();
            }
        }

        /// <summary>Gets the first element with the local name, in document order.</summary>
        public XmlNode Element(string local)
        {
            var found = Find(Tree.Document, local);
            return found.IsEmpty ? throw new InvalidOperationException("No element " + local) : found;
        }

        /// <summary>Gets the attribute with the local name.</summary>
        public XmlNode Attribute(XmlNode element, string local)
        {
            for (var a = element.FirstAttribute; !a.IsEmpty; a = a.NextAttribute)
            {
                if (string.Equals(Tree.Names.GetString(a.Name.Local), local, StringComparison.Ordinal))
                {
                    return a;
                }
            }

            throw new InvalidOperationException("No attribute " + local);
        }

        public XsdAnnotation Annotation(XmlNode node)
        {
            var found = Source.TryGetAnnotation(node, out var annotation);
            return found ? annotation : throw new InvalidOperationException("The node is not annotated.");
        }

        public XsdAnnotation Annotation(string local) => Annotation(Element(local));

        public XdmItem[] TypedValue(XsdAnnotation annotation)
        {
            var items = new XdmItem[annotation.TypedValueCount];
            _ = annotation.CopyTypedValue(items);
            return items;
        }

        private XmlNode Find(XmlNode node, string local)
        {
            if (
                node.Kind == XmlNodeKind.Element
                && string.Equals(Tree.Names.GetString(node.Name.Local), local, StringComparison.Ordinal)
            )
            {
                return node;
            }

            for (var child = node.FirstChild; !child.IsEmpty; child = child.NextSibling)
            {
                var found = Find(child, local);
                if (!found.IsEmpty)
                {
                    return found;
                }
            }

            return default;
        }
    }
}
