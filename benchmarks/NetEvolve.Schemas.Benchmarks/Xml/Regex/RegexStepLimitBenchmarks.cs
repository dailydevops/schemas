namespace NetEvolve.Schemas.Benchmarks.Xml.Regex;

using BenchmarkDotNet.Attributes;
using NetEvolve.Schemas.Xml.Regex.Contracts;
using NetEvolve.Schemas.Xml.Regex.Matching;

/// <summary>
/// A pathological pattern on a long input that cannot match: the matcher aborts at the step ceiling, so the cost is
/// bounded by the limit, not by the input. <c>System.Xml</c> has no such bound, so there is no baseline.
/// </summary>
#pragma warning disable CA1515 // BenchmarkDotNet requires public benchmark classes
public class RegexStepLimitBenchmarks
#pragma warning restore CA1515
{
    private readonly RegexProgram _program = RegexBenchmarkCompiler.Compile("(a*)*b");
    private readonly string _input = new('a', 100_000);
    private readonly RegexStepLimits _limits = RegexStepLimits.Default.WithMaxNfaSteps(100_000);

    /// <summary>Proves that the match reaches the ceiling.</summary>
    [GlobalSetup]
    public void Setup()
    {
        if (MatchUntilStepLimit() != (int)RegexMatchResult.Aborted)
        {
            throw new InvalidOperationException("The pathological pattern did not reach the step ceiling.");
        }
    }

    /// <summary>Matches until the step ceiling aborts the match.</summary>
    /// <returns>The match result as an integer.</returns>
    [Benchmark]
    public int MatchUntilStepLimit() => (int)_program.Match(_input.AsSpan(), _limits);
}
