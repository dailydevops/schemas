namespace NetEvolve.Schemas.Xsd.Tests.Integration.Differential;

/// <summary>What one engine observed for one case.</summary>
/// <param name="Schema">The verdict for the schema documents.</param>
/// <param name="Instance">The verdict for the instance; <see cref="Verdict.NotRun"/> without an instance or after an invalid schema.</param>
/// <param name="Issues">The positions of the reported issues, in any order.</param>
internal sealed record EngineResult(Verdict Schema, Verdict Instance, IReadOnlyList<IssuePosition> Issues);
