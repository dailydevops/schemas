namespace NetEvolve.Schemas.Schematron.Tests.Unit.Loading.XInclude.Suite;

/// <summary>The expected result of a case of the W3C XInclude test suite.</summary>
internal enum XIncludeCaseType
{
    /// <summary>The inclusion succeeds and gives the reference result.</summary>
    Success,

    /// <summary>The inclusion ends with a fatal error.</summary>
    Error,
}
