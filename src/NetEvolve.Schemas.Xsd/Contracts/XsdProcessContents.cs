namespace NetEvolve.Schemas.Xsd.Contracts;

/// <summary>How a wildcard assesses the items it matches.</summary>
internal enum XsdProcessContents
{
    Strict = 0,
    Lax = 1,
    Skip = 2,
}
