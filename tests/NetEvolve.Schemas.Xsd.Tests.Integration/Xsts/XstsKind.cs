namespace NetEvolve.Schemas.Xsd.Tests.Integration.Xsts;

/// <summary>The kind of a suite test.</summary>
internal enum XstsKind
{
    /// <summary>A schema test: the schema documents are compiled and must be valid or invalid schemas.</summary>
    Schema,

    /// <summary>An instance test: the instance is validated against the schema of its group.</summary>
    Instance,
}
