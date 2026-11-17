namespace NetEvolve.Schemas.Xml.Tests.Integration.XmlConf;

/// <summary>What a reader observed when it read the document of a case.</summary>
internal enum XmlConfObservation
{
    /// <summary>The reader reported no fatal error.</summary>
    Accepted,

    /// <summary>The reader reported a fatal error (well-formedness or namespace constraint).</summary>
    FatalError,

    /// <summary>No reader is bound, so nothing was observed; never an expected outcome.</summary>
    NotImplemented,
}
