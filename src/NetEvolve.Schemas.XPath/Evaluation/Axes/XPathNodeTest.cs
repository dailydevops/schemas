namespace NetEvolve.Schemas.XPath.Evaluation.Axes;

using System;
using System.Runtime.InteropServices;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>A node test with its names resolved to namespace URIs; <see cref="Bind"/> ties it to the name table of a tree.</summary>
/// <remarks>
/// A <see langword="null"/> local name or namespace URI is a wildcard; the empty namespace URI is the absent namespace. Limitations:
/// <c>schema-element()</c> and <c>schema-attribute()</c> (#52) and type names in <c>element()</c> and <c>attribute()</c> (XP-21) are not
/// handled here; <see cref="TryFromSyntax"/> reports them as unsupported.
/// </remarks>
[StructLayout(LayoutKind.Auto)]
internal readonly struct XPathNodeTest
{
    private readonly XPathNodeTestKind _kind;
    private readonly string? _local;
    private readonly string? _namespaceUri;
    private readonly bool _requireDocumentElement;

    private XPathNodeTest(XPathNodeTestKind kind, string? local, string? namespaceUri, bool requireDocumentElement)
    {
        _kind = kind;
        _local = local;
        _namespaceUri = namespaceUri;
        _requireDocumentElement = requireDocumentElement;
    }

    /// <summary>Gets <c>node()</c>.</summary>
    public static XPathNodeTest AnyNode => new(XPathNodeTestKind.AnyNode, null, null, false);

    /// <summary>Gets <c>text()</c>.</summary>
    public static XPathNodeTest Text => new(XPathNodeTestKind.Text, null, null, false);

    /// <summary>Gets <c>comment()</c>.</summary>
    public static XPathNodeTest Comment => new(XPathNodeTestKind.Comment, null, null, false);

    /// <summary>Gets <c>document-node()</c>.</summary>
    public static XPathNodeTest Document => new(XPathNodeTestKind.Document, null, null, false);

    /// <summary>Creates <c>processing-instruction()</c> or <c>processing-instruction(target)</c>.</summary>
    /// <param name="target">The target, <see langword="null"/> for any.</param>
    /// <returns>The test.</returns>
    public static XPathNodeTest ProcessingInstruction(string? target) =>
        new(XPathNodeTestKind.ProcessingInstruction, target, null, false);

    /// <summary>Creates an element test: <c>element()</c>, <c>element(name)</c>, or a name test on an element axis.</summary>
    /// <param name="local">The local name, <see langword="null"/> for any.</param>
    /// <param name="namespaceUri">The namespace URI, <see langword="null"/> for any.</param>
    /// <returns>The test.</returns>
    public static XPathNodeTest Element(string? local, string? namespaceUri) =>
        new(XPathNodeTestKind.Element, local, namespaceUri, false);

    /// <summary>Creates an attribute test: <c>attribute()</c>, <c>attribute(name)</c>, or a name test on the attribute axis.</summary>
    /// <param name="local">The local name, <see langword="null"/> for any.</param>
    /// <param name="namespaceUri">The namespace URI, <see langword="null"/> for any.</param>
    /// <returns>The test.</returns>
    public static XPathNodeTest Attribute(string? local, string? namespaceUri) =>
        new(XPathNodeTestKind.Attribute, local, namespaceUri, false);

    /// <summary>Creates <c>document-node(element(name))</c>.</summary>
    /// <param name="local">The local name of the element, <see langword="null"/> for any.</param>
    /// <param name="namespaceUri">The namespace URI of the element, <see langword="null"/> for any.</param>
    /// <returns>The test.</returns>
    public static XPathNodeTest DocumentWithElement(string? local, string? namespaceUri) =>
        new(XPathNodeTestKind.Document, local, namespaceUri, true);

    /// <summary>Resolves a node test of the syntax tree against the static context.</summary>
    /// <param name="test">The syntax node.</param>
    /// <param name="axis">The axis of the step, which decides the principal node kind of a name test.</param>
    /// <param name="context">The static context that resolves prefixes and supplies the default element namespace.</param>
    /// <param name="result">The resolved test.</param>
    /// <returns><see langword="false"/> for a test this package does not handle yet (see the remarks of the type).</returns>
    /// <exception cref="XPathException">A prefix is not bound (XPST0081).</exception>
    public static bool TryFromSyntax(NodeTest test, Axis axis, XPathStaticContext context, out XPathNodeTest result)
    {
        Argument.ThrowIfNull(test);
        Argument.ThrowIfNull(context);

        switch (test)
        {
            case NameTest name:
                result = FromNameTest(name, axis, context);
                return true;
            case AnyKindTest:
                result = AnyNode;
                return true;
            case TextTest:
                result = Text;
                return true;
            case CommentTest:
                result = Comment;
                return true;
            case ProcessingInstructionTest pi:
                result = ProcessingInstruction(pi.Target);
                return true;
            case ElementTest { TypeName: null } element:
                result = FromName(element.Name, context, isAttribute: false);
                return true;
            case AttributeTest { TypeName: null } attribute:
                result = FromName(attribute.Name, context, isAttribute: true);
                return true;
            case DocumentTest { Element: null }:
                result = Document;
                return true;
            case DocumentTest { Element: ElementTest { TypeName: null } documentElement }:
                var inner = FromName(documentElement.Name, context, isAttribute: false);
                result = DocumentWithElement(inner._local, inner._namespaceUri);
                return true;
            default:
                result = default;
                return false;
        }
    }

    /// <summary>Binds the test to a name table, resolving the names to integer handles once.</summary>
    /// <param name="names">The name table of the tree to search; a name the table does not contain cannot match any node of it.</param>
    /// <returns>The matcher.</returns>
    public XPathNodeMatcher Bind(XmlNameTable names)
    {
        Argument.ThrowIfNull(names);

        var local = default(XmlSymbol);
        var namespaceUri = default(XmlSymbol);
        var missing =
            (_local is not null && !TryGet(names, _local, out local))
            || (_namespaceUri is not null && !TryGet(names, _namespaceUri, out namespaceUri));

        return new XPathNodeMatcher(
            _kind,
            local,
            namespaceUri,
            _local is null,
            _namespaceUri is null,
            _requireDocumentElement,
            missing
        );
    }

    private static bool TryGet(XmlNameTable names, string text, out XmlSymbol symbol)
    {
        if (text.Length == 0)
        {
            symbol = default;
            return true;
        }

        return names.TryGet(text.AsSpan(), out symbol);
    }

    private static XPathNodeTest FromNameTest(NameTest name, Axis axis, XPathStaticContext context)
    {
        var isAttribute = axis == Axis.Attribute;
        var (local, namespaceUri) = name.Kind switch
        {
            NameTestKind.AnyName => ((string?)null, (string?)null),
            NameTestKind.AnyNamespace => (name.LocalName, null),
            NameTestKind.AnyLocalName => (null, Resolve(name.Prefix, context, useDefaultElementNamespace: false)),
            _ => (name.LocalName, Resolve(name.Prefix, context, useDefaultElementNamespace: !isAttribute)),
        };
        return isAttribute ? Attribute(local, namespaceUri) : Element(local, namespaceUri);
    }

    // element(*), element(), attribute(*) and attribute() carry no name.
    private static XPathNodeTest FromName(QualifiedName? name, XPathStaticContext context, bool isAttribute)
    {
        var local = name?.LocalName;
        var namespaceUri = name is null ? null : Resolve(name.Prefix, context, !isAttribute);
        return isAttribute ? Attribute(local, namespaceUri) : Element(local, namespaceUri);
    }

    private static string Resolve(string? prefix, XPathStaticContext context, bool useDefaultElementNamespace)
    {
        if (prefix is null)
        {
            return useDefaultElementNamespace ? context.DefaultElementNamespace ?? string.Empty : string.Empty;
        }

        return context.TryResolveNamespace(prefix, out var namespaceUri)
            ? namespaceUri ?? string.Empty
            : throw XPathErrors.Exception(XPathErrorCodes.XPST0081);
    }
}
