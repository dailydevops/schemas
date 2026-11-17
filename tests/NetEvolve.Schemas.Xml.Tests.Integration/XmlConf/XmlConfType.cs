namespace NetEvolve.Schemas.Xml.Tests.Integration.XmlConf;

/// <summary>The <c>TYPE</c> of a case: what a conforming reader must do with the document.</summary>
public enum XmlConfType
{
    /// <summary>The document is well-formed and valid; every reader must accept it.</summary>
    Valid,

    /// <summary>The document is well-formed but invalid; a non-validating reader must accept it.</summary>
    Invalid,

    /// <summary>The document is not well-formed; a reader must report a fatal error.</summary>
    NotWellFormed,

    /// <summary>The document is in error but readers are not required to report it.</summary>
    Error,
}
