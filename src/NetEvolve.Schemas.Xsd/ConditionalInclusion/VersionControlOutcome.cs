namespace NetEvolve.Schemas.Xsd.ConditionalInclusion;

/// <summary>The result of evaluating one version control attribute.</summary>
internal enum VersionControlOutcome
{
    /// <summary>The attribute does not exclude its element.</summary>
    Keep = 0,

    /// <summary>The attribute excludes its element with all its attributes and descendants.</summary>
    Exclude = 1,

    /// <summary>The value is not valid for the type of the attribute, so the attribute cannot be evaluated (<c>src-cip</c>).</summary>
    Invalid = 2,
}
