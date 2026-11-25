namespace NetEvolve.Schemas.Xml.Tests.Integration.Hostile;

using System.Text;
using NetEvolve.Schemas.Xml.Contracts;

public static partial class HostileCorpus
{
    private static string Attributes(int count)
    {
        var builder = new StringBuilder("<r");
        for (var index = 0; index < count; index++)
        {
            _ = builder.Append(" a").Append(index).Append("=\"v\"");
        }

        return builder.Append("/>").ToString();
    }

    private static string NamespaceDeclarations(int count)
    {
        var builder = new StringBuilder("<r");
        for (var index = 0; index < count; index++)
        {
            _ = builder.Append(" xmlns:p").Append(index).Append("=\"urn:n").Append(index).Append('"');
        }

        return builder.Append("/>").ToString();
    }

    private static string WideStartTag(int attributes, int valueLength)
    {
        var value = Repeat("x", valueLength);
        var builder = new StringBuilder("<r");
        for (var index = 0; index < attributes; index++)
        {
            _ = builder.Append(" a").Append(index).Append("=\"").Append(value).Append('"');
        }

        return builder.Append("/>").ToString();
    }

    private static string NestedPrefixed(int depth)
    {
        var builder = new StringBuilder();
        for (var index = 0; index < depth; index++)
        {
            _ = builder.Append("<p:a xmlns:p=\"urn:n\">");
        }

        return builder.ToString();
    }

    private static readonly string LongName = Repeat("n", 1_025);
    private static readonly XmlReaderLimits TightText = XmlReaderLimits.Default.WithMaxTextNodeLength(100);

    private static IEnumerable<HostileCase> Nesting()
    {
        yield return new HostileCase(
            "nesting-257-elements-open",
            HostileCategory.DeepNesting,
            XmlErrorCodes.XML1102,
            () => Repeat("<a>", 257)
        );
        yield return new HostileCase(
            "nesting-257-elements-closed",
            HostileCategory.DeepNesting,
            XmlErrorCodes.XML1102,
            () => Repeat("<a>", 257) + Repeat("</a>", 257)
        );
        yield return new HostileCase(
            "nesting-100000-elements-closed",
            HostileCategory.DeepNesting,
            XmlErrorCodes.XML1102,
            () => Repeat("<a>", 100_000) + Repeat("</a>", 100_000)
        );
        yield return new HostileCase(
            "nesting-1000000-elements-open",
            HostileCategory.DeepNesting,
            XmlErrorCodes.XML1102,
            () => Repeat("<a>", 1_000_000)
        );
        yield return new HostileCase(
            "nesting-300-prefixed-elements-with-declarations",
            HostileCategory.DeepNesting,
            XmlErrorCodes.XML1102,
            () => NestedPrefixed(300)
        );
        yield return new HostileCase(
            "nesting-depth-limit-of-one",
            HostileCategory.DeepNesting,
            XmlErrorCodes.XML1102,
            () => "<a><b/></a>",
            Limits: XmlReaderLimits.Default.WithMaxElementDepth(1)
        );
    }

    private static IEnumerable<HostileCase> HugeNames()
    {
        yield return new HostileCase(
            "name-element-1025-characters",
            HostileCategory.HugeName,
            XmlErrorCodes.XML1105,
            () => $"<{LongName}/>"
        );
        yield return new HostileCase(
            "name-attribute-1025-characters",
            HostileCategory.HugeName,
            XmlErrorCodes.XML1105,
            () => $"<r {LongName}=\"v\"/>"
        );
        yield return new HostileCase(
            "name-end-tag-1025-characters",
            HostileCategory.HugeName,
            XmlErrorCodes.XML1105,
            () => $"<r></{LongName}>"
        );
        yield return new HostileCase(
            "name-prefix-1025-characters",
            HostileCategory.HugeName,
            XmlErrorCodes.XML1105,
            () => $"<{LongName}:r xmlns:{LongName}=\"urn:n\"/>"
        );
        yield return new HostileCase(
            "name-processing-instruction-target-1025-characters",
            HostileCategory.HugeName,
            XmlErrorCodes.XML1105,
            () => $"<r><?{LongName} data?></r>"
        );
        yield return new HostileCase(
            "name-reference-1025-characters",
            HostileCategory.HugeName,
            XmlErrorCodes.XML1105,
            () => $"<r>&{LongName};</r>"
        );
        yield return new HostileCase(
            "name-entity-declaration-1025-characters",
            HostileCategory.HugeName,
            XmlErrorCodes.XML1105,
            () => $"<!DOCTYPE r [<!ENTITY {LongName} \"x\">]><r/>"
        );
        yield return new HostileCase(
            "name-element-two-million-characters-unterminated",
            HostileCategory.HugeName,
            XmlErrorCodes.XML1105,
            () => "<" + Repeat("n", 2_000_000)
        );
        yield return new HostileCase(
            "name-limit-of-eight",
            HostileCategory.HugeName,
            XmlErrorCodes.XML1105,
            () => "<abcdefghi/>",
            Limits: XmlReaderLimits.Default.WithMaxNameLength(8)
        );
    }

    private static IEnumerable<HostileCase> AttributeCounts()
    {
        yield return new HostileCase(
            "attributes-257-on-one-element",
            HostileCategory.HugeAttributeCount,
            XmlErrorCodes.XML1103,
            () => Attributes(257)
        );
        yield return new HostileCase(
            "attributes-100000-on-one-element",
            HostileCategory.HugeAttributeCount,
            XmlErrorCodes.XML1103,
            () => Attributes(100_000)
        );
        yield return new HostileCase(
            "attributes-100000-unterminated",
            HostileCategory.HugeAttributeCount,
            XmlErrorCodes.XML1103,
            () => Attributes(100_000)[..^2]
        );
        yield return new HostileCase(
            "namespace-declarations-257-on-one-element",
            HostileCategory.HugeAttributeCount,
            XmlErrorCodes.XML1104,
            () => NamespaceDeclarations(257)
        );
        yield return new HostileCase(
            "namespace-declarations-100000-on-one-element",
            HostileCategory.HugeAttributeCount,
            XmlErrorCodes.XML1104,
            () => NamespaceDeclarations(100_000)
        );
        yield return new HostileCase(
            "attributes-limit-of-two",
            HostileCategory.HugeAttributeCount,
            XmlErrorCodes.XML1103,
            () => "<r a=\"1\" b=\"2\" c=\"3\"/>",
            Limits: XmlReaderLimits.Default.WithMaxAttributesPerElement(2)
        );
    }

    private static IEnumerable<HostileCase> TextSizes()
    {
        yield return new HostileCase(
            "text-node-16777217-characters",
            HostileCategory.HugeText,
            XmlErrorCodes.XML1107,
            () => "<r>" + Repeat("x", 16_777_217) + "</r>"
        );
        yield return new HostileCase(
            "text-cdata-16777217-characters",
            HostileCategory.HugeText,
            XmlErrorCodes.XML1107,
            () => "<r><![CDATA[" + Repeat("x", 16_777_217) + "]]></r>"
        );
        yield return new HostileCase(
            "text-node-from-101-character-references",
            HostileCategory.HugeText,
            XmlErrorCodes.XML1107,
            () => "<r>" + Repeat("&#65;", 101) + "</r>",
            Limits: TightText
        );
        yield return new HostileCase(
            "text-node-coalesced-from-text-and-cdata",
            HostileCategory.HugeText,
            XmlErrorCodes.XML1107,
            () => "<r>" + Repeat("x<![CDATA[x]]>", 51) + "</r>",
            Limits: TightText
        );
        yield return new HostileCase(
            "text-node-limit-of-100",
            HostileCategory.HugeText,
            XmlErrorCodes.XML1107,
            () => "<r>" + Repeat("x", 101) + "</r>",
            Limits: TightText
        );
    }

    private static IEnumerable<HostileCase> TokenSizes()
    {
        yield return new HostileCase(
            "text-attribute-value-1048577-characters",
            HostileCategory.HugeText,
            XmlErrorCodes.XML1112,
            () => "<r a=\"" + Repeat("x", 1_048_577) + "\"/>"
        );
        yield return new HostileCase(
            "text-comment-1048577-characters",
            HostileCategory.HugeText,
            XmlErrorCodes.XML1106,
            () => "<r><!--" + Repeat("x", 1_048_577) + "--></r>"
        );
        yield return new HostileCase(
            "text-processing-instruction-1048577-characters",
            HostileCategory.HugeText,
            XmlErrorCodes.XML1106,
            () => "<r><?p " + Repeat("x", 1_048_577) + "?></r>"
        );
    }

    private static IEnumerable<HostileCase> OtherLimits()
    {
        yield return new HostileCase(
            "limit-document-characters-of-100",
            HostileCategory.OtherLimit,
            XmlErrorCodes.XML1101,
            () => "<r>" + Repeat("x", 100) + "</r>",
            Limits: XmlReaderLimits.Default.WithMaxDocumentCharacters(100)
        );
        yield return new HostileCase(
            "limit-node-count-of-ten",
            HostileCategory.OtherLimit,
            XmlErrorCodes.XML1108,
            () => "<r>" + Repeat("<a/>", 11) + "</r>",
            Limits: XmlReaderLimits.Default.WithMaxNodeCount(10)
        );
        yield return new HostileCase(
            "limit-distinct-names-of-ten",
            HostileCategory.OtherLimit,
            XmlErrorCodes.XML1111,
            () =>
            {
                var builder = new StringBuilder("<r>");
                for (var index = 0; index < 11; index++)
                {
                    _ = builder.Append("<e").Append(index).Append("/>");
                }

                return builder.Append("</r>").ToString();
            },
            Limits: XmlReaderLimits.Default.WithMaxDistinctNames(10)
        );
        yield return new HostileCase(
            "limit-start-tag-4194305-characters",
            HostileCategory.OtherLimit,
            XmlErrorCodes.XML1113,
            () => WideStartTag(210, 20_000)
        );
        yield return new HostileCase(
            "limit-xml-declaration-1025-characters",
            HostileCategory.OtherLimit,
            XmlErrorCodes.XML1114,
            () => "<?xml version=\"1.0\"" + Repeat(" ", 1_025) + "?><r/>"
        );
        yield return new HostileCase(
            "limit-document-type-declaration-1048577-characters",
            HostileCategory.OtherLimit,
            XmlErrorCodes.XML1115,
            () => "<!DOCTYPE r [" + Repeat(" ", 1_048_577) + "]><r/>"
        );
    }

    private static string DistinctElements(int count)
    {
        var builder = new StringBuilder("<r>");
        for (var index = 0; index < count; index++)
        {
            _ = builder.Append("<e").Append(index).Append("/>");
        }

        return builder.Append("</r>").ToString();
    }

    private static IEnumerable<HostileCase> Unterminated()
    {
        yield return new HostileCase(
            "unterminated-comment-two-million-characters",
            HostileCategory.HugeText,
            XmlErrorCodes.XML1106,
            () => "<r><!--" + Repeat("x", 2_000_000)
        );
        yield return new HostileCase(
            "unterminated-processing-instruction-two-million-characters",
            HostileCategory.HugeText,
            XmlErrorCodes.XML1106,
            () => "<r><?p " + Repeat("x", 2_000_000)
        );
        yield return new HostileCase(
            "unterminated-cdata-17-million-characters",
            HostileCategory.HugeText,
            XmlErrorCodes.XML1107,
            () => "<r><![CDATA[" + Repeat("x", 17_000_000)
        );
        yield return new HostileCase(
            "unterminated-text-17-million-characters",
            HostileCategory.HugeText,
            XmlErrorCodes.XML1107,
            () => "<r>" + Repeat("x", 17_000_000)
        );
        yield return new HostileCase(
            "unterminated-attribute-value-two-million-characters",
            HostileCategory.HugeText,
            XmlErrorCodes.XML1112,
            () => "<r a=\"" + Repeat("x", 2_000_000)
        );
        yield return new HostileCase(
            "unterminated-start-tag-of-whitespace",
            HostileCategory.OtherLimit,
            XmlErrorCodes.XML1113,
            () => "<r" + Repeat(" ", 4_300_000)
        );
        yield return new HostileCase(
            "unterminated-document-type-declaration",
            HostileCategory.OtherLimit,
            XmlErrorCodes.XML1115,
            () => "<!DOCTYPE r [" + Repeat("x", 2_000_000)
        );
        yield return new HostileCase(
            "malformed-xml-declaration-of-5000-characters",
            HostileCategory.OtherLimit,
            XmlErrorCodes.XML0003,
            () => "<?xml version=\"1.0\"" + Repeat("x", 5_000)
        );
        yield return new HostileCase(
            "unterminated-xml-declaration-of-whitespace",
            HostileCategory.OtherLimit,
            XmlErrorCodes.XML1114,
            () => "<?xml version=\"1.0\"" + Repeat(" ", 2_000_000)
        );
        yield return new HostileCase(
            "limit-distinct-names-1048577",
            HostileCategory.OtherLimit,
            XmlErrorCodes.XML1111,
            () => DistinctElements(1_048_577)
        );
    }
}
