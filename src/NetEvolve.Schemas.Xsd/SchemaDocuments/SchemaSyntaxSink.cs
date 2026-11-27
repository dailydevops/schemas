namespace NetEvolve.Schemas.Xsd.SchemaDocuments;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Text.NameTable;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;

/// <summary>
/// Receives the reader events of one schema document, builds the located syntax nodes in the one pass, and reports the
/// structural errors of the schema document.
/// </summary>
internal sealed class SchemaSyntaxSink : IXmlSink
{
    private readonly string? _baseUri;
    private readonly XsdIssueReporter _reporter;
    private readonly List<SchemaElementRule> _rules = [];
    private readonly List<XsdSyntaxAttribute> _attributes = [];
    private SchemaSyntaxNode? _current;
    private int _skipDepth;
    private IXmlPositionSource? _positions;

    // The appinfo or documentation element being read: no node is built for it, only its attributes are checked.
    private SchemaElementRule? _opaqueRule;
    private DocumentLocation _opaqueLocation;
    private SchemaSyntaxNode? _textReported;

    internal SchemaSyntaxSink(string? baseUri, XsdIssueReporter reporter)
    {
        ArgumentNullException.ThrowIfNull(reporter);

        _baseUri = baseUri;
        _reporter = reporter;
        Names = new SpanInterningNameTable(XmlReaderLimits.Default.MaxDistinctNames);
    }

    /// <inheritdoc />
    public XmlNameTable Names { get; }

    /// <summary>Gets the <c>schema</c> element, <see langword="null"/> when the root was rejected or the document did not start.</summary>
    internal SchemaSyntaxNode? Root { get; private set; }

    /// <summary>
    /// Locates attributes at their names by asking the reader that pumps this sink; without a source they stay at the start tag
    /// of their element.
    /// </summary>
    /// <param name="source">The reader that pumps this sink.</param>
    internal void UsePositions(IXmlPositionSource? source) => _positions = source;

    /// <inheritdoc />
    public void StartDocument(in XmlDeclaration declaration) { }

    /// <inheritdoc />
    public void EndDocument() { }

    /// <inheritdoc />
    public void StartElement(XmlName name, XmlPosition position)
    {
        if (_skipDepth > 0)
        {
            _skipDepth++;
            return;
        }

        if (_opaqueRule is not null)
        {
            _skipDepth = 1;
            return;
        }

        var ns = Names.GetString(name.NamespaceUri);
        var local = Names.GetString(name.Local);
        var location = new DocumentLocation(_baseUri, position.Line, position.Column);
        if (_current is null)
        {
            if (!IsSchemaElement(ns, local) || !string.Equals(local, "schema", StringComparison.Ordinal))
            {
                Report(
                    XsdRuleCodes.SchPropsCorrect,
                    location,
                    "The root element must be 'schema' in the namespace '" + SchemaGrammar.Namespace + "'."
                );
                _skipDepth = 1;
                return;
            }

            Root = Push(
                new SchemaSyntaxNode(new XPathQName(ns, local), location, null),
                SchemaGrammar.Find(local, string.Empty)
            );
            return;
        }

        var parent = _rules[_rules.Count - 1];
        if (
            !IsSchemaElement(ns, local)
            || !parent.Allows(local)
            || (
                string.Equals(local, "annotation", StringComparison.Ordinal)
                && !parent.RepeatableAnnotation
                && _current.ChildCount > 0
            )
        )
        {
            Report(parent.Code, location, "The element '" + local + "' is not allowed here in '" + parent.Name + "'.");
            _skipDepth = 1;
            return;
        }

        var rule = SchemaGrammar.Find(local, parent.Name);
        if (rule.Opaque)
        {
            _opaqueRule = rule;
            _opaqueLocation = location;
            return;
        }

        _ = Push(new SchemaSyntaxNode(new XPathQName(ns, local), location, _current), rule);
    }

    /// <inheritdoc />
    public void NamespaceDeclaration(XmlSymbol prefix, XmlSymbol uri)
    {
        if (_skipDepth == 0 && _opaqueRule is null)
        {
            _current?.AddNamespace(Names.GetString(prefix), Names.GetString(uri));
        }
    }

    /// <inheritdoc />
    public void Attribute(XmlName name, ReadOnlySpan<char> value)
    {
        if (_skipDepth > 0 || _current is null)
        {
            return;
        }

        var ns = Names.GetString(name.NamespaceUri);
        var local = Names.GetString(name.Local);
        var text = value.ToString();
        if (ns.Length == 0 || string.Equals(ns, SchemaGrammar.Namespace, StringComparison.Ordinal))
        {
            var rule = _opaqueRule ?? _rules[_rules.Count - 1];
            var location = AttributeLocation(_opaqueRule is null ? _current.Location : _opaqueLocation);
            var attribute = ns.Length == 0 ? rule.FindAttribute(local) : null;
            if (attribute is null)
            {
                Report(rule.Code, location, "The attribute '" + local + "' is not allowed on '" + rule.Name + "'.");
                return;
            }

            if (!SchemaAttributeValues.IsValid(attribute, text))
            {
                Report(
                    rule.Code,
                    location,
                    "The value '" + text + "' of the attribute '" + local + "' on '" + rule.Name + "' is not valid."
                );
            }
        }

        if (_opaqueRule is not null)
        {
            return;
        }

        _attributes.Add(new XsdSyntaxAttribute(new XPathQName(ns, local), text, AttributeLocation(_current.Location)));
    }

    /// <inheritdoc />
    public void EndAttributes()
    {
        if (_skipDepth > 0 || _current is null)
        {
            return;
        }

        // No attribute of appinfo or documentation is required (see the grammar), so there is nothing to check at the end.
        if (_opaqueRule is not null)
        {
            return;
        }

        _current.SetAttributes(_attributes);
        _attributes.Clear();

        var rule = _rules[_rules.Count - 1];
        foreach (var attribute in rule.Attributes)
        {
            if (attribute.Required && _current.FindAttribute(attribute.Name) is null)
            {
                Report(
                    rule.Code,
                    _current.Location,
                    "The attribute '" + attribute.Name + "' is required on '" + rule.Name + "'."
                );
            }
        }
    }

    /// <inheritdoc />
    public void EndElement(XmlName name)
    {
        if (_skipDepth > 0)
        {
            _skipDepth--;
            return;
        }

        if (_opaqueRule is not null)
        {
            _opaqueRule = null;
            return;
        }

        if (_current is null)
        {
            return;
        }

        CheckChoice(_current);
        _rules.RemoveAt(_rules.Count - 1);
        _current = _current.Parent;
    }

    /// <inheritdoc />
    public void Text(ReadOnlySpan<char> text, XmlTextKind kind)
    {
        // Every schema element except appinfo and documentation has element-only content: white space is fine, anything else
        // is reported once per element.
        if (_skipDepth > 0 || _opaqueRule is not null || _current is null || ReferenceEquals(_current, _textReported))
        {
            return;
        }

        foreach (var c in text)
        {
            if (!WhitespaceNormalizer.IsXmlSpace(c))
            {
                _textReported = _current;
                var rule = _rules[_rules.Count - 1];
                Report(
                    rule.Code,
                    AttributeLocation(_current.Location),
                    "The element '" + rule.Name + "' must not have character data."
                );
                return;
            }
        }
    }

    /// <inheritdoc />
    public void Comment(ReadOnlySpan<char> text) { }

    /// <inheritdoc />
    public void ProcessingInstruction(XmlSymbol target, ReadOnlySpan<char> data) { }

    /// <inheritdoc />
    public void Doctype(in XmlDoctypeInfo doctype) { }

    private static bool IsSchemaElement(string ns, string local) =>
        string.Equals(ns, SchemaGrammar.Namespace, StringComparison.Ordinal) && SchemaGrammar.IsKnown(local);

    private DocumentLocation AttributeLocation(DocumentLocation fallback)
    {
        if (_positions is null)
        {
            return fallback;
        }

        var position = _positions.Current;
        return new DocumentLocation(_baseUri, position.Line, position.Column);
    }

    private SchemaSyntaxNode Push(SchemaSyntaxNode node, SchemaElementRule rule)
    {
        _rules.Add(rule);
        _current = node;
        return node;
    }

    // src-restriction-base-or-simpleType, src-list-itemType-or-simpleType, src-union-memberTypes-or-simpleTypes.
    private void CheckChoice(SchemaSyntaxNode node)
    {
        var hasChild = node.CountChildren("simpleType") > 0;
        switch (node.Name.LocalName)
        {
            case "restriction" when string.Equals(node.Parent?.Name.LocalName, "simpleType", StringComparison.Ordinal):
                if ((node.FindAttribute("base") is not null) == hasChild)
                {
                    Report(
                        XsdRuleCodes.SrcRestrictionBaseOrSimpleType,
                        node.Location,
                        "A restriction has either a 'base' attribute or a 'simpleType' child."
                    );
                }

                break;
            case "list":
                if ((node.FindAttribute("itemType") is not null) == hasChild)
                {
                    Report(
                        XsdRuleCodes.SrcListItemTypeOrSimpleType,
                        node.Location,
                        "A list has either an 'itemType' attribute or a 'simpleType' child."
                    );
                }

                break;
            case "union" when node.FindAttribute("memberTypes") is null && !hasChild:
                Report(
                    XsdRuleCodes.SrcUnionMemberTypesOrSimpleTypes,
                    node.Location,
                    "A union has a 'memberTypes' attribute or 'simpleType' children."
                );
                break;
            default:
                break;
        }
    }

    private void Report(DiagnosticCode code, DocumentLocation location, string message)
    {
        foreach (var rule in XsdRuleCatalog.All)
        {
            if (rule.Code == code)
            {
                _reporter.Report(rule, location, message);
                return;
            }
        }
    }
}
