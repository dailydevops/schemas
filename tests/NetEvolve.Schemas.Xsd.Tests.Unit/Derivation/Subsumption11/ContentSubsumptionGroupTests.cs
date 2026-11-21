namespace NetEvolve.Schemas.Xsd.Tests.Unit.Derivation.Subsumption11;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Elements;
using static NetEvolve.Schemas.Xsd.Tests.Unit.Derivation.Particles10.ParticleKit;
using static NetEvolve.Schemas.Xsd.Tests.Unit.Derivation.Subsumption11.SubsumptionKit;

/// <summary>Sequences, choices, and nested groups: the language of the restriction must lie inside that of the base.</summary>
public class ContentSubsumptionGroupTests
{
    [Test]
    public async Task Sequence_AgainstItself_IsSubsumed() =>
        await AssertSubsumed(Seq([El("a"), El("b")]), Seq([El("a"), El("b")]));

    [Test]
    public async Task Sequence_ThatEndsEarly_IsNotSubsumed_AndIsLocatedAtTheLastParticle() =>
        await AssertNotSubsumed(Seq([El("a", line: 5)], line: 4), Seq([El("a"), El("b")]), line: 5);

    [Test]
    public async Task Sequence_WithAnExtraElement_IsNotSubsumed_AndIsLocatedAtTheExtraParticle() =>
        await AssertNotSubsumed(Seq([El("a"), El("b", line: 7)]), Seq([El("a")]), line: 7);

    [Test]
    public async Task Sequence_InAnotherOrder_IsNotSubsumed() =>
        await AssertNotSubsumed(Seq([El("b", line: 3), El("a")]), Seq([El("a"), El("b")]), line: 3);

    [Test]
    public async Task Sequence_ThatMakesAnOptionalElementRequired_IsSubsumed() =>
        await AssertSubsumed(Seq([El("a"), El("b")]), Seq([El("a"), El("b", 0, 1)]));

    [Test]
    public async Task Sequence_ThatMakesARequiredElementOptional_IsNotSubsumed() =>
        await AssertNotSubsumed(Seq([El("a", line: 6), El("b", 0, 1)]), Seq([El("a"), El("b")]), line: 6);

    [Test]
    public async Task EmptyContent_IsSubsumedByEmptiableContentOnly()
    {
        await AssertSubsumed(Seq([]), Seq([El("a", 0, 1)]));
        await AssertNotSubsumed(Seq([], line: 9), Seq([El("a")]), line: 9);
    }

    [Test]
    public async Task Choice_WithFewerAlternatives_IsSubsumed() =>
        await AssertSubsumed(Cho([El("a"), El("b")]), Cho([El("a"), El("b"), El("c")]));

    [Test]
    public async Task Choice_WithAnotherAlternative_IsNotSubsumed_AndIsLocatedAtThatAlternative() =>
        await AssertNotSubsumed(Cho([El("a"), El("b"), El("c", line: 8)]), Cho([El("a"), El("b")]), line: 8);

    [Test]
    public async Task Choice_AlternativesInAnotherOrder_AreSubsumed() =>
        await AssertSubsumed(Cho([El("b"), El("a")]), Cho([El("a"), El("b")]));

    [Test]
    public async Task NestedGroups_WithAChoiceInASequence_AreCompared()
    {
        await AssertSubsumed(Seq([El("a"), Cho([El("b"), El("c")])]), Seq([El("a"), Cho([El("b"), El("c"), El("d")])]));
        await AssertNotSubsumed(
            Seq([El("a"), Cho([El("b"), El("c"), El("d", line: 12)])]),
            Seq([El("a"), Cho([El("b"), El("c")])]),
            line: 12
        );
    }

    [Test]
    public async Task NestedGroups_WithASequenceInAChoice_AreCompared()
    {
        await AssertSubsumed(Cho([Seq([El("a"), El("b")])]), Cho([Seq([El("a"), El("b")]), El("c")]));
        await AssertNotSubsumed(
            Cho([Seq([El("a"), El("x", line: 13)])]),
            Cho([Seq([El("a"), El("b")]), El("c")]),
            line: 13
        );
    }

    [Test]
    public async Task NestedGroups_WithAGroupThatMayBeEmpty_AreCompared() =>
        await AssertSubsumed(Seq([El("a"), El("c")]), Seq([El("a"), Seq([El("b")], 0, 1), El("c")]));

    [Test]
    public async Task GroupDefinitions_AreCompared_ByTheirGroups()
    {
        await AssertSubsumed(
            Ref(ModelGroupCompositor.Sequence, [El("a"), El("b")]),
            Ref(ModelGroupCompositor.Sequence, [El("a"), El("b", 0, 1)])
        );
        await AssertNotSubsumed(
            Ref(ModelGroupCompositor.Sequence, [El("a", line: 14), El("b", 0, 1)]),
            Ref(ModelGroupCompositor.Sequence, [El("a"), El("b")]),
            line: 14
        );
    }

    [Test]
    public async Task GroupDefinition_UsedTwice_IsNotCircular()
    {
        var shared = ModelGroupDefinition.Create(
            new XPathQName(Tns, "shared"),
            At(1),
            ModelGroup.Create(ModelGroupCompositor.Sequence, At(1), [El("a"), El("b")])
        );
        Particle Use() => Particle.Create(At(1), 1, 1, shared);

        await AssertSubsumed(Seq([Use(), Use()]), Seq([Use(), Use()]));
        await AssertNotSubsumed(Seq([Use(), Use()]), Seq([Use(), Use(), Use()]));
    }

    [Test]
    public async Task ParticleThatCannotOccur_IsLeftOut()
    {
        await AssertSubsumed(Seq([El("a"), El("b", 0, 0)]), Seq([El("a")]));
        await AssertSubsumed(Seq([El("a"), El("b", 3, 2)]), Seq([El("a")]));
        await AssertSubsumed(El("a", 0, 0), Seq([]));
    }

    [Test]
    public async Task ElementsInAnotherNamespace_AreOtherElements() =>
        await AssertNotSubsumed(El("a", ns: "urn:other", line: 2), El("a"), line: 2);

    [Test]
    public async Task Violation_IsTheSameOnEveryRun()
    {
        var first = Check(Cho([El("x", line: 2), El("y", line: 3)]), Cho([El("a")]));
        var second = Check(Cho([El("x", line: 2), El("y", line: 3)]), Cho([El("a")]));

        await Assert.That(first.Violation.Location.Line).IsEqualTo(second.Violation.Location.Line);
        await Assert.That(first.Violation.Message).IsEqualTo(second.Violation.Message);
        await Assert.That(first.Work).IsEqualTo(second.Work);
    }
}
