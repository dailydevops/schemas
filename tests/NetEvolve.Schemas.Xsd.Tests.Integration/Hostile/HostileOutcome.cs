namespace NetEvolve.Schemas.Xsd.Tests.Integration.Hostile;

/// <summary>What a run ended with.</summary>
/// <param name="Codes">The diagnostic numbers of the issues of the run, in report order.</param>
/// <param name="Elapsed">The time the run took.</param>
/// <param name="Resolver">The resolver of the run.</param>
internal sealed record HostileOutcome(IReadOnlyList<int> Codes, TimeSpan Elapsed, RecordingResolver Resolver);
