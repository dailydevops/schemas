namespace NetEvolve.Schemas.Benchmarks.Xsd.ContentModels;

using BenchmarkDotNet.Attributes;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xsd.ContentModels.Automaton;
using NetEvolve.Schemas.Xsd.Diagnostics;

/// <summary>
/// Compile time of the counting automaton for a counted element, <c>maxOccurs</c> 10, 10,000, and one billion
/// (Structures 3.9). The size of the automaton must not depend on the occurrence value (design #146 DP-7).
/// </summary>
#pragma warning disable CA1515 // BenchmarkDotNet requires public benchmark classes
public class ContentModelCompileBenchmarks
#pragma warning restore CA1515
{
    private readonly AutomatonParticle _max10 = Model(10);
    private readonly AutomatonParticle _max10000 = Model(10_000);
    private readonly AutomatonParticle _maxOneBillion = Model(1_000_000_000);
    private readonly XsdIssueReporter _reporter = new(new ValidationIssueCollector(ValidationLimits.Default));

    /// <summary>Proves the state count is identical for every occurrence value.</summary>
    [GlobalSetup]
    public void Setup()
    {
        var small = Compile(_max10);
        var middle = Compile(_max10000);
        var large = Compile(_maxOneBillion);
        if (
            small.PositionCount != large.PositionCount
            || small.PositionCount != middle.PositionCount
            || small.TransitionCount != large.TransitionCount
            || small.TransitionCount != middle.TransitionCount
        )
        {
            throw new InvalidOperationException(
                $"The state count depends on maxOccurs: {small.PositionCount}/{middle.PositionCount}/{large.PositionCount} positions."
            );
        }

        Console.WriteLine(
            $"Content model state count (maxOccurs 10, 10000, 1000000000): {small.PositionCount} positions, {small.TransitionCount} transitions each."
        );
    }

    /// <summary>Compiles <c>a{1,10} b</c>.</summary>
    /// <returns>The number of positions.</returns>
    [Benchmark]
    public int CompileMaxOccurs10() => Compile(_max10).PositionCount;

    /// <summary>Compiles <c>a{1,10000} b</c>.</summary>
    /// <returns>The number of positions.</returns>
    [Benchmark]
    public int CompileMaxOccurs10000() => Compile(_max10000).PositionCount;

    /// <summary>Compiles <c>a{1,1000000000} b</c>.</summary>
    /// <returns>The number of positions.</returns>
    [Benchmark]
    public int CompileMaxOccursOneBillion() => Compile(_maxOneBillion).PositionCount;

    private static AutomatonParticle Model(long maxOccurs) =>
        AutomatonParticle.Sequence(
            1,
            1,
            AutomatonParticle.Element(1, maxOccurs, 1),
            AutomatonParticle.Element(1, 1, 2)
        );

    private CountingAutomaton Compile(AutomatonParticle root) =>
        CountingAutomatonCompiler.Compile(root, _reporter, default)
        ?? throw new InvalidOperationException("The content model exceeds a compiler ceiling.");
}
