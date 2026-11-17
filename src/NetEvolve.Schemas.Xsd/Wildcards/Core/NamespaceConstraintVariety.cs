namespace NetEvolve.Schemas.Xsd.Wildcards.Core;

/// <summary>The variety of a wildcard namespace constraint (Structures 3.10.1).</summary>
internal enum NamespaceConstraintVariety
{
    /// <summary>Any namespace, including no namespace.</summary>
    Any = 0,

    /// <summary>Every namespace except one value and except no namespace.</summary>
    Not = 1,

    /// <summary>A set of namespace names and possibly no namespace.</summary>
    Enumeration = 2,
}
