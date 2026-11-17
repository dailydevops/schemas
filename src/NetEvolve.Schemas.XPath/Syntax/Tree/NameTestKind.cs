namespace NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>Shape of a name test (productions NameTest and Wildcard).</summary>
internal enum NameTestKind
{
    /// <summary>A QName, prefix unresolved.</summary>
    QualifiedName = 0,

    /// <summary><c>*</c></summary>
    AnyName = 1,

    /// <summary><c>prefix:*</c></summary>
    AnyLocalName = 2,

    /// <summary><c>*:local</c></summary>
    AnyNamespace = 3,
}
