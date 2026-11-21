namespace NetEvolve.Schemas.Xml.Tests.Integration.Hostile;

/// <summary>How a corpus file is loaded with respect to external access; the document type declaration is processed in every mode.</summary>
internal enum HostileAccess
{
    /// <summary>No external access options: the external subset and the external parameter entities are skipped.</summary>
    Internal,

    /// <summary>External access options with a resolver that serves the file, and every access flag off, which is the default.</summary>
    Denied,

    /// <summary>Every access flag on, with a resolver that serves the resources of the file.</summary>
    Allowed,
}
