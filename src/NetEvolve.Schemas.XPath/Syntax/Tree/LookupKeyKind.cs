namespace NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>The form of the key specifier of a lookup (production KeySpecifier, XPath 3.1).</summary>
internal enum LookupKeyKind
{
    /// <summary>An NCName, as in <c>?name</c>.</summary>
    Name,

    /// <summary>An integer literal, as in <c>?1</c>.</summary>
    Integer,

    /// <summary>A parenthesized expression, as in <c>?(expr)</c>.</summary>
    Expression,

    /// <summary>The wildcard, <c>?*</c>.</summary>
    Wildcard,
}
