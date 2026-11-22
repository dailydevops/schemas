namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Engine;

/// <summary>Reads the file that links every baseline entry to its issue and checks it against the baseline.</summary>
internal static class KnownFailureLinks
{
    public static IReadOnlyList<KnownFailureLink> Parse(string text) => throw new NotSupportedException();

    public static IReadOnlyList<string> Verify(
        IReadOnlyCollection<string> baselineIds,
        IReadOnlyList<KnownFailureLink> links
    ) => throw new NotSupportedException();
}
