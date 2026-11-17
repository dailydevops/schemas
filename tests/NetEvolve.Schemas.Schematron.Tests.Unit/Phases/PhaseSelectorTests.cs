namespace NetEvolve.Schemas.Schematron.Tests.Unit.Phases;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas;
using NetEvolve.Schemas.Schematron;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Phases;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;

public class PhaseSelectorTests
{
    private static readonly DocumentLocation Origin = FakeModel.Origin;

    private static SchematronPattern Pattern(string? id, bool isAbstract = false) =>
        new SchematronPattern(Origin, id, isAbstract, null, null, null, null, [], [], [], [], null);

    private static SchematronGroup Group(string id) =>
        new SchematronGroup(Origin, id, false, null, null, null, null, [], [], [], [], null);

    private static SchematronPhase Phase(
        string id,
        string[] actives,
        SchematronLet[]? lets = null,
        string? from = null,
        string? when = null
    )
    {
        var list = new List<SchematronActive>();
        foreach (var pattern in actives)
        {
            list.Add(new SchematronActive(Origin, [], pattern));
        }
        return new SchematronPhase(Origin, id, from, when, [], lets ?? [], list);
    }

    private static SchematronSchema Schema(
        string? defaultPhase,
        SchematronPhase[] phases,
        params SchematronPatternBase[] patterns
    ) =>
        new(
            Origin,
            null,
            [],
            [],
            [],
            patterns,
            null,
            null,
            "s",
            null,
            null,
            SchematronEdition.Iso2016,
            defaultPhase,
            null,
            [],
            phases,
            [],
            []
        );

    private static (bool Ok, PhaseSelection Selection, FakeProblemSink Problems) Run(
        SchematronSchema schema,
        string phase
    )
    {
        var problems = new FakeProblemSink();
        var ok = PhaseSelector.TryResolve(schema, phase, problems, out var selection);
        return (ok, selection, problems);
    }

    private static string[] Ids(PhaseSelection selection)
    {
        var ids = new List<string>();
        foreach (var pattern in selection.Patterns)
        {
            ids.Add(pattern.Id!);
        }
        return ids.ToArray();
    }

    private static readonly SchematronSchema Sample = Schema(
        "basic",
        [Phase("basic", ["p3", "p1", "p1"]), Phase("empty", [])],
        Pattern("p1"),
        Pattern("abs", true),
        Pattern("p2"),
        Group("p3")
    );

    [Test]
    public async Task HashAll_ActivatesEveryConcretePatternInDocumentOrder()
    {
        var (ok, selection, problems) = Run(Sample, "#ALL");

        await Assert.That(ok).IsTrue();
        await Assert.That(problems.Problems.Count).IsEqualTo(0);
        await Assert.That(selection.EffectivePhase).IsEqualTo("#ALL");
        await Assert.That(Ids(selection)).IsEquivalentTo(["p1", "p2", "p3"]);
    }

    [Test]
    public async Task HashDefault_UsesDefaultPhase()
    {
        var (ok, selection, _) = Run(Sample, "#DEFAULT");

        await Assert.That(ok).IsTrue();
        await Assert.That(selection.EffectivePhase).IsEqualTo("basic");
        await Assert.That(Ids(selection)).IsEquivalentTo(["p1", "p3"]);
    }

    [Test]
    public async Task HashDefault_WithoutDefaultPhase_FallsBackToAll()
    {
        var (ok, selection, _) = Run(Schema(null, [], Pattern("p1"), Pattern("p2")), "#DEFAULT");

        await Assert.That(ok).IsTrue();
        await Assert.That(selection.EffectivePhase).IsEqualTo("#ALL");
        await Assert.That(Ids(selection)).IsEquivalentTo(["p1", "p2"]);
    }

    [Test]
    public async Task HashDefault_DefaultPhaseHashAll_ActivatesEverything()
    {
        var (ok, selection, _) = Run(Schema("#ALL", [], Pattern("p1"), Pattern("p2")), "#DEFAULT");

        await Assert.That(ok).IsTrue();
        await Assert.That(selection.EffectivePhase).IsEqualTo("#ALL");
        await Assert.That(Ids(selection)).IsEquivalentTo(["p1", "p2"]);
    }

    [Test]
    public async Task HashDefault_DefaultPhaseMatchesNothing_ReportsIdrefUnresolved()
    {
        var (ok, _, problems) = Run(Schema("nope", [], Pattern("p1")), "#DEFAULT");

        await Assert.That(ok).IsFalse();
        await Assert.That(problems.Problems.Count).IsEqualTo(1);
        await Assert.That(problems.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.IdrefUnresolved);
    }

    [Test]
    public async Task NamedPhase_ActivatesExactlyTheListedPatterns()
    {
        var (ok, selection, _) = Run(Sample, "basic");

        await Assert.That(ok).IsTrue();
        await Assert.That(selection.EffectivePhase).IsEqualTo("basic");
        await Assert.That(Ids(selection)).IsEquivalentTo(["p1", "p3"]);
    }

    [Test]
    public async Task NamedPhase_WithoutActive_ActivatesNothing()
    {
        var (ok, selection, _) = Run(Sample, "empty");

        await Assert.That(ok).IsTrue();
        await Assert.That(selection.Patterns.Count).IsEqualTo(0);
    }

    [Test]
    [Arguments("missing")]
    [Arguments("#ANY")]
    [Arguments("#all")]
    public async Task UnknownPhase_ReportsPhaseUnknownNamingThePhase(string name)
    {
        var (ok, _, problems) = Run(Sample, name);

        await Assert.That(ok).IsFalse();
        await Assert.That(problems.Problems.Count).IsEqualTo(1);
        await Assert.That(problems.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.PhaseUnknown);
        await Assert.That(problems.Problems[0].Message).Contains(name);
    }

    [Test]
    public async Task Active_PatternMatchesNothing_ReportsIdrefUnresolved()
    {
        var schema = Schema(null, [Phase("p", ["ghost"])], Pattern("p1"));

        var (ok, _, problems) = Run(schema, "p");

        await Assert.That(ok).IsFalse();
        await Assert.That(problems.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.IdrefUnresolved);
        await Assert.That(problems.Problems[0].Message).Contains("ghost");
    }

    [Test]
    [Arguments("x", null)]
    [Arguments(null, "y")]
    public async Task PhaseWithFromOrWhen_ReportsUnsupported_EvenWhenNotSelected(string? from, string? when)
    {
        var schema = Schema(null, [Phase("gated", [], null, from, when)], Pattern("p1"));

        var (ok, _, problems) = Run(schema, "#ALL");

        await Assert.That(ok).IsFalse();
        await Assert.That(problems.Problems.Count).IsEqualTo(1);
        await Assert.That(problems.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.Unsupported);
        await Assert.That(problems.Problems[0].Message).Contains(from is null ? "when" : "from");
    }

    [Test]
    public async Task PhaseLet_IsExposedForTheSelectedPhaseOnly()
    {
        var let = new SchematronLet(Origin, "v", null, "1");
        var schema = Schema(null, [Phase("a", ["p1"], [let]), Phase("b", ["p1"])], Pattern("p1"));

        var (_, withLet, _) = Run(schema, "a");
        var (_, without, _) = Run(schema, "b");
        var (_, all, _) = Run(schema, "#ALL");

        await Assert.That(withLet.Lets.Count).IsEqualTo(1);
        await Assert.That(withLet.Lets[0]).IsSameReferenceAs(let);
        await Assert.That(without.Lets.Count).IsEqualTo(0);
        await Assert.That(all.Lets.Count).IsEqualTo(0);
    }

    [Test]
    public async Task TryResolve_NullArguments_Throw()
    {
        var sink = new FakeProblemSink();

        await Assert.That(() => PhaseSelector.TryResolve(null!, "#ALL", sink, out _)).Throws<ArgumentNullException>();
        await Assert.That(() => PhaseSelector.TryResolve(Sample, null!, sink, out _)).Throws<ArgumentNullException>();
        await Assert.That(() => PhaseSelector.TryResolve(Sample, "#ALL", null!, out _)).Throws<ArgumentNullException>();
    }
}
