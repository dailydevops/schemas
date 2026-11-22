namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Runner;

/// <summary>The outcome of validating a case document with a case schema.</summary>
internal enum SchematronOutcome
{
    /// <summary>The schema is valid and the instance has no failed assertion.</summary>
    Valid,

    /// <summary>The instance has at least one failed assertion or successful report.</summary>
    Invalid,

    /// <summary>The schema or its processing is in error.</summary>
    Error,

    /// <summary>Nothing was observed, because no validator is bound or the run did not end in an outcome; never an expected outcome.</summary>
    NotImplemented,
}
