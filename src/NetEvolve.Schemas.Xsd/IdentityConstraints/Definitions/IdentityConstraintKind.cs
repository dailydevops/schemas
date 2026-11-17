namespace NetEvolve.Schemas.Xsd.IdentityConstraints.Definitions;

/// <summary>The category of an identity-constraint definition (Structures section 3.11.1).</summary>
internal enum IdentityConstraintKind
{
    Unique = 0,
    Key = 1,
    Keyref = 2,
}
