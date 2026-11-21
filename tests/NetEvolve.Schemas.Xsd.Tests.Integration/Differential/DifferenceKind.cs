namespace NetEvolve.Schemas.Xsd.Tests.Integration.Differential;

/// <summary>What differs between this library and the baseline for a case.</summary>
internal enum DifferenceKind
{
    /// <summary>The verdicts for the schema documents differ.</summary>
    SchemaVerdict,

    /// <summary>The verdicts for the instance differ.</summary>
    InstanceVerdict,

    /// <summary>The verdicts agree, but the positions of the reported issues differ.</summary>
    IssuePositions,
}
