namespace NetEvolve.Schemas.Xsd.Components;

using System;
using System.Runtime.InteropServices;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.SchemaDocuments;

/// <summary>
/// The namespace a schema document contributes its components to, and how it resolves QName references (Structures 3.15.3).
/// </summary>
/// <remarks>
/// A document without <c>targetNamespace</c> that another schema includes is a chameleon: its components take the namespace
/// of the including schema, and unprefixed references that resolve to no namespace are read as references into it.
/// </remarks>
[StructLayout(LayoutKind.Auto)]
internal readonly struct SchemaNamespaceScope
{
    /// <summary>Initializes a new instance of the <see cref="SchemaNamespaceScope"/> struct.</summary>
    /// <param name="targetNamespace">The effective target namespace, the including namespace for a chameleon.</param>
    /// <param name="isChameleon">Whether the document has no target namespace of its own and was included.</param>
    /// <param name="schemaElement">The <c>schema</c> element whose document-level defaults apply, <see langword="null"/> to take those of the ancestors of a node.</param>
    internal SchemaNamespaceScope(string targetNamespace, bool isChameleon, SchemaSyntaxNode? schemaElement = null)
    {
        ArgumentNullException.ThrowIfNull(targetNamespace);

        TargetNamespace = targetNamespace;
        IsChameleon = isChameleon;
        SchemaElement = schemaElement;
    }

    /// <summary>
    /// Gets the <c>schema</c> element of the unit, the source of the document-level defaults: a component that an
    /// <see langword="override"/> replaces takes those of the overridden document (Structures 1.1, 4.2.5), not of the ancestors of its node.
    /// </summary>
    public SchemaSyntaxNode? SchemaElement { get; }

    /// <summary>Gets the effective target namespace.</summary>
    public string TargetNamespace { get; }

    /// <summary>Gets a value indicating whether the document is included as a chameleon.</summary>
    public bool IsChameleon { get; }

    /// <summary>Gets the expanded name of a component declared with a local name.</summary>
    /// <param name="localName">The value of the <c>name</c> attribute.</param>
    /// <returns>The expanded name in the effective target namespace.</returns>
    internal XPathQName Declare(string localName) => new(TargetNamespace, localName);

    /// <summary>Resolves a QName value with the namespace declarations in scope at <paramref name="context"/>.</summary>
    /// <param name="context">The element that carries the value.</param>
    /// <param name="lexical">The lexical QName, <c>prefix:local</c> or <c>local</c>.</param>
    /// <param name="name">The expanded name.</param>
    /// <returns><see langword="false"/> when the prefix is not declared or the value is malformed (<c>src-resolve</c>).</returns>
    internal bool TryResolve(SchemaSyntaxNode context, string lexical, out XPathQName name)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(lexical);

        name = default;
        var colon = lexical.AsSpan().IndexOf(':');
        var local = colon < 0 ? lexical : lexical.Substring(colon + 1);
        if (local.Length == 0 || colon == 0)
        {
            return false;
        }

        string ns;
        if (colon > 0)
        {
            if (!context.TryResolvePrefix(lexical.Substring(0, colon), out ns))
            {
                return false;
            }
        }
        else
        {
            _ = context.TryResolvePrefix(string.Empty, out ns);
            if (ns.Length == 0 && IsChameleon)
            {
                ns = TargetNamespace;
            }
        }

        name = new XPathQName(ns, local);
        return true;
    }
}
