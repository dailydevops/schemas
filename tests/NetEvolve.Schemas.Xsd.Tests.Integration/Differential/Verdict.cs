namespace NetEvolve.Schemas.Xsd.Tests.Integration.Differential;

/// <summary>The verdict of one engine for the schema or the instance of a case.</summary>
internal enum Verdict
{
    /// <summary>The schema is valid, or the instance is valid.</summary>
    Valid,

    /// <summary>The schema is not a valid schema, or the instance is invalid.</summary>
    Invalid,

    /// <summary>Nothing was run: the case has no instance, or the schema was already invalid.</summary>
    NotRun,
}
