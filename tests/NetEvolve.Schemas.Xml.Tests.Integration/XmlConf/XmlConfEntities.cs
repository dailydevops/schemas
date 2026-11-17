namespace NetEvolve.Schemas.Xml.Tests.Integration.XmlConf;

/// <summary>The <c>ENTITIES</c> tag of a case: which external entities a reader has to read to see the result.</summary>
public enum XmlConfEntities
{
    /// <summary>No external entity is needed (the default of the catalog).</summary>
    None,

    /// <summary>An external general entity is needed.</summary>
    General,

    /// <summary>An external parameter entity is needed.</summary>
    Parameter,

    /// <summary>External general and parameter entities are needed.</summary>
    Both,
}
