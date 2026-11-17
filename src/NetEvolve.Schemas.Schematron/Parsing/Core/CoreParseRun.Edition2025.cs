namespace NetEvolve.Schemas.Schematron.Parsing.Core;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>
/// The elements and attributes the 2025 edition adds: <see langword="group"/>, <c>library</c>, <c>rules</c>, <c>param</c> under
/// <c>schema</c>, <c>properties</c>, <c>property</c>, and the foreign children of <c>schema</c> and <c>library</c> that the
/// XSLT registry needs.
/// </summary>
/// <remarks>
/// <para>
/// Limitations: <see langword="group"/>, <c>library</c>, <c>rules</c>, and a <c>param</c> directly under <c>schema</c> are gated by the
/// edition (<c>SCH-EDITION-MISMATCH</c>); a rejected <c>param</c> is reported at its element and not parsed, because the model
/// records positions for element starts only. Foreign elements are kept only as direct children of <c>schema</c> and
/// <c>library</c> (design #137 AP-3 as narrowed by #150 EP-11); namespace declarations on kept foreign elements and
/// comments inside them are not recorded. XSLT-namespace elements inside <c>pattern</c>, <see langword="group"/>, <c>rule</c>, <c>phase</c>,
/// diagnostics, properties, and message text are rejected as <c>SCH-ELEMENT-MISPLACED</c> when the query binding is an XSLT
/// binding or absent, and skipped under the XPath bindings; the element-content form of <see langword="let"/> is excluded and stays
/// the decision of its own issue.
/// </para>
/// </remarks>
internal sealed partial class CoreParseRun
{
    /// <summary>Parses a library document, which the loader reads as an include target.</summary>
    internal SchematronLibrary? RunLibrary(XmlNode start, SchematronEdition edition, bool xsltBinding)
    {
        _edition = edition;
        _rejectXslt = xsltBinding;
        if (!TryGetRoot(start, out var root))
        {
            return null;
        }

        if (Classify(root) != CoreElement.Library)
        {
            Report(
                SchematronDiagnosticCodes.RootInvalid,
                "The root element '" + LocalName(root) + "' is not 'library' in the Schematron namespace.",
                Location(root)
            );
            return null;
        }

        _ = Gate2025(root, "library");
        var parts = new ContainerParts();
        ReadContainerChildren(root, parts, isLibrary: true);
        var library = new SchematronLibrary(
            Location(root),
            parts.Title,
            parts.Paragraphs.ToArray(),
            parts.Parameters.ToArray(),
            parts.Lets.ToArray(),
            parts.Patterns.ToArray(),
            parts.Diagnostics,
            parts.Properties,
            parts.Rules.ToArray(),
            parts.Foreign.ToArray()
        );
        return _failed ? null : library;
    }

    private static bool IsXsltBinding(string? queryBinding) =>
        queryBinding is null
        || string.Equals(queryBinding, "xslt", StringComparison.OrdinalIgnoreCase)
        || string.Equals(queryBinding, "xslt1", StringComparison.OrdinalIgnoreCase)
        || string.Equals(queryBinding, "xslt2", StringComparison.OrdinalIgnoreCase)
        || string.Equals(queryBinding, "xslt3", StringComparison.OrdinalIgnoreCase);

    private bool Gate2025(XmlNode element, string name)
    {
        if (_edition == SchematronEdition.Iso2025)
        {
            return true;
        }

        Report(
            SchematronDiagnosticCodes.EditionMismatch,
            "The element '" + name + "' requires schematronEdition=\"2025\".",
            Location(element)
        );
        return false;
    }

    private void ReadEditionElement(XmlNode element, CoreElement kind, ContainerParts parts, string parent)
    {
        switch (kind)
        {
            case CoreElement.Properties when parts.Properties is null:
                parts.Properties = ParseProperties(element);
                break;
            case CoreElement.Group:
                ReadGroup(element, parts);
                break;
            case CoreElement.Rules:
                ReadRules(element, parts);
                break;
            case CoreElement.Param:
                ReadParameter(element, parts, isLibrary: string.Equals(parent, "library", StringComparison.Ordinal));
                break;
            default:
                Reject(element, kind, parent);
                break;
        }
    }

    private void ReadGroup(XmlNode element, ContainerParts parts)
    {
        if (Gate2025(element, "group"))
        {
            parts.Patterns.Add(ParsePattern(element, isGroup: true));
        }
    }

    private void ReadRules(XmlNode element, ContainerParts parts)
    {
        if (Gate2025(element, "rules"))
        {
            parts.Rules.Add(ParseRules(element));
        }
    }

    private void ReadParameter(XmlNode element, ContainerParts parts, bool isLibrary)
    {
        // A library is gated as a whole, so only a param directly under schema is gated on its own.
        if (isLibrary || Gate2025(element, "param"))
        {
            parts.Parameters.Add(ParseParam(element));
        }
    }

    private SchematronParameter ParseParam(XmlNode element)
    {
        var name = Required(element, "name");
        var value = Attribute(element, "value");
        RejectChildren(element, "param");
        return new SchematronParameter(Location(element), name, value);
    }

    private SchematronRules ParseRules(XmlNode element)
    {
        SchematronTitle? title = null;
        var paragraphs = new List<SchematronParagraph>();
        var rules = new List<SchematronRule>();

        var id = Attribute(element, "id");
        for (var child = FirstChild(element); !child.IsEmpty; child = NextChild(child))
        {
            var kind = Classify(child);
            switch (kind)
            {
                case CoreElement.Title when title is null:
                    title = ParseTitle(child);
                    break;
                case CoreElement.Paragraph:
                    paragraphs.Add(ParseParagraph(child));
                    break;
                case CoreElement.Rule:
                    rules.Add(ParseRule(child));
                    break;
                default:
                    Reject(child, kind, "rules");
                    break;
            }
        }

        return new SchematronRules(Location(element), id, title, paragraphs.ToArray(), rules.ToArray());
    }

    private SchematronProperties ParseProperties(XmlNode element)
    {
        var items = new List<SchematronProperty>();
        for (var child = FirstChild(element); !child.IsEmpty; child = NextChild(child))
        {
            var kind = Classify(child);
            switch (kind)
            {
                case CoreElement.Property:
                    items.Add(ParseProperty(child));
                    break;
                default:
                    Reject(child, kind, "properties");
                    break;
            }
        }

        return new SchematronProperties(Location(element), items.ToArray());
    }

    private SchematronProperty ParseProperty(XmlNode element)
    {
        var id = Required(element, "id");
        var role = Attribute(element, "role");
        var scheme = Attribute(element, "scheme");
        return new SchematronProperty(
            Location(element),
            id,
            role,
            scheme,
            ParseMessage(element, CoreInline.Diagnostic)
        );
    }

    private SchematronForeignElement ParseForeign(XmlNode element)
    {
        var attributes = new List<KeyValuePair<string, string>>();
        for (var attribute = element.FirstAttribute; !attribute.IsEmpty; attribute = attribute.NextAttribute)
        {
            var name = attribute.Name;
            var local = _names.GetString(name.Local);
            var key = name.NamespaceUri.IsEmpty ? local : "{" + _names.GetString(name.NamespaceUri) + "}" + local;
            attributes.Add(new KeyValuePair<string, string>(key, attribute.StringValue));
        }

        var children = new List<SchematronNode>();
        for (var child = element.FirstChild; !child.IsEmpty; child = child.NextSibling)
        {
            if (child.Kind == XmlNodeKind.Text)
            {
                children.Add(new SchematronText(Location(element), child.StringValue));
            }
            else if (child.Kind == XmlNodeKind.Element)
            {
                children.Add(ParseForeign(child));
            }
        }

        return new SchematronForeignElement(
            Location(element),
            _names.GetString(element.Name.NamespaceUri),
            LocalName(element),
            attributes.ToArray(),
            children.ToArray()
        );
    }

    private void RejectXslt(XmlNode element, XmlNode parent)
    {
        if (
            _rejectXslt
            && _hasXsltNamespace
            && element.Name.NamespaceUri == _xsltNamespace
            && Classify(parent) != CoreElement.Let
        )
        {
            Report(
                SchematronDiagnosticCodes.ElementMisplaced,
                "The XSLT element '" + LocalName(element) + "' is not allowed in '" + LocalName(parent) + "'.",
                Location(element)
            );
        }
    }
}
