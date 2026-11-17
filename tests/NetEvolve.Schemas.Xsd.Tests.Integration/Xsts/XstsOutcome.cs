namespace NetEvolve.Schemas.Xsd.Tests.Integration.Xsts;

/// <summary>The outcome of compiling a schema or validating an instance.</summary>
internal enum XstsOutcome
{
    /// <summary>The schema is valid, or the instance is valid.</summary>
    Valid,

    /// <summary>The schema is not a valid schema, or the instance is invalid.</summary>
    Invalid,

    /// <summary>No validator is bound, so nothing was observed; never an expected outcome.</summary>
    NotImplemented,
}
