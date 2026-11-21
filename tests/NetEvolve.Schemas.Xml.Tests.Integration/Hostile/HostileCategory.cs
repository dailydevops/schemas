namespace NetEvolve.Schemas.Xml.Tests.Integration.Hostile;

/// <summary>The families of hostile input of the corpus.</summary>
internal enum HostileCategory
{
    /// <summary>An expansion bomb through general entities.</summary>
    GeneralEntityBomb,

    /// <summary>An expansion bomb through parameter entities.</summary>
    ParameterEntityBomb,

    /// <summary>Elements nested beyond the depth limit.</summary>
    DeepNesting,

    /// <summary>A name or reference longer than the name limit.</summary>
    HugeName,

    /// <summary>More attributes or namespace declarations than the limits allow.</summary>
    HugeAttributeCount,

    /// <summary>A text node, value, or token longer than its limit.</summary>
    HugeText,

    /// <summary>A document, node, or name count beyond its limit, or an oversized declaration.</summary>
    OtherLimit,

    /// <summary>An attempt to reach an external resource.</summary>
    ExternalAccess,
}
