namespace NetEvolve.Schemas.Xsd.Wildcards.Negation;

using System;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.SchemaDocuments;

/// <summary>An XSD 1.1 wildcard component.</summary>
internal sealed class NegatedWildcard : IWildcard
{
    private NegatedWildcard() { }

    /// <inheritdoc />
    public XsdComponentKind Kind => XsdComponentKind.Wildcard;

    /// <inheritdoc />
    public XPathQName Name => default;

    /// <inheritdoc />
    public DocumentLocation Location => throw new NotSupportedException();

    /// <inheritdoc />
    public XsdProcessContents ProcessContents => throw new NotSupportedException();

    /// <summary>Gets the namespace constraint and disallowed names.</summary>
    public NameConstraint Constraint => throw new NotSupportedException();

    /// <summary>Gets the declared names that <c>##defined</c> refers to.</summary>
    public IDeclaredNames? DefinedNames => throw new NotSupportedException();

    /// <summary>Gets the sibling names that <c>##definedSibling</c> refers to.</summary>
    public IDeclaredNames? SiblingNames => throw new NotSupportedException();

    /// <summary>Creates a wildcard.</summary>
    /// <param name="constraint">The constraint.</param>
    /// <param name="processContents">How matched items are assessed.</param>
    /// <param name="location">The location.</param>
    /// <returns>The wildcard.</returns>
    public static NegatedWildcard Create(
        NameConstraint constraint,
        XsdProcessContents processContents,
        DocumentLocation location
    ) => throw new NotSupportedException();

    /// <summary>Creates a wildcard from an <c>any</c> or <c>anyAttribute</c> element.</summary>
    /// <param name="node">The element.</param>
    /// <param name="scope">The namespace scope of the document.</param>
    /// <param name="attributeWildcard"><see langword="true"/> for <c>anyAttribute</c>.</param>
    /// <param name="reporter">The reporter.</param>
    /// <param name="wildcard">The wildcard.</param>
    /// <returns><see langword="false"/> after an issue was reported.</returns>
    public static bool TryCreate(
        SchemaSyntaxNode node,
        SchemaNamespaceScope scope,
        bool attributeWildcard,
        XsdIssueReporter reporter,
        out NegatedWildcard? wildcard
    ) => throw new NotSupportedException();

    /// <summary>Returns a copy bound to declared names.</summary>
    /// <param name="defined">The names for <c>##defined</c>.</param>
    /// <param name="siblings">The names for <c>##definedSibling</c>.</param>
    /// <returns>The copy.</returns>
    public NegatedWildcard WithContext(IDeclaredNames? defined, IDeclaredNames? siblings) =>
        throw new NotSupportedException();

    /// <summary>Gets the 1.1 constraint of a wildcard of either implementation.</summary>
    /// <param name="wildcard">The wildcard.</param>
    /// <returns>The constraint.</returns>
    public static NameConstraint ConstraintOf(IWildcard wildcard) => throw new NotSupportedException();

    /// <inheritdoc />
    public bool Allows(XPathQName name) => throw new NotSupportedException();

    /// <inheritdoc />
    public bool IsSubsetOf(IWildcard other) => throw new NotSupportedException();

    /// <inheritdoc />
    public IWildcard? Union(IWildcard other) => throw new NotSupportedException();

    /// <inheritdoc />
    public IWildcard? Intersect(IWildcard other) => throw new NotSupportedException();
}
