namespace NetEvolve.Schemas.Xsd.Tests.Integration.Differential;

/// <summary>The outcome of one differential run.</summary>
/// <param name="BaselineRun"><see langword="true"/> when the baseline engine ran; <see langword="false"/> on every other leg.</param>
/// <param name="Failures">One line per failure; empty when the run passed.</param>
internal sealed record DifferentialReport(bool BaselineRun, IReadOnlyList<string> Failures)
{
    public bool IsSuccess => Failures.Count == 0;
}
