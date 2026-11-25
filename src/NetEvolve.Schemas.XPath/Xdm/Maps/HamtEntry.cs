namespace NetEvolve.Schemas.XPath.Xdm.Maps;

using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>One entry of a <see cref="HamtMap"/>: the key, the value, and the hash of the key.</summary>
internal readonly struct HamtEntry
{
    /// <summary>The key.</summary>
    public readonly XdmItem Key;

    /// <summary>The value.</summary>
    public readonly XdmSequence Value;

    /// <summary>The hash of the key.</summary>
    public readonly int Hash;

    /// <summary>Initializes a new instance of the <see cref="HamtEntry"/> struct.</summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <param name="hash">The hash of the key.</param>
    public HamtEntry(XdmItem key, XdmSequence value, int hash)
    {
        Key = key;
        Value = value;
        Hash = hash;
    }
}
