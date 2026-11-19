namespace NetEvolve.Schemas.Xml.Datatypes.Facets.Ordered;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.DateTime;

/// <summary>The <c>explicitTimezone</c> facet (XSD 1.1 Part 2, section 4.3.14): a date or time value must, or must not, carry a timezone.</summary>
/// <remarks>
/// The facet exists in XSD 1.1 only; <see cref="OrderedFacetRestriction"/> rejects it in 1.0 mode through the applicability table.
/// Limitation: wiring the facet into the schema components and the 1.1 datatypes is a schema-level concern (XSD-25).
/// </remarks>
public sealed class ExplicitTimezoneFacet : IValueFacet<XsdDateTime>
{
    /// <summary>Initializes a new instance of the <see cref="ExplicitTimezoneFacet"/> class.</summary>
    /// <param name="mode">The value of the facet.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="mode"/> is not a defined value.</exception>
    public ExplicitTimezoneFacet(ExplicitTimezoneMode mode)
    {
        Argument.ThrowIfLessThan((int)mode, (int)ExplicitTimezoneMode.Optional);
        Argument.ThrowIfGreaterThan((int)mode, (int)ExplicitTimezoneMode.Prohibited);
        Mode = mode;
    }

    /// <inheritdoc />
    public FacetKind Kind => FacetKind.ExplicitTimezone;

    /// <summary>Gets the value of the facet.</summary>
    public ExplicitTimezoneMode Mode { get; }

    /// <inheritdoc />
    public bool IsSatisfiedBy(in XsdDateTime value) =>
        Mode switch
        {
            ExplicitTimezoneMode.Required => value.TimezoneOffsetMinutes.HasValue,
            ExplicitTimezoneMode.Prohibited => !value.TimezoneOffsetMinutes.HasValue,
            _ => true,
        };
}
