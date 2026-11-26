namespace NetEvolve.Schemas.Schematron.Tests.Integration.Hostile;

/// <summary>The result of one run: every reported code, the elapsed time, and the resolver that recorded the requests.</summary>
/// <param name="Codes">The diagnostic numbers of the build failure or of the validation issues.</param>
/// <param name="Elapsed">The elapsed time of the run.</param>
/// <param name="Resolver">The resolver of the run.</param>
internal sealed record HostileOutcome(IReadOnlyList<int> Codes, TimeSpan Elapsed, RecordingResolver Resolver);
