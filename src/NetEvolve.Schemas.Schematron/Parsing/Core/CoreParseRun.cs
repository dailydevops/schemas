namespace NetEvolve.Schemas.Schematron.Parsing.Core;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Editions;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;

/// <summary>The state of one parse: the tree, the source, the problem sink, and the failure flag.</summary>
internal sealed partial class CoreParseRun
{
    private const string SchematronNamespace = "http://purl.oclc.org/dsdl/schematron";
    private const string XsltNamespace = "http://www.w3.org/1999/XSL/Transform";

    private readonly XmlNameTable _names;
    private readonly XmlDocumentTree? _positions;
    private readonly string? _systemId;
    private readonly IProblemSink _problems;
    private readonly XmlSymbol _schematronNamespace;
    private readonly bool _hasNamespace;
    private readonly XmlSymbol _xsltNamespace;
    private readonly bool _hasXsltNamespace;
    private SchematronEdition _edition;
    private bool _rejectXslt;
    private bool _failed;

    internal CoreParseRun(XmlTree tree, string? systemId, IProblemSink problems)
    {
        _names = tree.Names;
        _positions = tree as XmlDocumentTree;
        _systemId = systemId;
        _problems = problems;
        _hasNamespace = _names.TryGet(SchematronNamespace.AsSpan(), out _schematronNamespace);
        _hasXsltNamespace = _names.TryGet(XsltNamespace.AsSpan(), out _xsltNamespace);
    }

    internal SchematronSchema? Run(XmlNode start)
    {
        if (!TryGetRoot(start, out var root))
        {
            return null;
        }

        var kind = Classify(root);
        if (kind != CoreElement.Schema)
        {
            if (kind == CoreElement.Library)
            {
                Report(SchematronDiagnosticCodes.Unsupported, "A library root is not supported yet.", Location(root));
            }
            else
            {
                Report(
                    SchematronDiagnosticCodes.RootInvalid,
                    "The root element '" + LocalName(root) + "' is not 'schema' in the Schematron namespace.",
                    Location(root)
                );
            }

            return null;
        }

        var schema = ParseSchema(root);
        return _failed ? null : schema;
    }

    private SchematronSchema ParseSchema(XmlNode element)
    {
        var parts = new ContainerParts();
        var edition = ReadEdition(element, out var editionText);
        _edition = edition;
        _rejectXslt = IsXsltBinding(Attribute(element, "queryBinding"));
        ReadContainerChildren(element, parts, isLibrary: false);

        return new SchematronSchema(
            Location(element),
            parts.Title,
            parts.Paragraphs.ToArray(),
            parts.Parameters.ToArray(),
            parts.Lets.ToArray(),
            parts.Patterns.ToArray(),
            parts.Diagnostics,
            parts.Properties,
            Attribute(element, "id"),
            Attribute(element, "schemaVersion"),
            editionText,
            edition,
            Attribute(element, "defaultPhase"),
            Attribute(element, "queryBinding"),
            parts.Namespaces.ToArray(),
            parts.Phases.ToArray(),
            parts.Rules.ToArray(),
            parts.Foreign.ToArray(),
            parts.Includes.ToArray()
        );
    }

    private void ReadContainerChildren(XmlNode element, ContainerParts parts, bool isLibrary)
    {
        var parent = isLibrary ? "library" : "schema";
        for (var child = SkipToElement(element.FirstChild); !child.IsEmpty; child = SkipToElement(child.NextSibling))
        {
            var kind = Classify(child);
            switch (kind)
            {
                case CoreElement.Foreign:
                    parts.Foreign.Add(ParseForeign(child));
                    break;
                case CoreElement.Title when parts.Title is null:
                    parts.Title = ParseTitle(child);
                    break;
                case CoreElement.Diagnostics when parts.Diagnostics is null:
                    parts.Diagnostics = ParseDiagnostics(child);
                    break;
                case CoreElement.Ns when !isLibrary:
                    parts.Namespaces.Add(ParseNamespace(child));
                    break;
                case CoreElement.Paragraph:
                    parts.Paragraphs.Add(ParseParagraph(child));
                    break;
                case CoreElement.Let:
                    parts.Lets.Add(ParseLet(child));
                    break;
                case CoreElement.Phase when !isLibrary:
                    parts.Phases.Add(ParsePhase(child));
                    break;
                case CoreElement.Pattern:
                    parts.Patterns.Add(ParsePattern(child, isGroup: false));
                    break;
                case CoreElement.Include:
                    parts.Includes.Add(
                        ParseInclude(
                            child,
                            parts.Patterns.Count,
                            parts.Lets.Count,
                            parts.Parameters.Count,
                            parts.Foreign.Count
                        )
                    );
                    break;
                default:
                    ReadEditionElement(child, kind, parts, parent);
                    break;
            }
        }
    }

    private SchematronEdition ReadEdition(XmlNode element, out string? text)
    {
        text = Attribute(element, SchematronEditionDetector.AttributeName);
        if (!SchematronEditionDetector.TryDetect(text, out var edition))
        {
            Report(
                SchematronDiagnosticCodes.EditionUnknown,
                SchematronEditionDetector.UnknownMessage(text!),
                Location(element)
            );
        }

        return edition;
    }

    private SchematronNamespace ParseNamespace(XmlNode element)
    {
        var prefix = Required(element, "prefix");
        var uri = Required(element, "uri");
        RejectChildren(element, "ns");
        return new SchematronNamespace(Location(element), prefix, uri);
    }

    private SchematronPatternBase ParsePattern(XmlNode element, bool isGroup)
    {
        var id = Attribute(element, "id");
        var isAbstract = Boolean(element, "abstract");
        var isA = Attribute(element, "is-a");
        var documents = Attribute(element, "documents");
        var role = Attribute(element, "role");
        var parts = new ContainerParts();
        ReadPatternChildren(element, isGroup ? "group" : "pattern", parts);

        var paragraphs = parts.Paragraphs.ToArray();
        var parameters = parts.Parameters.ToArray();
        var lets = parts.Lets.ToArray();
        var rules = parts.PatternRules.ToArray();
        var includes = parts.Includes.ToArray();
        return isGroup
            ? new SchematronGroup(
                Location(element),
                id,
                isAbstract,
                isA,
                documents,
                role,
                parts.Title,
                paragraphs,
                parameters,
                lets,
                rules,
                null,
                includes
            )
            : new SchematronPattern(
                Location(element),
                id,
                isAbstract,
                isA,
                documents,
                role,
                parts.Title,
                paragraphs,
                parameters,
                lets,
                rules,
                null,
                includes
            );
    }

    private void ReadPatternChildren(XmlNode element, string parent, ContainerParts parts)
    {
        for (var child = FirstChild(element); !child.IsEmpty; child = NextChild(child))
        {
            var kind = Classify(child);
            switch (kind)
            {
                case CoreElement.Title when parts.Title is null:
                    parts.Title = ParseTitle(child);
                    break;
                case CoreElement.Paragraph:
                    parts.Paragraphs.Add(ParseParagraph(child));
                    break;
                case CoreElement.Param:
                    parts.Parameters.Add(ParseParam(child));
                    break;
                case CoreElement.Let:
                    parts.Lets.Add(ParseLet(child));
                    break;
                case CoreElement.Rule:
                    parts.PatternRules.Add(ParseRule(child));
                    break;
                case CoreElement.Include:
                    parts.Includes.Add(ParseInclude(child, parts.PatternRules.Count));
                    break;
                default:
                    Reject(child, kind, parent);
                    break;
            }
        }
    }

    private SchematronRule ParseRule(XmlNode element)
    {
        SchematronTitle? title = null;
        var lets = new List<SchematronLet>();
        var items = new List<SchematronNode>();

        var id = Attribute(element, "id");
        var isAbstract = Boolean(element, "abstract");
        var context = isAbstract ? Attribute(element, "context") : Required(element, "context");
        var visitEach = Attribute(element, "visit-each");
        var flags = Tokens(Attribute(element, "flag"));
        var role = Attribute(element, "role");
        var subject = Attribute(element, "subject");
        for (var child = FirstChild(element); !child.IsEmpty; child = NextChild(child))
        {
            var kind = Classify(child);
            switch (kind)
            {
                case CoreElement.Title when title is null:
                    title = ParseTitle(child);
                    break;
                case CoreElement.Paragraph:
                    items.Add(ParseParagraph(child));
                    break;
                case CoreElement.Let:
                    lets.Add(ParseLet(child));
                    break;
                case CoreElement.Assert:
                    items.Add(ParseAssert(child));
                    break;
                case CoreElement.Report:
                    items.Add(ParseReport(child));
                    break;
                case CoreElement.Include:
                    items.Add(ParseInclude(child));
                    break;
                case CoreElement.Extends:
                    items.Add(ParseExtends(child));
                    break;
                default:
                    Reject(child, kind, "rule");
                    break;
            }
        }

        return new SchematronRule(
            Location(element),
            id,
            context,
            visitEach,
            flags,
            role,
            subject,
            isAbstract,
            title,
            lets.ToArray(),
            items.ToArray(),
            null
        );
    }

    private SchematronAssert ParseAssert(XmlNode element)
    {
        var test = Required(element, "test");
        var id = Attribute(element, "id");
        var flags = Tokens(Attribute(element, "flag"));
        var role = Attribute(element, "role");
        var severity = Attribute(element, "severity");
        var subject = Attribute(element, "subject");
        var diagnostics = Tokens(Attribute(element, "diagnostics"));
        var properties = Tokens(Attribute(element, "properties"));
        return new SchematronAssert(
            Location(element),
            test,
            id,
            flags,
            role,
            severity,
            subject,
            diagnostics,
            properties,
            ParseMessage(element, CoreInline.Message)
        );
    }

    private SchematronReport ParseReport(XmlNode element)
    {
        var test = Required(element, "test");
        var id = Attribute(element, "id");
        var flags = Tokens(Attribute(element, "flag"));
        var role = Attribute(element, "role");
        var severity = Attribute(element, "severity");
        var subject = Attribute(element, "subject");
        var diagnostics = Tokens(Attribute(element, "diagnostics"));
        var properties = Tokens(Attribute(element, "properties"));
        return new SchematronReport(
            Location(element),
            test,
            id,
            flags,
            role,
            severity,
            subject,
            diagnostics,
            properties,
            ParseMessage(element, CoreInline.Message)
        );
    }

    private SchematronLet ParseLet(XmlNode element)
    {
        var name = Required(element, "name");
        var declaredType = Attribute(element, "as");
        var value = Attribute(element, "value");
        var content = ReadLetContent(element);
        if (value is null)
        {
            if (content is null)
            {
                Report(
                    SchematronDiagnosticCodes.AttributeMissing,
                    "The element 'let' requires the attribute 'value' or element content.",
                    Location(element)
                );
            }

            value = string.Empty;
        }
        else if (content is not null)
        {
            Report(
                SchematronDiagnosticCodes.ElementMisplaced,
                "The element 'let' has either the attribute 'value' or content, not both.",
                Location(element)
            );
            content = null;
        }

        return new SchematronLet(Location(element), name, declaredType, value, content);
    }

    // The content of a let is data: elements of any namespace, the XSLT namespace included, and text, kept as written and built into a
    // temporary tree when the variable is bound. White space alone, as around the children of an element, is no content.
    private SchematronNode[]? ReadLetContent(XmlNode element)
    {
        List<SchematronNode>? content = null;
        var hasContent = false;
        for (var child = element.FirstChild; !child.IsEmpty; child = child.NextSibling)
        {
            if (child.Kind == XmlNodeKind.Text)
            {
                (content ??= []).Add(new SchematronText(Location(element), child.StringValue));
                hasContent |= !IsBlank(child.StringValue);
            }
            else if (child.Kind == XmlNodeKind.Element)
            {
                (content ??= []).Add(ParseForeign(child));
                hasContent = true;
            }
        }

        return hasContent ? content!.ToArray() : null;
    }

    private static bool IsBlank(string text)
    {
        foreach (var c in text)
        {
            if (c is not (' ' or '\t' or '\n' or '\r'))
            {
                return false;
            }
        }

        return true;
    }

    private SchematronPhase ParsePhase(XmlNode element)
    {
        var paragraphs = new List<SchematronParagraph>();
        var lets = new List<SchematronLet>();
        var actives = new List<SchematronActive>();

        var id = Required(element, "id");
        var from = Attribute(element, "from");
        var when = Attribute(element, "when");
        for (var child = FirstChild(element); !child.IsEmpty; child = NextChild(child))
        {
            var kind = Classify(child);
            switch (kind)
            {
                case CoreElement.Paragraph:
                    paragraphs.Add(ParseParagraph(child));
                    break;
                case CoreElement.Let:
                    lets.Add(ParseLet(child));
                    break;
                case CoreElement.Active:
                    actives.Add(ParseActive(child));
                    break;
                default:
                    Reject(child, kind, "phase");
                    break;
            }
        }

        return new SchematronPhase(
            Location(element),
            id,
            from,
            when,
            paragraphs.ToArray(),
            lets.ToArray(),
            actives.ToArray()
        );
    }

    private SchematronActive ParseActive(XmlNode element)
    {
        var pattern = Required(element, "pattern");
        return new SchematronActive(Location(element), ParseParts(element, CoreInline.Plain), pattern);
    }

    private SchematronDiagnostics ParseDiagnostics(XmlNode element)
    {
        var items = new List<SchematronDiagnostic>();
        for (var child = FirstChild(element); !child.IsEmpty; child = NextChild(child))
        {
            var kind = Classify(child);
            switch (kind)
            {
                case CoreElement.Diagnostic:
                    items.Add(ParseDiagnostic(child));
                    break;
                default:
                    Reject(child, kind, "diagnostics");
                    break;
            }
        }

        return new SchematronDiagnostics(Location(element), items.ToArray());
    }

    private SchematronDiagnostic ParseDiagnostic(XmlNode element)
    {
        var id = Required(element, "id");
        var role = Attribute(element, "role");
        return new SchematronDiagnostic(Location(element), id, role, ParseMessage(element, CoreInline.Diagnostic));
    }

    private SchematronTitle ParseTitle(XmlNode element) =>
        new(Location(element), ParseParts(element, CoreInline.Direction));

    private SchematronParagraph ParseParagraph(XmlNode element)
    {
        var id = Attribute(element, "id");
        var styleClass = Attribute(element, "class");
        var icon = Attribute(element, "icon");
        return new SchematronParagraph(Location(element), ParseParts(element, CoreInline.Plain), id, styleClass, icon);
    }

    private SchematronMessage ParseMessage(XmlNode element, CoreInline allowed) =>
        new(Location(element), ParseParts(element, allowed));

    private SchematronMessagePart[] ParseParts(XmlNode element, CoreInline allowed)
    {
        List<SchematronMessagePart>? parts = null;
        for (var child = element.FirstChild; !child.IsEmpty; child = child.NextSibling)
        {
            if (child.Kind == XmlNodeKind.Text)
            {
                (parts ??= []).Add(new SchematronText(Location(element), child.StringValue));
            }
            else if (child.Kind == XmlNodeKind.Element)
            {
                var part = ParsePart(child, allowed, element);
                if (part is not null)
                {
                    (parts ??= []).Add(part);
                }
            }
        }

        return parts is null ? Array.Empty<SchematronMessagePart>() : parts.ToArray();
    }

    private SchematronMessagePart? ParsePart(XmlNode element, CoreInline allowed, XmlNode host)
    {
        var kind = Classify(element);
        if (kind == CoreElement.Foreign)
        {
            if ((allowed & CoreInline.CopyOf) != CoreInline.None && IsCopyOf(element))
            {
                return ParseCopyOf(element);
            }

            RejectXslt(element, host);
            return null;
        }

        if (kind == CoreElement.Name && (allowed & CoreInline.Name) != CoreInline.None)
        {
            return new SchematronName(Location(element), Attribute(element, "path"));
        }

        if (kind == CoreElement.ValueOf && (allowed & CoreInline.ValueOf) != CoreInline.None)
        {
            return new SchematronValueOf(Location(element), Required(element, "select"));
        }

        if (kind == CoreElement.Emphasis && (allowed & CoreInline.Emphasis) != CoreInline.None)
        {
            return new SchematronEmphasis(Location(element), ParseParts(element, allowed & ~CoreInline.CopyOf));
        }

        if (kind == CoreElement.Direction && (allowed & CoreInline.Direction) != CoreInline.None)
        {
            var value = Attribute(element, "value");
            return new SchematronDirection(Location(element), value, ParseParts(element, allowed & ~CoreInline.CopyOf));
        }

        if (kind == CoreElement.Span && (allowed & CoreInline.Span) != CoreInline.None)
        {
            var styleClass = Attribute(element, "class");
            return new SchematronSpan(Location(element), styleClass, ParseParts(element, allowed & ~CoreInline.CopyOf));
        }

        Reject(element, kind, LocalName(host));
        return null;
    }

    // xsl:copy-of is kept only under an XSLT binding; under an XPath binding the XSLT content is skipped like all of it.
    private bool IsCopyOf(XmlNode element) =>
        _rejectXslt
        && _hasXsltNamespace
        && element.Name.NamespaceUri == _xsltNamespace
        && string.Equals(LocalName(element), "copy-of", StringComparison.Ordinal);

    // The copy of nodes into a message is the one XSLT instruction a property takes. The attributes that select a typed copy
    // (type, validation) or leave the namespace nodes out (copy-namespaces="no", #813) are not supported.
    private SchematronCopyOf ParseCopyOf(XmlNode element)
    {
        var select = Required(element, "select");
        foreach (var unsupported in new[] { "type", "validation" })
        {
            if (Attribute(element, unsupported) is not null)
            {
                ReportCopyOf(element, "The attribute '" + unsupported + "' of 'xsl:copy-of' is not supported.");
            }
        }

        var namespaces = Attribute(element, "copy-namespaces");
        if (namespaces is "no" or "false" or "0")
        {
            ReportCopyOf(
                element,
                "The value '" + namespaces + "' of 'copy-namespaces' on 'xsl:copy-of' is not supported."
            );
        }
        else if (namespaces is not (null or "yes" or "true" or "1"))
        {
            Report(
                SchematronDiagnosticCodes.AttributeInvalid,
                "The attribute 'copy-namespaces' of 'xsl:copy-of' must be 'yes' or 'no'.",
                Location(element)
            );
        }

        return new SchematronCopyOf(Location(element), select);
    }

    private void ReportCopyOf(XmlNode element, string message) =>
        Report(SchematronDiagnosticCodes.XsltUnsupported, message, Location(element));

    private CoreElement Classify(XmlNode element)
    {
        var name = element.Name;
        if (!_hasNamespace || name.NamespaceUri != _schematronNamespace)
        {
            return CoreElement.Foreign;
        }

        return _names.GetString(name.Local) switch
        {
            "schema" => CoreElement.Schema,
            "ns" => CoreElement.Ns,
            "pattern" => CoreElement.Pattern,
            "rule" => CoreElement.Rule,
            "assert" => CoreElement.Assert,
            "report" => CoreElement.Report,
            "let" => CoreElement.Let,
            "phase" => CoreElement.Phase,
            "active" => CoreElement.Active,
            "diagnostics" => CoreElement.Diagnostics,
            "diagnostic" => CoreElement.Diagnostic,
            "title" => CoreElement.Title,
            "p" => CoreElement.Paragraph,
            "name" => CoreElement.Name,
            "value-of" => CoreElement.ValueOf,
            "emph" => CoreElement.Emphasis,
            "dir" => CoreElement.Direction,
            "span" => CoreElement.Span,
            "library" => CoreElement.Library,
            "rules" => CoreElement.Rules,
            "group" => CoreElement.Group,
            "param" => CoreElement.Param,
            "properties" => CoreElement.Properties,
            "property" => CoreElement.Property,
            "include" => CoreElement.Include,
            "extends" => CoreElement.Extends,
            _ => CoreElement.Unknown,
        };
    }

    private void Reject(XmlNode element, CoreElement kind, string parent)
    {
        var local = LocalName(element);
        if (kind == CoreElement.Unknown)
        {
            Report(
                SchematronDiagnosticCodes.ElementUnknown,
                "The element '" + local + "' is not known in the Schematron namespace.",
                Location(element)
            );
        }
        else if (kind is CoreElement.Include or CoreElement.Extends)
        {
            Report(
                SchematronDiagnosticCodes.Unsupported,
                "The element '" + local + "' is not supported yet.",
                Location(element)
            );
        }
        else
        {
            Report(
                SchematronDiagnosticCodes.ElementMisplaced,
                "The element '" + local + "' is not allowed in '" + parent + "'.",
                Location(element)
            );
        }
    }

    private void RejectChildren(XmlNode element, string parent)
    {
        for (var child = FirstChild(element); !child.IsEmpty; child = NextChild(child))
        {
            Reject(child, Classify(child), parent);
        }
    }

    private string LocalName(XmlNode element) => _names.GetString(element.Name.Local);

    private string? Attribute(XmlNode element, string name)
    {
        for (var attribute = element.FirstAttribute; !attribute.IsEmpty; attribute = attribute.NextAttribute)
        {
            var attributeName = attribute.Name;
            if (
                attributeName.NamespaceUri.IsEmpty
                && string.Equals(_names.GetString(attributeName.Local), name, StringComparison.Ordinal)
            )
            {
                return attribute.StringValue;
            }
        }

        return null;
    }

    private string Required(XmlNode element, string name)
    {
        var value = Attribute(element, name);
        if (value is null)
        {
            Report(
                SchematronDiagnosticCodes.AttributeMissing,
                "The element '" + LocalName(element) + "' requires the attribute '" + name + "'.",
                Location(element)
            );
            return string.Empty;
        }

        return value;
    }

    private bool Boolean(XmlNode element, string name)
    {
        var value = Attribute(element, name)?.Trim();
        if (value is null or "false" or "0")
        {
            return false;
        }

        if (value is "true" or "1")
        {
            return true;
        }

        Report(
            SchematronDiagnosticCodes.AttributeInvalid,
            "The attribute '" + name + "' of '" + LocalName(element) + "' must be a boolean.",
            Location(element)
        );
        return false;
    }

    private static string[] Tokens(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return Array.Empty<string>();
        }

        List<string>? tokens = null;
        var start = -1;
        for (var i = 0; i <= value!.Length; i++)
        {
            var isSeparator = i == value.Length || value[i] is ' ' or '\t' or '\r' or '\n';
            if (!isSeparator && start < 0)
            {
                start = i;
            }
            else if (isSeparator && start >= 0)
            {
                (tokens ??= []).Add(value.Substring(start, i - start));
                start = -1;
            }
        }

        return tokens is null ? Array.Empty<string>() : tokens.ToArray();
    }

    private XmlNode FirstChild(XmlNode parent) => SkipToCore(SkipToElement(parent.FirstChild), parent);

    private XmlNode NextChild(XmlNode element) => SkipToCore(SkipToElement(element.NextSibling), element.Parent);

    private XmlNode SkipToCore(XmlNode node, XmlNode parent)
    {
        while (!node.IsEmpty && Classify(node) == CoreElement.Foreign)
        {
            RejectXslt(node, parent);
            node = SkipToElement(node.NextSibling);
        }

        return node;
    }

    private static XmlNode SkipToElement(XmlNode node)
    {
        while (!node.IsEmpty && node.Kind != XmlNodeKind.Element)
        {
            node = node.NextSibling;
        }

        return node;
    }

    private bool TryGetRoot(XmlNode start, out XmlNode root)
    {
        root = start.Kind == XmlNodeKind.Document ? SkipToElement(start.FirstChild) : start;
        if (root.IsEmpty)
        {
            Report(SchematronDiagnosticCodes.RootInvalid, "The document has no root element.", Location(start));
            return false;
        }

        return true;
    }

    private DocumentLocation Location(XmlNode node)
    {
        if (_positions is null || node.Kind != XmlNodeKind.Element)
        {
            return new DocumentLocation(_systemId, 0, 0);
        }

        var position = _positions.GetPosition(node.Index);
        return new DocumentLocation(_systemId, position.Line, position.Column);
    }

    private void Report(DiagnosticCode code, string message, DocumentLocation location)
    {
        _failed = true;
        _problems.Add(new SchematronProblem(code, message, location));
    }
}
