namespace NetEvolve.Schemas.Xml.Dtd.Validation.Identity;

/// <summary>The outcome of adding a value to the <see cref="DtdIdTable"/>.</summary>
internal enum DtdIdAddResult
{
    /// <summary>The value was new and is stored.</summary>
    Added,

    /// <summary>The table already holds the value.</summary>
    Duplicate,

    /// <summary>The value is new but the table holds as many values as the limit allows; nothing was stored.</summary>
    LimitExceeded,
}
