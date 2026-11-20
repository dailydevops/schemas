namespace NetEvolve.Schemas.Schematron.Xslt.Keys;

/// <summary>The outcome of turning a key value into its key text.</summary>
internal enum XsltKeyValueStatus : byte
{
    /// <summary>The value has a key text.</summary>
    Ok = 0,

    /// <summary>The value equals no value, not even itself, as <c>NaN</c> does; it is neither indexed nor found.</summary>
    NeverEqual = 1,

    /// <summary>The type of the value is not a key value type of this package.</summary>
    Unsupported = 2,
}
