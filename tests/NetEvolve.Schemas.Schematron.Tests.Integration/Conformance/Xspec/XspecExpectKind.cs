namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Xspec;

/// <summary>The declarative Schematron expectations of XSpec.</summary>
internal enum XspecExpectKind
{
    /// <summary><c>x:expect-assert</c>: a failed assertion matches.</summary>
    Assert,

    /// <summary><c>x:expect-not-assert</c>: a rule fired and no failed assertion matches.</summary>
    NotAssert,

    /// <summary><c>x:expect-report</c>: a successful report matches.</summary>
    Report,

    /// <summary><c>x:expect-not-report</c>: a rule fired and no successful report matches.</summary>
    NotReport,

    /// <summary><c>x:expect-valid</c>: a rule fired and nothing with the role error or fatal (or without a role) was reported.</summary>
    Valid,
}
