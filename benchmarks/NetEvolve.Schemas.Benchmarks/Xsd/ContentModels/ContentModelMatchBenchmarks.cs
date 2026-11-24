namespace NetEvolve.Schemas.Benchmarks.Xsd.ContentModels;

using BenchmarkDotNet.Attributes;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xsd.ContentModels.Automaton;
using NetEvolve.Schemas.Xsd.Diagnostics;

/// <summary>
/// Match step throughput of the counting automaton for a sequence, a choice, and nested counted groups. Each
/// operation is one step (<c>OperationsPerInvoke</c>); the allocation per step must be zero.
/// </summary>
#pragma warning disable CA1515 // BenchmarkDotNet requires public benchmark classes
public class ContentModelMatchBenchmarks
#pragma warning restore CA1515
{
    private const int Steps = 64;

    private readonly int[] _sequenceInput = [1, 2, 3, 4];
    private readonly int[] _choiceInput = Repeat([1, 2, 3, 4], Steps);
    private readonly int[] _nestedInput = Repeat([1, 2], Steps);
    private CountingRun _sequence = null!;
    private CountingRun _choice = null!;
    private CountingRun _nested = null!;

    /// <summary>Compiles the models and proves that a step allocates nothing.</summary>
    [GlobalSetup]
    public void Setup()
    {
        // a b c d
        _sequence = Run(
            AutomatonParticle.Sequence(
                1,
                1,
                AutomatonParticle.Element(1, 1, 1),
                AutomatonParticle.Element(1, 1, 2),
                AutomatonParticle.Element(1, 1, 3),
                AutomatonParticle.Element(1, 1, 4)
            )
        );

        // (a | b | c | d)*
        _choice = Run(
            AutomatonParticle.Choice(
                0,
                AutomatonParticle.Unbounded,
                AutomatonParticle.Element(1, 1, 1),
                AutomatonParticle.Element(1, 1, 2),
                AutomatonParticle.Element(1, 1, 3),
                AutomatonParticle.Element(1, 1, 4)
            )
        );

        // ((a b){1,10}){1,1000000000}
        _nested = Run(
            AutomatonParticle.Sequence(
                1,
                1_000_000_000,
                AutomatonParticle.Sequence(
                    1,
                    10,
                    AutomatonParticle.Element(1, 1, 1),
                    AutomatonParticle.Element(1, 1, 2)
                )
            )
        );

        // Warm up, then measure: the first calls may allocate (JIT, lazy buffers).
        _ = Sequence() + Choice() + Nested();
        var before = GC.GetAllocatedBytesForCurrentThread();
        _ = Sequence() + Choice() + Nested();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        if (allocated != 0)
        {
            throw new InvalidOperationException($"A match step allocates: {allocated} bytes.");
        }
    }

    /// <summary>Matches <c>a b c d</c>.</summary>
    /// <returns>The number of accepted steps.</returns>
    [Benchmark(OperationsPerInvoke = 4)]
    public int MatchSequence() => Sequence();

    /// <summary>Matches 64 children of <c>(a | b | c | d)*</c>.</summary>
    /// <returns>The number of accepted steps.</returns>
    [Benchmark(OperationsPerInvoke = Steps)]
    public int MatchChoice() => Choice();

    /// <summary>Matches 64 children of the nested counted groups <c>((a b){1,10}){1,1000000000}</c>.</summary>
    /// <returns>The number of accepted steps.</returns>
    [Benchmark(OperationsPerInvoke = Steps)]
    public int MatchNestedCountedGroups() => Nested();

    private int Sequence() => Match(_sequence, _sequenceInput);

    private int Choice() => Match(_choice, _choiceInput);

    private int Nested() => Match(_nested, _nestedInput);

    private static int Match(CountingRun run, int[] input)
    {
        run.Reset();
        var accepted = 0;
        foreach (var id in input)
        {
            if (run.TryStep(id, out _))
            {
                accepted++;
            }
        }

        return run.CanEnd() ? accepted : -1;
    }

    private static CountingRun Run(AutomatonParticle root)
    {
        var issues = new ValidationIssueCollector(ValidationLimits.Default);
        var automaton = CountingAutomatonCompiler.Compile(root, new XsdIssueReporter(issues), default)
            ?? throw new InvalidOperationException("The content model exceeds a compiler ceiling.");

        return automaton.CreateRun();
    }

    private static int[] Repeat(int[] pattern, int length)
    {
        var result = new int[length];
        for (var i = 0; i < length; i++)
        {
            result[i] = pattern[i % pattern.Length];
        }

        return result;
    }
}
