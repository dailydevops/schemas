namespace NetEvolve.Schemas.Xml.Contracts;

/// <summary>The outcome of <see cref="XmlNameTable.TryIntern"/>.</summary>
public enum XmlNameTableStatus
{
    /// <summary>The text is interned.</summary>
    Ok = 0,

    /// <summary>The table holds <c>MaxDistinctNames</c> names and the text is not one of them (<c>XML1111</c>).</summary>
    LimitExceeded = 1,

    /// <summary>The table is frozen and does not contain the text (<c>XML1116</c>).</summary>
    Frozen = 2,
}
