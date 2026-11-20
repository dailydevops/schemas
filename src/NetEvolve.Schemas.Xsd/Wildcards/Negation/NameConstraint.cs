namespace NetEvolve.Schemas.Xsd.Wildcards.Negation;

using System;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Wildcards.Core;

/// <summary>The namespace constraint and disallowed names of an XSD 1.1 wildcard.</summary>
internal sealed class NameConstraint : IEquatable<NameConstraint>
{
    private NameConstraint() { }

    /// <summary>Gets the constraint that allows every name.</summary>
    public static NameConstraint Any => throw new NotSupportedException();

    /// <summary>Gets a value indicating whether the namespaces are excluded rather than allowed.</summary>
    public bool IsNegation => throw new NotSupportedException();

    /// <summary>Gets the variety.</summary>
    public NamespaceConstraintVariety Variety => throw new NotSupportedException();

    /// <summary>Gets the namespaces, ordered ordinally.</summary>
    public string[] Namespaces => throw new NotSupportedException();

    /// <summary>Gets the disallowed names.</summary>
    public XPathQName[] DisallowedNames => throw new NotSupportedException();

    /// <summary>Gets a value indicating whether <c>##defined</c> is disallowed.</summary>
    public bool DisallowsDefined => throw new NotSupportedException();

    /// <summary>Gets a value indicating whether <c>##definedSibling</c> is disallowed.</summary>
    public bool DisallowsSibling => throw new NotSupportedException();

    /// <summary>Gets a value indicating whether no namespace is allowed.</summary>
    public bool IsEmpty => throw new NotSupportedException();

    /// <summary>Creates a constraint.</summary>
    /// <param name="negation">Whether <paramref name="namespaces"/> are excluded.</param>
    /// <param name="namespaces">The namespaces, empty string for the absent namespace.</param>
    /// <param name="disallowedNames">The disallowed names.</param>
    /// <param name="disallowsDefined">Whether <c>##defined</c> is disallowed.</param>
    /// <param name="disallowsSibling">Whether <c>##definedSibling</c> is disallowed.</param>
    /// <returns>The constraint.</returns>
    public static NameConstraint Create(
        bool negation,
        string[] namespaces,
        XPathQName[] disallowedNames,
        bool disallowsDefined = false,
        bool disallowsSibling = false
    ) => throw new NotSupportedException();

    /// <summary>Converts a constraint of the 1.0 wildcard.</summary>
    /// <param name="constraint">The constraint.</param>
    /// <returns>The equivalent constraint.</returns>
    public static NameConstraint FromCore(NamespaceConstraint constraint) => throw new NotSupportedException();

    /// <summary>Determines whether the namespaces allow a namespace name.</summary>
    /// <param name="namespaceName">The namespace name, empty for none.</param>
    /// <returns><see langword="true"/> when allowed.</returns>
    public bool AllowsNamespace(string namespaceName) => throw new NotSupportedException();

    /// <summary>Determines whether the constraint allows a name.</summary>
    /// <param name="name">The name.</param>
    /// <param name="defined">The declared names for <c>##defined</c>, <see langword="null"/> for none.</param>
    /// <param name="siblings">The sibling names for <c>##definedSibling</c>, <see langword="null"/> for none.</param>
    /// <returns><see langword="true"/> when allowed.</returns>
    public bool Allows(XPathQName name, IDeclaredNames? defined = null, IDeclaredNames? siblings = null) =>
        throw new NotSupportedException();

    /// <summary>Determines whether this constraint is a subset of another.</summary>
    /// <param name="super">The superset candidate.</param>
    /// <returns><see langword="true"/> when a subset.</returns>
    public bool IsSubsetOf(NameConstraint super) => throw new NotSupportedException();

    /// <summary>Computes the union.</summary>
    /// <param name="other">The other constraint.</param>
    /// <returns>The union.</returns>
    public NameConstraint Union(NameConstraint other) => throw new NotSupportedException();

    /// <summary>Computes the intersection.</summary>
    /// <param name="other">The other constraint.</param>
    /// <returns>The intersection.</returns>
    public NameConstraint Intersect(NameConstraint other) => throw new NotSupportedException();

    /// <summary>Determines whether some namespace is allowed by both constraints.</summary>
    /// <param name="other">The other constraint.</param>
    /// <returns><see langword="true"/> when they overlap.</returns>
    public bool Overlaps(NameConstraint other) => throw new NotSupportedException();

    /// <inheritdoc />
    public bool Equals(NameConstraint? other) => false;

    /// <inheritdoc />
    public override bool Equals(object? obj) => false;

    /// <inheritdoc />
    public override int GetHashCode() => 0;

    /// <inheritdoc />
    public override string ToString() => string.Empty;
}
