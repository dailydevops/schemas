namespace NetEvolve.Schemas.Xsd.Tests.Unit.OpenContent;

using System;
using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.ContentModels.AllGroups;
using NetEvolve.Schemas.Xsd.ContentModels.Automaton;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.OpenContent;
using NetEvolve.Schemas.Xsd.Tests.Unit.ContentModels.Automaton;
using NetEvolve.Schemas.Xsd.Wildcards.Core;

public class OpenContentRunTests
{
    private const int A = AutomatonTestKit.A;
    private const int B = AutomatonTestKit.B;

    private const string Foreign = "urn:foreign";

    // A name the content model does not know (id -1) in the foreign namespace, and one in no namespace.
    private static readonly XPathQName ForeignName = new(Foreign, "x");
    private static readonly XPathQName LocalName = new(string.Empty, "x");

    private static OpenContentProperty Open(OpenContentMode mode, NamespaceConstraint? constraint = null) =>
        OpenContentProperty.Create(
            mode,
            Wildcard.Create(constraint ?? NamespaceConstraint.Any, XsdProcessContents.Lax, default)
        );

    // (a, b)
    private static OpenContentRun Sequence(OpenContentMode mode, NamespaceConstraint? constraint = null) =>
        new(
            Open(mode, constraint),
            AutomatonTestKit.Compile(AutomatonTestKit.Seq(AutomatonTestKit.El(A), AutomatonTestKit.El(B))).CreateRun()
        );

    // (a, b?)
    private static OpenContentRun SequenceWithOptionalB(OpenContentMode mode, NamespaceConstraint? constraint = null) =>
        new(
            Open(mode, constraint),
            AutomatonTestKit
                .Compile(AutomatonTestKit.Seq(AutomatonTestKit.El(A), AutomatonTestKit.El(B, 0, 1)))
                .CreateRun()
        );

    private static OpenContentRun AllOfAAndB(OpenContentMode mode) =>
        new(
            Open(mode),
            new AllGroup(
                false,
                [
                    [A],
                    [B],
                ],
                [false, false]
            ).CreateRun()
        );

    private static bool Declared(ref OpenContentRun run, int id, out bool open) =>
        Step(ref run, id, ForeignName, out open);

    private static bool Foreigner(ref OpenContentRun run, out bool open) => Step(ref run, -1, ForeignName, out open);

    private static bool Step(ref OpenContentRun run, int id, XPathQName name, out bool open) =>
        run.TryStep(id, name, out _, out open);

    [Test]
    public async Task OpenContent_InterleaveAndSuffix_WrapStep()
    {
        var interleave = Sequence(OpenContentMode.Interleave);
        var suffix = Sequence(OpenContentMode.Suffix);

        // Interleaved: before, between, and after the declared elements, and the state does not move.
        await Assert.That(Foreigner(ref interleave, out var o1)).IsTrue();
        await Assert.That(o1).IsTrue();
        await Assert.That(Declared(ref interleave, A, out var o2)).IsTrue();
        await Assert.That(o2).IsFalse();
        await Assert.That(Foreigner(ref interleave, out _)).IsTrue();
        await Assert.That(Declared(ref interleave, B, out _)).IsTrue();
        await Assert.That(Foreigner(ref interleave, out _)).IsTrue();
        await Assert.That(interleave.CanEnd()).IsTrue();

        // Suffix: only after the declared content is complete.
        await Assert.That(Foreigner(ref suffix, out _)).IsFalse();
        await Assert.That(Declared(ref suffix, A, out _)).IsTrue();
        await Assert.That(Foreigner(ref suffix, out _)).IsFalse();
        await Assert.That(Declared(ref suffix, B, out _)).IsTrue();
        await Assert.That(Foreigner(ref suffix, out var o3)).IsTrue();
        await Assert.That(o3).IsTrue();
        await Assert.That(Foreigner(ref suffix, out _)).IsTrue();
        await Assert.That(suffix.CanEnd()).IsTrue();
    }

    [Test]
    public async Task Interleave_ElementsBetweenTheDeclaredOnes_DoNotCompleteTheContent()
    {
        var run = Sequence(OpenContentMode.Interleave);

        _ = Declared(ref run, A, out _);
        _ = Foreigner(ref run, out _);

        await Assert.That(run.CanEnd()).IsFalse();
        await Assert.That(Declared(ref run, B, out _)).IsTrue();
        await Assert.That(run.CanEnd()).IsTrue();
    }

    [Test]
    public async Task Interleave_DeclaredElementOutOfOrder_IsAcceptedAsOpenContentWhenTheWildcardAllowsIt()
    {
        var run = Sequence(OpenContentMode.Interleave);

        // b is declared, but not allowed here; the wildcard allows it, so it is open content and the state stays.
        var accepted = run.TryStep(B, new XPathQName(string.Empty, "b"), out _, out var open);

        await Assert.That(accepted).IsTrue();
        await Assert.That(open).IsTrue();
        await Assert.That(Declared(ref run, A, out var byOpen)).IsTrue();
        await Assert.That(byOpen).IsFalse();
    }

    [Test]
    public async Task Interleave_ElementTheWildcardDoesNotAllow_IsRejectedAndTheRunStays()
    {
        // ##other allows neither the target namespace nor no namespace.
        var run = Sequence(OpenContentMode.Interleave, NamespaceConstraint.Not("urn:t"));

        await Assert.That(Step(ref run, -1, LocalName, out _)).IsFalse();
        await Assert.That(Foreigner(ref run, out _)).IsTrue();
        await Assert.That(Declared(ref run, A, out _)).IsTrue();
        await Assert.That(Declared(ref run, B, out _)).IsTrue();
        await Assert.That(run.CanEnd()).IsTrue();
    }

    [Test]
    public async Task DeclaredElement_TakesPriorityOverTheOpenContentWildcard()
    {
        var run = Sequence(OpenContentMode.Interleave);

        // The wildcard allows every name, including a, but a is the declared element at this position.
        var accepted = run.TryStep(A, new XPathQName(string.Empty, "a"), out var matched, out var open);

        await Assert.That(accepted).IsTrue();
        await Assert.That(open).IsFalse();
        await Assert.That(matched.ParticleIndex).IsEqualTo(0);
    }

    [Test]
    public async Task Suffix_AfterTheFirstOpenElement_DeclaredElementsMatchNoMore()
    {
        // (a, b?) with a suffix wildcard that excludes the local names: the content is complete after a.
        var run = SequenceWithOptionalB(OpenContentMode.Suffix, NamespaceConstraint.Not("urn:t"));

        await Assert.That(Declared(ref run, A, out _)).IsTrue();
        await Assert.That(Foreigner(ref run, out var open)).IsTrue();
        await Assert.That(open).IsTrue();

        // b would continue the content model, but the suffix has begun, and the wildcard does not allow b.
        await Assert.That(run.TryStep(B, new XPathQName(string.Empty, "b"), out _, out _)).IsFalse();
        await Assert.That(Foreigner(ref run, out _)).IsTrue();
        await Assert.That(run.CanEnd()).IsTrue();
    }

    [Test]
    public async Task Suffix_ElementsAfterCompleteContent_IncludeTheOptionalDeclaredOnesFirst()
    {
        var run = SequenceWithOptionalB(OpenContentMode.Suffix);

        await Assert.That(Declared(ref run, A, out _)).IsTrue();
        await Assert.That(Declared(ref run, B, out var byOpen)).IsTrue();
        await Assert.That(byOpen).IsFalse();
        await Assert.That(Foreigner(ref run, out var viaOpen)).IsTrue();
        await Assert.That(viaOpen).IsTrue();
    }

    [Test]
    public async Task AllGroup_InterleaveAcceptsOpenContentAroundAndBetweenTheMembers()
    {
        var run = AllOfAAndB(OpenContentMode.Interleave);

        await Assert.That(Foreigner(ref run, out _)).IsTrue();
        await Assert.That(Declared(ref run, B, out var byOpen)).IsTrue();
        await Assert.That(byOpen).IsFalse();
        await Assert.That(Foreigner(ref run, out _)).IsTrue();
        await Assert.That(run.CanEnd()).IsFalse();
        await Assert.That(Declared(ref run, A, out _)).IsTrue();
        await Assert.That(run.CanEnd()).IsTrue();
    }

    [Test]
    public async Task AllGroup_RepeatedMember_IsOpenContentWhenTheWildcardAllowsIt()
    {
        var run = AllOfAAndB(OpenContentMode.Interleave);

        _ = Declared(ref run, A, out _);
        var accepted = run.TryStep(A, new XPathQName(string.Empty, "a"), out _, out var open);

        await Assert.That(accepted).IsTrue();
        await Assert.That(open).IsTrue();
    }

    [Test]
    public async Task AllGroup_SuffixAcceptsOpenContentOnlyAfterEveryRequiredMember()
    {
        var run = AllOfAAndB(OpenContentMode.Suffix);

        _ = Declared(ref run, B, out _);
        await Assert.That(Foreigner(ref run, out _)).IsFalse();
        _ = Declared(ref run, A, out _);
        await Assert.That(Foreigner(ref run, out var open)).IsTrue();
        await Assert.That(open).IsTrue();
    }

    [Test]
    public async Task WildcardExpected_IsAlwaysTrueForInterleave_AndForSuffixOnlyWhenTheContentMayEnd()
    {
        var interleave = Sequence(OpenContentMode.Interleave);
        var suffix = Sequence(OpenContentMode.Suffix);

        await Assert.That(interleave.WildcardExpected).IsTrue();
        await Assert.That(suffix.WildcardExpected).IsFalse();

        _ = Declared(ref suffix, A, out _);
        await Assert.That(suffix.WildcardExpected).IsFalse();

        _ = Declared(ref suffix, B, out _);
        await Assert.That(suffix.WildcardExpected).IsTrue();
    }

    [Test]
    public async Task Reset_ReturnsToTheStateBeforeTheFirstChild_AlsoTheSuffix()
    {
        var run = SequenceWithOptionalB(OpenContentMode.Suffix);
        _ = Declared(ref run, A, out _);
        _ = Foreigner(ref run, out _);

        run.Reset();

        await Assert.That(run.CanEnd()).IsFalse();
        await Assert.That(Declared(ref run, A, out var byOpen)).IsTrue();
        await Assert.That(byOpen).IsFalse();
        await Assert.That(Declared(ref run, B, out _)).IsTrue();
    }

    [Test]
    public async Task Step_AllocatesZeroBytes()
    {
        var run = Sequence(OpenContentMode.Interleave);
        var all = AllOfAAndB(OpenContentMode.Suffix);

        AllocationAssert.NoAllocation(() =>
        {
            run.Reset();
            all.Reset();
            for (var i = 0; i < 100; i++)
            {
                _ = Foreigner(ref run, out _);
                _ = Declared(ref run, A, out _);
                _ = Declared(ref all, B, out _);
                _ = Declared(ref all, A, out _);
                _ = Foreigner(ref all, out _);
                _ = run.WildcardExpected;
            }
        });

        await Assert.That(run.IsActive).IsTrue();
    }

    [Test]
    public async Task Default_IsNotActive()
    {
        var run = default(OpenContentRun);

        await Assert.That(run.IsActive).IsFalse();
    }

    [Test]
    public async Task Constructors_RejectNullArguments()
    {
        var open = Open(OpenContentMode.Interleave);
        var counting = AutomatonTestKit.Compile(AutomatonTestKit.Seq(AutomatonTestKit.El(A))).CreateRun();
        var all = new AllGroup(
            false,
            [
                [A],
            ],
            [false]
        ).CreateRun();

        await Assert.That(() => new OpenContentRun(null!, counting)).Throws<ArgumentNullException>();
        await Assert.That(() => new OpenContentRun(open, (CountingRun)null!)).Throws<ArgumentNullException>();
        await Assert.That(() => new OpenContentRun(null!, all)).Throws<ArgumentNullException>();
        await Assert.That(() => new OpenContentRun(open, (AllGroupRun)null!)).Throws<ArgumentNullException>();
    }
}
