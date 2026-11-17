namespace NetEvolve.Schemas.Xsd.SchemaDocuments;

/// <summary>The lexical space of a schema attribute value.</summary>
internal enum SchemaAttributeKind
{
    /// <summary>Any string.</summary>
    Any = 0,

    /// <summary><c>xs:boolean</c>.</summary>
    Boolean,

    /// <summary><c>xs:NCName</c>.</summary>
    NCName,

    /// <summary><c>xs:QName</c>.</summary>
    QName,

    /// <summary>A white space separated list of <c>xs:QName</c> values, possibly empty.</summary>
    QNameList,

    /// <summary><c>xs:nonNegativeInteger</c>.</summary>
    UnsignedInteger,

    /// <summary><c>unbounded</c> or <c>xs:nonNegativeInteger</c>.</summary>
    MaxOccurs,

    /// <summary>One token of a fixed set.</summary>
    Token,

    /// <summary>A white space separated list of tokens of a fixed set, or <c>#all</c>.</summary>
    TokenList,
}
