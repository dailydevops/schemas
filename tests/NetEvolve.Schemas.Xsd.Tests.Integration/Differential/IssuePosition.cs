namespace NetEvolve.Schemas.Xsd.Tests.Integration.Differential;

/// <summary>The position of one reported issue in a schema or instance document; messages are never compared.</summary>
/// <param name="Line">The one-based line number, or 0 when the engine reports none.</param>
/// <param name="Column">The one-based column, or 0 when the engine reports none.</param>
internal sealed record IssuePosition(int Line, int Column)
{
    public override string ToString() => $"{Line}:{Column}";
}
