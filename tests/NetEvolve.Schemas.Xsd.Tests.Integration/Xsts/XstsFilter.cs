namespace NetEvolve.Schemas.Xsd.Tests.Integration.Xsts;

/// <summary>The selection criteria as data: one XSD version and the statuses that count as accepted.</summary>
/// <param name="Version">The XSD version token, <see cref="XstsVersions.V10"/> or <see cref="XstsVersions.V11"/>.</param>
/// <param name="Statuses">The statuses a test may have; a test with another status or none is excluded.</param>
internal sealed record XstsFilter(string Version, IReadOnlySet<string> Statuses)
{
    /// <summary>Gets the statuses that mark a test as agreed: the others are submitted, queried, or disputed.</summary>
    public static IReadOnlySet<string> AcceptedStatuses { get; } =
        new HashSet<string>(["accepted", "stable"], StringComparer.Ordinal);
}
