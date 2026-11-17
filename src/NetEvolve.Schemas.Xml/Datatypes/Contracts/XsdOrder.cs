namespace NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>The outcome of comparing two values of an ordered datatype.</summary>
public enum XsdOrder
{
    /// <summary>The first value is less than the second.</summary>
    Less = 0,

    /// <summary>The values are equal in the value space of the version.</summary>
    Equal = 1,

    /// <summary>The first value is greater than the second.</summary>
    Greater = 2,

    /// <summary>The values are not ordered against each other, for example durations, timezone-less date and time values, and NaN.</summary>
    Incomparable = 3,
}
