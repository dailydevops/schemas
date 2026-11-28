namespace NetEvolve.Schemas.Xml.Datatypes.Core;

using NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>An inclusive bound facet that another bound of the same kind can make redundant for evaluation.</summary>
/// <typeparam name="TValue">The value type the facet checks.</typeparam>
internal interface IFoldableBound<TValue> : IValueFacet<TValue>
{
    /// <summary>Determines whether <paramref name="other"/> rejects every value this bound rejects, so this bound adds nothing to it.</summary>
    /// <param name="other">The facet to compare with.</param>
    /// <returns><see langword="true"/> when <paramref name="other"/> is a bound of the same kind that is at least as strict as this one.</returns>
    bool IsImpliedBy(IValueFacet<TValue> other);
}
