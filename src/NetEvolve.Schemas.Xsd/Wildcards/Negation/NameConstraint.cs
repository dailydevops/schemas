namespace NetEvolve.Schemas.Xsd.Wildcards.Negation;

using System;
using System.Collections.Generic;
using System.Text;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Wildcards.Core;

/// <summary>
/// The namespace constraint and the disallowed names of an XSD 1.1 wildcard (Structures 3.10.1): any namespace, every
/// namespace except a set (<c>notNamespace</c>, <c>##other</c>), or a set, together with names that stay disallowed
/// (<c>notQName</c>) and the keywords <c>##defined</c> and <c>##definedSibling</c>. Immutable. The absent namespace (no
/// namespace) is the empty string.
/// </summary>
/// <remarks>
/// <para>
/// A constraint is a set of namespaces that is finite or the complement of a finite set, so the union, the intersection, and
/// the subset test of the namespaces are exact; a namespace holds infinitely many names, so two constraints overlap exactly
/// when their namespaces do. A name listed in the disallowed names that its namespaces already exclude is dropped, so equal
/// wildcards have equal constraints (Wildcard Properties Correct makes it an error in a schema document, see
/// <see cref="NegatedWildcard.TryCreate"/>).
/// </para>
/// <para>
/// Limitations: the subset test is intensional for the keywords, as Structures 3.10.6.2 defines it: a constraint that
/// disallows a keyword is a subset only of one that disallows it too, whatever the declarations are. The standard names
/// the keyword <c>defined</c> in the union and the intersection, and <c>sibling</c> only for element wildcards, for which it
/// defines no set operation; the union keeps <c>##definedSibling</c> when both operands have it and the intersection when
/// either has it, as for <c>##defined</c>. The names of <c>##defined</c> and <c>##definedSibling</c> are known to the
/// wildcard component, not to the constraint.
/// </para>
/// </remarks>
internal sealed class NameConstraint : IEquatable<NameConstraint>
{
    private static readonly string[] NoNamespaces = [];
    private static readonly XPathQName[] NoNames = [];

    private NameConstraint(
        bool negation,
        string[] namespaces,
        XPathQName[] disallowedNames,
        bool disallowsDefined,
        bool disallowsSibling
    )
    {
        IsNegation = negation;
        Namespaces = namespaces;
        DisallowedNames = disallowedNames;
        DisallowsDefined = disallowsDefined;
        DisallowsSibling = disallowsSibling;
    }

    /// <summary>Gets the constraint that allows every name.</summary>
    public static NameConstraint Any { get; } = new(true, NoNamespaces, NoNames, false, false);

    /// <summary>Gets a value indicating whether the namespaces are excluded rather than allowed.</summary>
    public bool IsNegation { get; }

    /// <summary>Gets the variety: any, not (a negation of some namespaces), or enumeration.</summary>
    public NamespaceConstraintVariety Variety =>
        !IsNegation ? NamespaceConstraintVariety.Enumeration
        : Namespaces.Length == 0 ? NamespaceConstraintVariety.Any
        : NamespaceConstraintVariety.Not;

    /// <summary>Gets the excluded namespaces of a negation, or the allowed ones of a set, ordered ordinally.</summary>
    public string[] Namespaces { get; }

    /// <summary>Gets the names that stay disallowed in an allowed namespace, ordered by namespace and local name.</summary>
    public XPathQName[] DisallowedNames { get; }

    /// <summary>Gets a value indicating whether <c>##defined</c> is disallowed: the names that are declared globally.</summary>
    public bool DisallowsDefined { get; }

    /// <summary>Gets a value indicating whether <c>##definedSibling</c> is disallowed: the elements of the same content model.</summary>
    public bool DisallowsSibling { get; }

    /// <summary>Gets a value indicating whether no namespace is allowed.</summary>
    public bool IsEmpty => !IsNegation && Namespaces.Length == 0;

    /// <summary>Creates a constraint.</summary>
    /// <param name="negation">Whether <paramref name="namespaces"/> are excluded; a negation of none allows every namespace.</param>
    /// <param name="namespaces">The namespaces, the empty string for the absent namespace; duplicates are removed.</param>
    /// <param name="disallowedNames">The names that stay disallowed; names in an excluded namespace are dropped.</param>
    /// <param name="disallowsDefined">Whether <c>##defined</c> is disallowed.</param>
    /// <param name="disallowsSibling">Whether <c>##definedSibling</c> is disallowed.</param>
    /// <returns>The constraint.</returns>
    public static NameConstraint Create(
        bool negation,
        string[] namespaces,
        XPathQName[] disallowedNames,
        bool disallowsDefined = false,
        bool disallowsSibling = false
    )
    {
        ArgumentNullException.ThrowIfNull(namespaces);
        ArgumentNullException.ThrowIfNull(disallowedNames);

        for (var i = 0; i < namespaces.Length; i++)
        {
            ArgumentNullException.ThrowIfNull(namespaces[i]);
        }

        return Build(negation, namespaces, disallowedNames, disallowsDefined, disallowsSibling);
    }

    /// <summary>Converts the constraint of a wildcard of XSD 1.0, where <see langword="not"/> excludes one namespace and the absent namespace.</summary>
    /// <param name="constraint">The constraint.</param>
    /// <returns>The equivalent constraint.</returns>
    public static NameConstraint FromCore(NamespaceConstraint constraint)
    {
        ArgumentNullException.ThrowIfNull(constraint);

        return constraint.Variety switch
        {
            NamespaceConstraintVariety.Any => Any,
            NamespaceConstraintVariety.Not => Build(true, [constraint.Values[0], string.Empty], NoNames, false, false),
            _ => Build(false, constraint.Values, NoNames, false, false),
        };
    }

    /// <summary>Determines whether the namespaces allow a namespace name.</summary>
    /// <param name="namespaceName">The namespace name, empty for none.</param>
    /// <returns><see langword="true"/> when allowed.</returns>
    public bool AllowsNamespace(string namespaceName)
    {
        ArgumentNullException.ThrowIfNull(namespaceName);

        return Contains(Namespaces, namespaceName) != IsNegation;
    }

    /// <summary>Determines whether the constraint allows a name (Wildcard allows Expanded Name, Structures 3.10.4).</summary>
    /// <param name="name">The name.</param>
    /// <param name="defined">The names for <c>##defined</c>, <see langword="null"/> when no declarations are known.</param>
    /// <param name="siblings">The names for <c>##definedSibling</c>, <see langword="null"/> when no content model is known.</param>
    /// <returns><see langword="true"/> when allowed.</returns>
    public bool Allows(XPathQName name, IDeclaredNames? defined = null, IDeclaredNames? siblings = null)
    {
        if (!AllowsNamespace(name.NamespaceName) || Array.BinarySearch(DisallowedNames, name, NameOrder.Instance) >= 0)
        {
            return false;
        }

        if (DisallowsDefined && defined is not null && defined.Contains(name))
        {
            return false;
        }

        return !(DisallowsSibling && siblings is not null && siblings.Contains(name));
    }

    /// <summary>
    /// Determines whether this constraint is a subset of another: the namespaces are, every name the other one disallows in
    /// a namespace this one allows is disallowed here too, and so is every keyword of the other one.
    /// </summary>
    /// <param name="super">The superset candidate.</param>
    /// <returns><see langword="true"/> when a subset.</returns>
    public bool IsSubsetOf(NameConstraint super)
    {
        ArgumentNullException.ThrowIfNull(super);

        if (!NamespacesAreSubsetOf(super) || (super.DisallowsDefined && !DisallowsDefined))
        {
            return false;
        }

        if (super.DisallowsSibling && !DisallowsSibling)
        {
            return false;
        }

        return DisallowsAllOf(super);
    }

    /// <summary>
    /// Computes the union (Attribute Wildcard Union, Structures 3.10.6.3), which is always expressible: a keyword is kept only
    /// when both operands have it, which may allow names that neither operand allows, as the note of the standard says.
    /// </summary>
    /// <param name="other">The other constraint.</param>
    /// <returns>The union.</returns>
    public NameConstraint Union(NameConstraint other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (Equals(other))
        {
            return this;
        }

        string[] namespaces;
        if (IsNegation && other.IsNegation)
        {
            namespaces = Filter(Namespaces, other.Namespaces, true);
        }
        else if (!IsNegation && !other.IsNegation)
        {
            namespaces = Concat(Namespaces, other.Namespaces);
        }
        else
        {
            // The negation without the members of the set.
            namespaces = IsNegation
                ? Filter(Namespaces, other.Namespaces, false)
                : Filter(other.Namespaces, Namespaces, false);
        }

        // A name stays disallowed when both operands disallow it: by the list or by the namespaces.
        var names = new List<XPathQName>();
        AddDisallowedByBoth(this, other, names);
        AddDisallowedByBoth(other, this, names);

        return Build(
            IsNegation || other.IsNegation,
            namespaces,
            names.ToArray(),
            DisallowsDefined && other.DisallowsDefined,
            DisallowsSibling && other.DisallowsSibling
        );
    }

    /// <summary>Computes the intersection (Attribute Wildcard Intersection, Structures 3.10.6); it is always expressible.</summary>
    /// <param name="other">The other constraint.</param>
    /// <returns>The intersection.</returns>
    public NameConstraint Intersect(NameConstraint other)
    {
        ArgumentNullException.ThrowIfNull(other);

        string[] namespaces;
        if (IsNegation && other.IsNegation)
        {
            namespaces = Concat(Namespaces, other.Namespaces);
        }
        else if (!IsNegation && !other.IsNegation)
        {
            namespaces = Filter(Namespaces, other.Namespaces, true);
        }
        else
        {
            // The set without the members of the negation.
            namespaces = IsNegation
                ? Filter(other.Namespaces, Namespaces, false)
                : Filter(Namespaces, other.Namespaces, false);
        }

        return Build(
            IsNegation && other.IsNegation,
            namespaces,
            Concat(DisallowedNames, other.DisallowedNames),
            DisallowsDefined || other.DisallowsDefined,
            DisallowsSibling || other.DisallowsSibling
        );
    }

    /// <summary>Determines whether some namespace is allowed by both constraints, which means that some name is.</summary>
    /// <param name="other">The other constraint.</param>
    /// <returns><see langword="true"/> when they overlap.</returns>
    public bool Overlaps(NameConstraint other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (IsNegation && other.IsNegation)
        {
            return true;
        }

        // Some member of the set is allowed by the other side.
        var set = IsNegation ? other : this;
        var against = IsNegation ? this : other;
        for (var i = 0; i < set.Namespaces.Length; i++)
        {
            if (against.AllowsNamespace(set.Namespaces[i]))
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc />
    public bool Equals(NameConstraint? other)
    {
        if (
            other is null
            || IsNegation != other.IsNegation
            || DisallowsDefined != other.DisallowsDefined
            || DisallowsSibling != other.DisallowsSibling
            || Namespaces.Length != other.Namespaces.Length
            || DisallowedNames.Length != other.DisallowedNames.Length
        )
        {
            return false;
        }

        for (var i = 0; i < Namespaces.Length; i++)
        {
            if (!string.Equals(Namespaces[i], other.Namespaces[i], StringComparison.Ordinal))
            {
                return false;
            }
        }

        for (var i = 0; i < DisallowedNames.Length; i++)
        {
            if (DisallowedNames[i] != other.DisallowedNames[i])
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as NameConstraint);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = (IsNegation ? 1 : 0) + (DisallowsDefined ? 2 : 0) + (DisallowsSibling ? 4 : 0);
        for (var i = 0; i < Namespaces.Length; i++)
        {
            hash = unchecked((hash * 31) + StringComparer.Ordinal.GetHashCode(Namespaces[i]));
        }

        for (var i = 0; i < DisallowedNames.Length; i++)
        {
            hash = unchecked((hash * 31) + DisallowedNames[i].GetHashCode());
        }

        return hash;
    }

    /// <summary>Formats the constraint as <c>##any</c>, <c>not(a,b)</c>, or <c>{a,b}</c>, followed by <c> except [...]</c> for names and keywords.</summary>
    /// <returns>The text.</returns>
    public override string ToString()
    {
        var builder = new StringBuilder();
        if (IsNegation && Namespaces.Length == 0)
        {
            _ = builder.Append("##any");
        }
        else
        {
            _ = builder.Append(IsNegation ? "not(" : "{");
            for (var i = 0; i < Namespaces.Length; i++)
            {
                _ = builder
                    .Append(i > 0 ? "," : string.Empty)
                    .Append(Namespaces[i].Length == 0 ? "##local" : Namespaces[i]);
            }

            _ = builder.Append(IsNegation ? ')' : '}');
        }

        var exceptions = new List<string>(DisallowedNames.Length + 2);
        for (var i = 0; i < DisallowedNames.Length; i++)
        {
            exceptions.Add(DisallowedNames[i].ToString());
        }

        if (DisallowsDefined)
        {
            exceptions.Add("##defined");
        }

        if (DisallowsSibling)
        {
            exceptions.Add("##definedSibling");
        }

        return exceptions.Count == 0
            ? builder.ToString()
            : builder.Append(" except [").Append(string.Join(",", exceptions)).Append(']').ToString();
    }

    private static NameConstraint Build(
        bool negation,
        string[] namespaces,
        XPathQName[] disallowedNames,
        bool disallowsDefined,
        bool disallowsSibling
    )
    {
        var members = (string[])namespaces.Clone();
        Array.Sort(members, StringComparer.Ordinal);
        var count = 0;
        for (var i = 0; i < members.Length; i++)
        {
            if (count == 0 || !string.Equals(members[count - 1], members[i], StringComparison.Ordinal))
            {
                members[count++] = members[i];
            }
        }

        Array.Resize(ref members, count);

        // A name in an excluded namespace changes nothing, so it is not kept.
        var kept = new List<XPathQName>(disallowedNames.Length);
        for (var i = 0; i < disallowedNames.Length; i++)
        {
            if (Contains(members, disallowedNames[i].NamespaceName) != negation)
            {
                kept.Add(disallowedNames[i]);
            }
        }

        kept.Sort(NameOrder.Instance);
        var names = new List<XPathQName>(kept.Count);
        foreach (var name in kept)
        {
            if (names.Count == 0 || names[names.Count - 1] != name)
            {
                names.Add(name);
            }
        }

        return new(negation, members, names.Count == 0 ? NoNames : names.ToArray(), disallowsDefined, disallowsSibling);
    }

    // The namespaces of this constraint, as a set over all namespace names, are in the namespaces of the other one.
    private bool NamespacesAreSubsetOf(NameConstraint super)
    {
        if (IsNegation)
        {
            // A complement of a finite set is in a complement of a finite set when the larger set is excluded.
            return super.IsNegation && IsSupersetOfMembers(Namespaces, super.Namespaces);
        }

        for (var i = 0; i < Namespaces.Length; i++)
        {
            if (!super.AllowsNamespace(Namespaces[i]))
            {
                return false;
            }
        }

        return true;
    }

    // Every name that the other one disallows in a namespace that this one allows is disallowed here.
    private bool DisallowsAllOf(NameConstraint super)
    {
        for (var i = 0; i < super.DisallowedNames.Length; i++)
        {
            var name = super.DisallowedNames[i];
            if (
                AllowsNamespace(name.NamespaceName)
                && Array.BinarySearch(DisallowedNames, name, NameOrder.Instance) < 0
            )
            {
                return false;
            }
        }

        return true;
    }

    // Adds the names of the first operand that the second one disallows too: in its list or by its namespaces.
    private static void AddDisallowedByBoth(NameConstraint first, NameConstraint second, List<XPathQName> names)
    {
        for (var i = 0; i < first.DisallowedNames.Length; i++)
        {
            var name = first.DisallowedNames[i];
            if (
                !second.AllowsNamespace(name.NamespaceName)
                || Array.BinarySearch(second.DisallowedNames, name, NameOrder.Instance) >= 0
            )
            {
                names.Add(name);
            }
        }
    }

    private static bool IsSupersetOfMembers(string[] set, string[] members)
    {
        for (var i = 0; i < members.Length; i++)
        {
            if (!Contains(set, members[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool Contains(string[] values, string value) =>
        Array.BinarySearch(values, value, StringComparer.Ordinal) >= 0;

    // The values that are, or are not, in the other array.
    private static string[] Filter(string[] values, string[] other, bool keepMembers)
    {
        var kept = new List<string>(values.Length);
        for (var i = 0; i < values.Length; i++)
        {
            if (Contains(other, values[i]) == keepMembers)
            {
                kept.Add(values[i]);
            }
        }

        return kept.ToArray();
    }

    private static T[] Concat<T>(T[] left, T[] right)
    {
        var all = new T[left.Length + right.Length];
        left.CopyTo(all, 0);
        right.CopyTo(all, left.Length);
        return all;
    }

    private sealed class NameOrder : IComparer<XPathQName>
    {
        internal static readonly NameOrder Instance = new();

        public int Compare(XPathQName x, XPathQName y)
        {
            var byNamespace = string.CompareOrdinal(x.NamespaceName, y.NamespaceName);
            return byNamespace != 0 ? byNamespace : string.CompareOrdinal(x.LocalName, y.LocalName);
        }
    }
}
