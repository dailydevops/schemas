namespace NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>A facet that constrains the value space, such as <c>maxInclusive</c> and <c>totalDigits</c>.</summary>
/// <typeparam name="TValue">The value type, always a non-ref struct.</typeparam>
public interface IValueFacet<TValue> : IFacet
{
    /// <summary>Determines whether a value satisfies the facet.</summary>
    /// <param name="value">The value.</param>
    /// <returns><see langword="true"/> when satisfied.</returns>
    bool IsSatisfiedBy(in TValue value);
}
