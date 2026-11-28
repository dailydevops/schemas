namespace NetEvolve.Schemas.Xsd.ContentModels.Determinism;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.ContentModels.Automaton;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Wildcards.Core;

/// <summary>
/// Decides whether two positions of a content model can match the same element: two element positions share a schema
/// name id, an element position has a name a wildcard allows, or two wildcards allow a common namespace.
/// </summary>
/// <remarks>
/// Limitation: the overlap of two wildcards is decided on the namespace constraints of <see cref="Wildcard"/>, the one
/// implementation of the seam; a wildcard of another implementation is assumed to overlap every other wildcard.
/// </remarks>
internal sealed class ParticleOverlap
{
    private readonly CountingAutomaton _model;
    private readonly IWildcard?[] _wildcards;
    private readonly Func<int, XPathQName> _nameOf;
    private readonly Dictionary<int, XPathQName> _names = [];

    internal ParticleOverlap(CountingAutomaton model, IReadOnlyList<IXsdComponent> terms, Func<int, XPathQName> nameOf)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(terms);
        ArgumentNullException.ThrowIfNull(nameOf);
        ArgumentOutOfRangeException.ThrowIfNotEqual(terms.Count, model.PositionCount);

        _model = model;
        _nameOf = nameOf;
        _wildcards = new IWildcard?[model.PositionCount];
        for (var position = 0; position < _wildcards.Length; position++)
        {
            ArgumentNullException.ThrowIfNull(terms[position]);

            if (model.IsWildcard(position))
            {
                var wildcard = terms[position] as IWildcard;
                ArgumentNullException.ThrowIfNull(wildcard);

                _wildcards[position] = wildcard;
            }
        }
    }

    /// <summary>Determines whether a position is a wildcard.</summary>
    /// <param name="position">The position.</param>
    /// <returns><see langword="true"/> for a wildcard position.</returns>
    internal bool IsWildcard(int position) => _wildcards[position] is not null;

    /// <summary>Determines whether an element position has a name that a wildcard position allows.</summary>
    /// <param name="wildcardPosition">The wildcard position.</param>
    /// <param name="elementPosition">The element position.</param>
    /// <returns><see langword="true"/> when the wildcard allows one of the names.</returns>
    internal bool WildcardAllowsElement(int wildcardPosition, int elementPosition)
    {
        var wildcard = _wildcards[wildcardPosition]!;
        foreach (var id in _model.NamesOf(elementPosition))
        {
            if (!_names.TryGetValue(id, out var name))
            {
                name = _nameOf(id);
                _names.Add(id, name);
            }

            if (wildcard.Allows(name))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Determines whether two wildcard positions allow a common namespace.</summary>
    /// <param name="first">The first wildcard position.</param>
    /// <param name="second">The second wildcard position.</param>
    /// <returns><see langword="true"/> when some namespace is allowed by both.</returns>
    internal bool WildcardsOverlap(int first, int second)
    {
        if (_wildcards[first] is not Wildcard left || _wildcards[second] is not Wildcard right)
        {
            return true;
        }

        // A set overlaps when one of its members is allowed by the other side; two of any or not allow infinitely many.
        if (left.Constraint.Variety != NamespaceConstraintVariety.Enumeration)
        {
            (left, right) = (right, left);
        }

        if (left.Constraint.Variety != NamespaceConstraintVariety.Enumeration)
        {
            return true;
        }

        foreach (var member in left.Constraint.Values)
        {
            if (right.Constraint.Allows(member))
            {
                return true;
            }
        }

        return false;
    }
}
