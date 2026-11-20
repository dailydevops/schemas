namespace NetEvolve.Schemas.Xsd.Wildcards.Core;

using System;
using System.Text;
using NetEvolve.Arguments;

/// <summary>
/// The namespace constraint of a wildcard: any, a negation, or a set (Structures 3.10.1), with the set operations of
/// Structures 3.10.6. Immutable. The absent namespace (no namespace) is the empty string.
/// </summary>
/// <remarks>
/// Limitation: the operations follow the intensional definitions of Structures 1.0, 3.10.6, so a negation is a subset
/// only of the same negation even when it allows fewer names. <c>notNamespace</c> and <c>notQName</c> of XSD 1.1 are
/// modeled by <c>NameConstraint</c> in <c>Wildcards/Negation</c> (#306).
/// </remarks>
internal sealed class NamespaceConstraint : IEquatable<NamespaceConstraint>
{
    private static readonly string[] NoValues = [];

    private NamespaceConstraint(NamespaceConstraintVariety variety, string[] values)
    {
        Variety = variety;
        Values = values;
    }

    /// <summary>Gets the constraint that allows every namespace.</summary>
    public static NamespaceConstraint Any { get; } = new(NamespaceConstraintVariety.Any, NoValues);

    /// <summary>Gets the variety.</summary>
    public NamespaceConstraintVariety Variety { get; }

    /// <summary>Gets the negated value of a negation, or the members of a set, ordered ordinally; empty for any.</summary>
    public string[] Values { get; }

    /// <summary>Creates a negation of one namespace name and of the absent namespace.</summary>
    /// <param name="namespaceName">The negated namespace name, empty for the absent namespace.</param>
    /// <returns>The constraint.</returns>
    public static NamespaceConstraint Not(string namespaceName)
    {
        Argument.ThrowIfNull(namespaceName);

        return new(NamespaceConstraintVariety.Not, [namespaceName]);
    }

    /// <summary>Creates a set of namespace names.</summary>
    /// <param name="namespaceNames">The members, empty string for the absent namespace; duplicates are removed.</param>
    /// <returns>The constraint.</returns>
    public static NamespaceConstraint Set(string[] namespaceNames)
    {
        Argument.ThrowIfNull(namespaceNames);

        var copy = (string[])namespaceNames.Clone();
        Array.Sort(copy, StringComparer.Ordinal);
        var count = 0;
        for (var i = 0; i < copy.Length; i++)
        {
            Argument.ThrowIfNull(copy[i]);
            if (count == 0 || !string.Equals(copy[count - 1], copy[i], StringComparison.Ordinal))
            {
                copy[count++] = copy[i];
            }
        }

        Array.Resize(ref copy, count);
        return new(NamespaceConstraintVariety.Enumeration, copy);
    }

    /// <summary>Parses the <see langword="namespace"/> attribute of <c>any</c> and <c>anyAttribute</c> (Structures 3.10.2).</summary>
    /// <param name="value">The attribute value, <see langword="null"/> when absent, which means <c>##any</c>.</param>
    /// <param name="targetNamespace">The target namespace of the schema document, empty for none.</param>
    /// <param name="constraint">The constraint when the value is valid.</param>
    /// <returns><see langword="true"/> when the value is valid.</returns>
    public static bool TryParse(string? value, string targetNamespace, out NamespaceConstraint? constraint)
    {
        Argument.ThrowIfNull(targetNamespace);

        constraint = null;
        if (value is null)
        {
            constraint = Any;
            return true;
        }

        var members = new string[CountTokens(value)];
        var count = 0;
        var position = 0;
        while (NextToken(value, ref position, out var start, out var length))
        {
            var token = value.Substring(start, length);
            var isAny = string.Equals(token, "##any", StringComparison.Ordinal);
            if (isAny || string.Equals(token, "##other", StringComparison.Ordinal))
            {
                // Only a lone token may be one of the two keywords.
                if (members.Length != 1)
                {
                    return false;
                }

                constraint = isAny ? Any : Not(targetNamespace);
                return true;
            }

            if (string.Equals(token, "##local", StringComparison.Ordinal))
            {
                token = string.Empty;
            }
            else if (string.Equals(token, "##targetNamespace", StringComparison.Ordinal))
            {
                token = targetNamespace;
            }
            else if (token.StartsWith("##", StringComparison.Ordinal))
            {
                return false;
            }

            members[count++] = token;
        }

        constraint = Set(members);
        return true;
    }

    /// <summary>Determines whether the constraint allows a namespace name (Wildcard allows Namespace Name, Structures 3.10.4).</summary>
    /// <param name="namespaceName">The namespace name of the item, empty when it has none.</param>
    /// <returns><see langword="true"/> when allowed.</returns>
    public bool Allows(string namespaceName)
    {
        Argument.ThrowIfNull(namespaceName);

        return Variety switch
        {
            NamespaceConstraintVariety.Any => true,
            NamespaceConstraintVariety.Not => namespaceName.Length > 0
                && !string.Equals(Values[0], namespaceName, StringComparison.Ordinal),
            _ => Contains(Values, namespaceName),
        };
    }

    /// <summary>Determines whether this constraint is an intensional subset of another (Wildcard Subset).</summary>
    /// <param name="super">The superset candidate.</param>
    /// <returns><see langword="true"/> when this is a subset.</returns>
    public bool IsSubsetOf(NamespaceConstraint super)
    {
        Argument.ThrowIfNull(super);

        if (super.Variety == NamespaceConstraintVariety.Any)
        {
            return true;
        }

        if (Variety == NamespaceConstraintVariety.Not)
        {
            return super.Variety == NamespaceConstraintVariety.Not && Equals(super);
        }

        if (Variety == NamespaceConstraintVariety.Any)
        {
            return false;
        }

        if (super.Variety == NamespaceConstraintVariety.Enumeration)
        {
            for (var i = 0; i < Values.Length; i++)
            {
                if (!Contains(super.Values, Values[i]))
                {
                    return false;
                }
            }

            return true;
        }

        return !Contains(Values, super.Values[0]) && !Contains(Values, string.Empty);
    }

    /// <summary>Computes the intensional union (Attribute Wildcard Union).</summary>
    /// <param name="other">The other constraint.</param>
    /// <returns>The union, or <see langword="null"/> when it is not expressible.</returns>
    public NamespaceConstraint? Union(NamespaceConstraint other)
    {
        Argument.ThrowIfNull(other);

        if (Equals(other))
        {
            return this;
        }

        if (Variety == NamespaceConstraintVariety.Any || other.Variety == NamespaceConstraintVariety.Any)
        {
            return Any;
        }

        if (
            Variety == NamespaceConstraintVariety.Enumeration
            && other.Variety == NamespaceConstraintVariety.Enumeration
        )
        {
            return Set(Concat(Values, other.Values));
        }

        if (Variety == NamespaceConstraintVariety.Not && other.Variety == NamespaceConstraintVariety.Not)
        {
            return Not(string.Empty);
        }

        var negation = Variety == NamespaceConstraintVariety.Not ? this : other;
        var set = Variety == NamespaceConstraintVariety.Not ? other.Values : Values;
        var negated = negation.Values[0];
        var hasAbsent = Contains(set, string.Empty);

        if (negated.Length == 0)
        {
            return hasAbsent ? Any : negation;
        }

        if (Contains(set, negated))
        {
            return hasAbsent ? Any : Not(string.Empty);
        }

        return hasAbsent ? null : negation;
    }

    /// <summary>Computes the intensional intersection (Attribute Wildcard Intersection).</summary>
    /// <param name="other">The other constraint.</param>
    /// <returns>The intersection, or <see langword="null"/> when it is not expressible.</returns>
    public NamespaceConstraint? Intersect(NamespaceConstraint other)
    {
        Argument.ThrowIfNull(other);

        if (Equals(other))
        {
            return this;
        }

        if (Variety == NamespaceConstraintVariety.Any)
        {
            return other;
        }

        if (other.Variety == NamespaceConstraintVariety.Any)
        {
            return this;
        }

        if (Variety == NamespaceConstraintVariety.Not && other.Variety == NamespaceConstraintVariety.Not)
        {
            if (Values[0].Length == 0)
            {
                return other;
            }

            return other.Values[0].Length == 0 ? this : null;
        }

        // A set against a negation or a set: keep the members the other side allows (a negation drops its value and absent).
        var set = Variety == NamespaceConstraintVariety.Enumeration ? Values : other.Values;
        var against = Variety == NamespaceConstraintVariety.Enumeration ? other : this;
        var kept = new string[set.Length];
        var count = 0;
        for (var i = 0; i < set.Length; i++)
        {
            if (against.Allows(set[i]))
            {
                kept[count++] = set[i];
            }
        }

        Array.Resize(ref kept, count);
        return Set(kept);
    }

    /// <inheritdoc />
    public bool Equals(NamespaceConstraint? other)
    {
        if (other is null || Variety != other.Variety || Values.Length != other.Values.Length)
        {
            return false;
        }

        for (var i = 0; i < Values.Length; i++)
        {
            if (!string.Equals(Values[i], other.Values[i], StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as NamespaceConstraint);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = (int)Variety;
        for (var i = 0; i < Values.Length; i++)
        {
            hash = unchecked((hash * 31) + StringComparer.Ordinal.GetHashCode(Values[i]));
        }

        return hash;
    }

    /// <summary>Formats the constraint as <c>##any</c>, <c>not(value)</c>, or <c>{a,b}</c>, with <c>##local</c> for no namespace.</summary>
    /// <returns>The text.</returns>
    public override string ToString()
    {
        if (Variety == NamespaceConstraintVariety.Any)
        {
            return "##any";
        }

        var isNot = Variety == NamespaceConstraintVariety.Not;
        var builder = new StringBuilder(isNot ? "not(" : "{");
        for (var i = 0; i < Values.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(',');
            }

            builder.Append(Values[i].Length == 0 ? "##local" : Values[i]);
        }

        return builder.Append(isNot ? ')' : '}').ToString();
    }

    private static bool Contains(string[] values, string value)
    {
        for (var i = 0; i < values.Length; i++)
        {
            if (string.Equals(values[i], value, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static string[] Concat(string[] left, string[] right)
    {
        var all = new string[left.Length + right.Length];
        left.CopyTo(all, 0);
        right.CopyTo(all, left.Length);
        return all;
    }

    private static int CountTokens(string value)
    {
        var count = 0;
        var position = 0;
        while (NextToken(value, ref position, out _, out _))
        {
            count++;
        }

        return count;
    }

    private static bool NextToken(string value, ref int position, out int start, out int length)
    {
        while (position < value.Length && IsXmlSpace(value[position]))
        {
            position++;
        }

        start = position;
        while (position < value.Length && !IsXmlSpace(value[position]))
        {
            position++;
        }

        length = position - start;
        return length > 0;
    }

    private static bool IsXmlSpace(char c) => c is ' ' or '\t' or '\r' or '\n';
}
