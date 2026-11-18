namespace NetEvolve.Schemas.Xml.Dtd.Contracts;

/// <summary>The four clauses of the validity constraint Standalone Document Declaration (XML 1.0, section 2.9); one code, one discriminator.</summary>
internal enum XmlStandaloneClause
{
    /// <summary>An attribute with a default value declared externally is absent from the document.</summary>
    Defaults = 0,

    /// <summary>An entity declared externally is referenced.</summary>
    Entities = 1,

    /// <summary>An attribute value would change by normalization with a type declared externally.</summary>
    Normalization = 2,

    /// <summary>An element with element content declared externally contains white space.</summary>
    WhiteSpace = 3,
}
