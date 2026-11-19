namespace NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Facets.Pattern;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Datatypes.Facets.Pattern;
using NetEvolve.Schemas.Xml.Datatypes.Strings;
using NetEvolve.Schemas.Xml.Regex.Contracts;

public class PatternFacetTests
{
    private const int Warmups = 50;

    [Test]
    [Arguments("123", true)]
    [Arguments("abc", true)]
    [Arguments("1a", false)]
    [Arguments("", false)]
    public async Task TwoPatternsInOneStep_AreAlternatives(string lexical, bool expected)
    {
        var facet = Create("[0-9]+", "[a-z]+");

        await Assert.That(facet.IsSatisfiedBy(lexical.AsSpan())).IsEqualTo(expected);
    }

    [Test]
    public async Task Facet_ExposesKindAndPatterns()
    {
        var facet = Create("[0-9]+", "[a-z]+");

        await Assert.That(facet.Kind).IsEqualTo(FacetKind.Pattern);
        await Assert.That(facet.Patterns.Count).IsEqualTo(2);
        await Assert.That(facet.Patterns[0]).IsEqualTo("[0-9]+");
        await Assert.That(facet.Patterns[1]).IsEqualTo("[a-z]+");
    }

    [Test]
    public async Task Pattern_MatchesTheWholeLexicalForm() =>
        await Assert.That(Create("[a-z]+").IsSatisfiedBy("abc1".AsSpan())).IsFalse();

    [Test]
    public async Task PatternsOfEveryDerivationStep_AllApply()
    {
        var token = (AtomicDescriptor<XsdString>)BuiltInDatatypes.For(XsdVersion.V11).Get("token");
        var baseStep = new AtomicDescriptor<XsdString>("threeChars", token, null, [Create("[0-9a-c]{3}")]);
        var derivedStep = new AtomicDescriptor<XsdString>("letters", baseStep, null, [Create("[a-c]+")]);

        await Assert.That(derivedStep.Validate("abc".AsSpan()).IsValid).IsTrue();

        // Matches only the derived step's pattern.
        var onlyDerived = derivedStep.Validate("ab".AsSpan());
        await Assert.That(onlyDerived.IsValid).IsFalse();
        await Assert.That(onlyDerived.FailedFacet).IsEqualTo(FacetKind.Pattern);

        // Matches only the base step's pattern.
        var onlyBase = derivedStep.Validate("123".AsSpan());
        await Assert.That(onlyBase.IsValid).IsFalse();
        await Assert.That(onlyBase.FailedFacet).IsEqualTo(FacetKind.Pattern);
    }

    [Test]
    public async Task Pattern_IsAppliedToTheWhitespaceNormalizedLexicalForm()
    {
        var builtIns = BuiltInDatatypes.For(XsdVersion.V11);
        var facets = new IFacet[] { Create("a b") };
        var token = new AtomicDescriptor<XsdString>(
            "t",
            (AtomicDescriptor<XsdString>)builtIns.Get("token"),
            null,
            facets
        );
        var preserved = new AtomicDescriptor<XsdString>(
            "p",
            (AtomicDescriptor<XsdString>)builtIns.Get("string"),
            null,
            facets
        );

        // Collapse turns the tab, the line feed, and the runs of spaces into one space before the pattern runs.
        await Assert.That(token.Validate("  a \t\n  b  ".AsSpan()).IsValid).IsTrue();
        await Assert.That(preserved.Validate("  a \t\n  b  ".AsSpan()).IsValid).IsFalse();
        await Assert.That(preserved.Validate("a b".AsSpan()).IsValid).IsTrue();
    }

    [Test]
    public async Task PathologicalPattern_ReturnsTheStepLimitDiagnostic_NotAnExceptionOrATimeout()
    {
        var limits = RegexStepLimits.Default.WithMaxNfaSteps(5_000);
        var facet = Create(limits, "(a*)*b");
        var input = new string('a', 10_000);

        var satisfied = facet.Match(input.AsSpan(), out var diagnostic);

        await Assert.That(satisfied).IsFalse();
        await Assert.That(diagnostic.Code).IsEqualTo(PatternDiagnosticCode.StepLimitExceeded);
        await Assert.That(diagnostic.Pattern).IsEqualTo("(a*)*b");
        await Assert.That(diagnostic.PatternIndex).IsEqualTo(0);
        await Assert.That(facet.IsSatisfiedBy(input.AsSpan())).IsFalse();
    }

    [Test]
    public async Task StepLimit_OnADescriptor_IsAFailedPatternFacet()
    {
        var facet = Create(RegexStepLimits.Default.WithMaxNfaSteps(5_000), "(a*)*b");
        var restricted = new AtomicDescriptor<XsdString>(
            "t",
            (AtomicDescriptor<XsdString>)BuiltInDatatypes.For(XsdVersion.V11).Get("token"),
            null,
            [facet]
        );

        var result = restricted.Validate(new string('a', 10_000).AsSpan());

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.FailedFacet).IsEqualTo(FacetKind.Pattern);
    }

    [Test]
    public async Task StepLimit_OfOnePattern_DoesNotHideAMatchOfAnother()
    {
        var limits = RegexStepLimits.Default.WithMaxNfaSteps(5_000);
        var input = new string('a', 1_000);

        // On 1,000 characters "a+" takes about 4,000 steps and "(a*)*b" about 6,000: the second pattern matches within the
        // ceiling, and the order of the patterns does not change the answer.
        var abortFirst = Create(limits, "(a*)*b", "a+");
        var abortLast = Create(limits, "a+", "(a*)*b");

        await Assert.That(abortFirst.Match(input.AsSpan(), out var first)).IsTrue();
        await Assert.That(first.Code).IsEqualTo(PatternDiagnosticCode.None);
        await Assert.That(abortLast.Match(input.AsSpan(), out var last)).IsTrue();
        await Assert.That(last.Code).IsEqualTo(PatternDiagnosticCode.None);
    }

    [Test]
    public async Task StepLimit_NamesTheFirstPatternThatReachedTheCeiling()
    {
        var limits = RegexStepLimits.Default.WithMaxNfaSteps(5_000);
        var facet = Create(limits, "b+", "(a*)*b");

        var satisfied = facet.Match(new string('a', 10_000).AsSpan(), out var diagnostic);

        await Assert.That(satisfied).IsFalse();
        await Assert.That(diagnostic.Code).IsEqualTo(PatternDiagnosticCode.StepLimitExceeded);
        await Assert.That(diagnostic.Pattern).IsEqualTo("(a*)*b");
        await Assert.That(diagnostic.PatternIndex).IsEqualTo(1);
    }

    [Test]
    public async Task Mismatch_BelowTheCeiling_HasNoDiagnostic()
    {
        var facet = Create("[0-9]+");

        var satisfied = facet.Match("abc".AsSpan(), out var diagnostic);

        await Assert.That(satisfied).IsFalse();
        await Assert.That(diagnostic.Code).IsEqualTo(PatternDiagnosticCode.None);
    }

    [Test]
    [Arguments("a)", RegexError.Unbalanced, 1)]
    [Arguments("a|*", RegexError.InvalidQuantifier, 2)]
    public async Task InvalidPattern_IsACodedDiagnosticWithThePositionAndThePatternText(
        string pattern,
        RegexError error,
        int position
    )
    {
        var created = PatternFacet.TryCreate(
            ["[0-9]+", pattern],
            XsdVersion.V11,
            null,
            out var facet,
            out var diagnostic
        );

        await Assert.That(created).IsFalse();
        await Assert.That(facet).IsNull();
        await Assert.That(diagnostic.Code).IsEqualTo(PatternDiagnosticCode.InvalidPattern);
        await Assert.That(diagnostic.Pattern).IsEqualTo(pattern);
        await Assert.That(diagnostic.PatternIndex).IsEqualTo(1);
        await Assert.That(diagnostic.RegexError).IsEqualTo(error);
        await Assert.That(diagnostic.Position).IsEqualTo(position);
    }

    [Test]
    public async Task PatternBeyondTheProgramSizeCap_IsACodedDiagnostic()
    {
        var created = PatternFacet.TryCreate(["a{1,1000000}"], XsdVersion.V11, null, out _, out var diagnostic);

        await Assert.That(created).IsFalse();
        await Assert.That(diagnostic.Code).IsEqualTo(PatternDiagnosticCode.InvalidPattern);
        await Assert.That(diagnostic.RegexError).IsEqualTo(RegexError.LimitExceeded);
        await Assert.That(diagnostic.Pattern).IsEqualTo("a{1,1000000}");
    }

    [Test]
    public async Task ValidPatterns_GiveNoDiagnostic()
    {
        var created = PatternFacet.TryCreate(["[0-9]+", ""], XsdVersion.V10, null, out var facet, out var diagnostic);

        await Assert.That(created).IsTrue();
        await Assert.That(facet).IsNotNull();
        await Assert.That(diagnostic.Code).IsEqualTo(PatternDiagnosticCode.None);
        await Assert.That(facet!.IsSatisfiedBy(ReadOnlySpan<char>.Empty)).IsTrue();
    }

    [Test]
    [Arguments(XsdVersion.V10, false)]
    [Arguments(XsdVersion.V11, true)]
    public async Task NameEscapes_FollowTheNameEditionOfTheVersion(XsdVersion version, bool expected)
    {
        // U+0132 is a name start character in the Fifth Edition only.
        var created = PatternFacet.TryCreate([@"\i"], version, null, out var facet, out _);

        await Assert.That(created).IsTrue();
        await Assert.That(facet!.IsSatisfiedBy("Ĳ".AsSpan())).IsEqualTo(expected);
    }

    [Test]
    public async Task TryCreate_WithoutPatterns_IsRejected() =>
        await Assert
            .That(() => PatternFacet.TryCreate([], XsdVersion.V11, null, out _, out _))
            .Throws<ArgumentOutOfRangeException>();

    [Test]
    public async Task TryCreate_WithNullPatterns_IsRejected() =>
        await Assert
            .That(() => PatternFacet.TryCreate(null!, XsdVersion.V11, null, out _, out _))
            .Throws<ArgumentNullException>();

    [Test]
    public async Task TryCreate_WithANullPattern_IsRejected() =>
        await Assert
            .That(() => PatternFacet.TryCreate(["a", null!], XsdVersion.V11, null, out _, out _))
            .Throws<ArgumentNullException>();

    [Test]
    [Arguments("abc123", "[a-z]+[0-9]+")]
    [Arguments("DE89370400440532013000", "[A-Z]{2}[0-9]{2}[A-Z0-9]{1,30}")]
    [Arguments("x", "[a-z]|[0-9]")]
    public async Task Validate_OnACompiledPatternFacet_AllocatesNothing(string lexical, string pattern)
    {
        var facet = Create(pattern, "[0-9]+");
        var satisfied = facet.IsSatisfiedBy(lexical.AsSpan());

        AllocationAssert.NoAllocation(() => _ = facet.IsSatisfiedBy(lexical.AsSpan()), warmups: Warmups);
        AllocationAssert.NoAllocation(() => _ = facet.Match(lexical.AsSpan(), out _), warmups: Warmups);

        await Assert.That(satisfied).IsTrue();
    }

    [Test]
    public async Task StepLimit_AllocatesNothing()
    {
        var facet = Create(RegexStepLimits.Default.WithMaxNfaSteps(1_000), "(a*)*b");
        var input = new string('a', 500);

        AllocationAssert.NoAllocation(() => _ = facet.Match(input.AsSpan(), out _), warmups: Warmups);

        await Assert.That(facet.Match(input.AsSpan(), out var diagnostic)).IsFalse();
        await Assert.That(diagnostic.Code).IsEqualTo(PatternDiagnosticCode.StepLimitExceeded);
    }

    private static PatternFacet Create(params string[] patterns) => Create(null, patterns);

    private static PatternFacet Create(RegexStepLimits? limits, params string[] patterns)
    {
        var created = PatternFacet.TryCreate(patterns, XsdVersion.V11, limits, out var facet, out _);
        if (!created)
        {
            throw new InvalidOperationException("The test pattern is invalid.");
        }

        return facet!;
    }
}
