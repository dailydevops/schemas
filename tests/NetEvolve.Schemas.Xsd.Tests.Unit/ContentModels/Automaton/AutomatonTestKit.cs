namespace NetEvolve.Schemas.Xsd.Tests.Unit.ContentModels.Automaton;

using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xsd.ContentModels.Automaton;
using NetEvolve.Schemas.Xsd.Diagnostics;

/// <summary>Builds hand-made particles and runs name sequences through the compiled automaton.</summary>
internal static class AutomatonTestKit
{
    public const int A = 1;
    public const int B = 2;
    public const int C = 3;
    public const int D = 4;
    public const long Unbounded = AutomatonParticle.Unbounded;

    public static AutomatonParticle El(int id, long min = 1, long max = 1) => AutomatonParticle.Element(min, max, id);

    public static AutomatonParticle Seq(long min, long max, params AutomatonParticle[] children) =>
        AutomatonParticle.Sequence(min, max, children);

    public static AutomatonParticle Seq(params AutomatonParticle[] children) =>
        AutomatonParticle.Sequence(1, 1, children);

    public static AutomatonParticle Cho(long min, long max, params AutomatonParticle[] children) =>
        AutomatonParticle.Choice(min, max, children);

    public static AutomatonParticle Cho(params AutomatonParticle[] children) =>
        AutomatonParticle.Choice(1, 1, children);

    public static CountingAutomaton Compile(AutomatonParticle root) => Compile(root, out _);

    public static CountingAutomaton Compile(AutomatonParticle root, out ValidationIssueCollector issues)
    {
        issues = new ValidationIssueCollector(ValidationLimits.Default);
        var automaton = CountingAutomatonCompiler.Compile(root, new XsdIssueReporter(issues), default);
        return automaton!;
    }

    /// <summary>Returns <see langword="true"/> when every id is accepted in turn and the content may end afterwards.</summary>
    public static bool Accepts(CountingAutomaton automaton, params int[] ids)
    {
        var run = automaton.CreateRun();
        foreach (var id in ids)
        {
            if (!run.TryStep(id, out _))
            {
                return false;
            }
        }

        return run.CanEnd();
    }

    /// <summary>Repeats an id.</summary>
    public static int[] Rep(int id, int count)
    {
        var ids = new int[count];
        for (var i = 0; i < count; i++)
        {
            ids[i] = id;
        }

        return ids;
    }
}
