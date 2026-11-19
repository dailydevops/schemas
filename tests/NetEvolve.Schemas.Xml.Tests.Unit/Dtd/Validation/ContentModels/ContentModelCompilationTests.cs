namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.ContentModels;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using NetEvolve.Schemas;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Model;
using static NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.ContentModels.ContentModelHarness;

/// <summary>Compilation of a content model once: determinism report, bounded size, and the limit of positions.</summary>
public class ContentModelCompilationTests
{
    // Fixed bounds for the large models; the measured values are far below them, the margin absorbs a slow CI machine.
    private static readonly TimeSpan CompileTimeBound = TimeSpan.FromSeconds(5);
    private const long CompileAllocationBound = 128L * 1024 * 1024;

    private static DtdValidationOptions WithMode(DtdNonDeterminism mode) =>
        DtdValidationOptions.Default.WithNonDeterministicContentModels(mode);

    // The positions of (a1?,(a2?,(a3?,...))) with every sequence optional: 1000 nested optional particles.
    private static DtdContentParticle NestedSequences(int count)
    {
        var inner = Group(
            DtdParticleKind.Sequence,
            DtdOccurrence.Optional,
            Name("a" + Number(count), DtdOccurrence.Optional)
        );
        for (var i = count - 1; i >= 1; i--)
        {
            inner = Group(
                DtdParticleKind.Sequence,
                DtdOccurrence.Optional,
                Name("a" + Number(i), DtdOccurrence.Optional),
                inner
            );
        }

        return inner;
    }

    // ((((a?)?)?)...): 1000 nested optional groups around one name.
    private static DtdContentParticle NestedGroups(int count)
    {
        var particle = Name("a1", DtdOccurrence.Optional);
        for (var i = 0; i < count; i++)
        {
            particle = Group(DtdParticleKind.Sequence, DtdOccurrence.Optional, particle);
        }

        return particle;
    }

    private static DtdContentParticle Flat(int count)
    {
        var items = new DtdContentParticle[count];
        for (var i = 0; i < count; i++)
        {
            items[i] = Name("a" + Number(i + 1), DtdOccurrence.Optional);
        }

        return Group(DtdParticleKind.Sequence, DtdOccurrence.Once, items);
    }

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static List<DtdElementDeclaration> Elements(DtdContentParticle model, int leaves)
    {
        var list = new List<DtdElementDeclaration>(leaves + 1) { Children("r", model) };
        for (var i = 1; i <= leaves; i++)
        {
            list.Add(Empty("a" + Number(i)));
        }

        return list;
    }

    [Test]
    [Arguments("((a,b)|(a,c))", "<r><a/><b/></r>")]
    [Arguments("(a?,a)", "<r><a/></r>")]
    [Arguments("((a|b)*,a)", "<r><b/><a/></r>")]
    [Arguments("((a,b?)*,a)", "<r><a/><b/><a/></r>")]
    public async Task ContentModel_NonDeterministic_AbOrAc_DefaultReportsXml5911Warning_AndMatchesSameLanguage(
        string model,
        string validScript
    )
    {
        var elements = WithLeaves(Children("r", model));

        var run = Run(validScript, null, elements);

        await Assert.That(run.Issues.Count).IsEqualTo(1);
        var issue = run.Issues[0];
        await Assert.That(issue.Code).IsEqualTo(DtdValidityCodes.XML5911.Code);
        await Assert.That(issue.Severity).IsEqualTo(IssueSeverity.Warning);
        await Assert.That(issue.Message).Contains("'r'");
        await Assert.That(run.Result.IsValid).IsTrue();
    }

    [Test]
    [Arguments("((a,b)|(a,c))", "<r><a/><b/></r>", true)]
    [Arguments("((a,b)|(a,c))", "<r><a/><c/></r>", true)]
    [Arguments("((a,b)|(a,c))", "<r><a/></r>", false)]
    [Arguments("((a,b)|(a,c))", "<r><a/><a/></r>", false)]
    [Arguments("(a?,a)", "<r><a/></r>", true)]
    [Arguments("(a?,a)", "<r><a/><a/></r>", true)]
    [Arguments("(a?,a)", "<r><a/><a/><a/></r>", false)]
    [Arguments("((a|b)*,a)", "<r><b/><a/><b/><a/></r>", true)]
    [Arguments("((a|b)*,a)", "<r><b/><a/><b/></r>", false)]
    public async Task ContentModel_NonDeterministic_AcceptsTheSameLanguageInEveryMode(
        string model,
        string script,
        bool valid
    )
    {
        var elements = WithLeaves(Children("r", model));

        foreach (var mode in new[] { DtdNonDeterminism.Accept, DtdNonDeterminism.Warn, DtdNonDeterminism.Reject })
        {
            var run = Run(script, WithMode(mode), elements);

            var elementErrors = 0;
            foreach (var issue in run.Issues)
            {
                if (issue.Code == DtdValidityCodes.XML5004.Code)
                {
                    elementErrors++;
                }
            }

            await Assert.That(elementErrors == 0).IsEqualTo(valid);
        }
    }

    [Test]
    public async Task ContentModel_NonDeterministic_RejectMode_ReportsError()
    {
        var run = Run(
            "<r><a/><b/></r>",
            WithMode(DtdNonDeterminism.Reject),
            WithLeaves(Children("r", "((a,b)|(a,c))"))
        );

        await Assert.That(run.Issues.Count).IsEqualTo(1);
        await Assert.That(run.Issues[0].Code).IsEqualTo(DtdValidityCodes.XML5911.Code);
        await Assert.That(run.Issues[0].Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(run.Result.IsValid).IsFalse();
    }

    [Test]
    public async Task ContentModel_NonDeterministic_AcceptMode_ReportsNothing()
    {
        var run = Run(
            "<r><a/><b/></r>",
            WithMode(DtdNonDeterminism.Accept),
            WithLeaves(Children("r", "((a,b)|(a,c))"))
        );

        await Assert.That(run.Issues).IsEmpty();
    }

    [Test]
    [Arguments("(a,(b|c))")]
    [Arguments("(a?,b)")]
    [Arguments("((a|b)*)")]
    [Arguments("((a,b)|(c,d))")]
    [Arguments("(a*,b)")]
    [Arguments("((a,b)+,c?)")]
    public async Task ContentModel_Deterministic_ReportsNothing(string model)
    {
        var run = Run("<r/>", null, WithLeaves(Children("r", model)));

        await Assert.That(run.Issues.Where(i => i.Code == DtdValidityCodes.XML5911.Code)).IsEmpty();
    }

    [Test]
    public async Task ContentModel_NonDeterministic_IsReportedOncePerModel_AtTheDocumentTypeLocation()
    {
        var issues = Begin(
            null,
            [
                Children("r", "((a,b)|(a,c))"),
                Children("s", "(a?,a)"),
                Children("t", "(a,b)"),
                Empty("a"),
                Empty("b"),
                Empty("c"),
            ]
        );

        var result = issues.ToResult();
        await Assert.That(result.Issues.Count).IsEqualTo(2);
        await Assert.That(result.Issues[0].Message).Contains("'r'");
        await Assert.That(result.Issues[1].Message).Contains("'s'");
    }

    [Test]
    public async Task ContentModel_Mixed_DuplicateNames_AreNotReportedAsNonDeterministic()
    {
        // Duplicate names in a mixed list are the constraint No Duplicate Types (XML-67), not a determinism matter.
        var issues = Begin(null, [Mixed("r", "a", "a"), Empty("a")]);

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ContentModel_1000NestedOptionalSequences_CompileWithinATimeAndMemoryBound_AndMatch()
    {
        var elements = Elements(NestedSequences(1000), 1000);
        var watch = Stopwatch.StartNew();

        AllocationAssert.AtMost(CompileAllocationBound, () => _ = Begin(null, elements));

        watch.Stop();
        var run = Run("<r><a3/><a999/></r>", null, elements.ToArray());
        var reject = Run("<r><a999/>~<a3/></r>", null, elements.ToArray());
        await Assert.That(watch.Elapsed).IsLessThan(CompileTimeBound);
        await Assert.That(run.Issues).IsEmpty();
        await Assert.That(reject.Issues.Count).IsEqualTo(1);
        await Assert.That(reject.Issues[0].Location).IsEqualTo(reject.Expected);
        await Assert.That(Begin(null, elements).IsStopped).IsFalse();
    }

    [Test]
    public async Task ContentModel_1000NestedOptionalGroups_CompileWithinATimeAndMemoryBound_AndMatch()
    {
        var elements = Elements(NestedGroups(1000), 1);
        var watch = Stopwatch.StartNew();

        AllocationAssert.AtMost(CompileAllocationBound, () => _ = Begin(null, elements));

        watch.Stop();
        var empty = Run("<r/>", null, elements.ToArray());
        var one = Run("<r><a1/></r>", null, elements.ToArray());
        var reject = Run("<r><a1/>~<a1/></r>", null, elements.ToArray());
        await Assert.That(watch.Elapsed).IsLessThan(CompileTimeBound);
        await Assert.That(empty.Issues).IsEmpty();
        await Assert.That(one.Issues).IsEmpty();
        await Assert.That(reject.Issues.Count).IsEqualTo(1);
        await Assert.That(reject.Issues[0].Location).IsEqualTo(reject.Expected);
    }

    [Test]
    public async Task ContentModel_NestingFarBeyondAnyParserLimit_DoesNotOverflowTheStack()
    {
        // A hand-built view is not bound by the nesting limit of the declaration parser; compilation must not recurse.
        var particle = Name("a1", DtdOccurrence.Once);
        for (var i = 0; i < 200_000; i++)
        {
            particle = Group(DtdParticleKind.Choice, DtdOccurrence.Once, particle);
        }

        var run = Run("<r><a1/></r>", null, Elements(particle, 1).ToArray());

        await Assert.That(run.Issues).IsEmpty();
    }

    [Test]
    public async Task ContentModel_AllOptionalSequenceBeyondTheSizeCeiling_StopsWithXml1118_WithoutAllocatingGigabytes()
    {
        // 5,000 positions are below MaxContentModelStates, but the follow relation of an all-optional sequence is
        // quadratic (12.5 million transitions): the internal size ceiling stops the compilation with the same limit code.
        var elements = Elements(Flat(5000), 0);
        ValidationIssueCollector? issues = null;
        var watch = Stopwatch.StartNew();

        AllocationAssert.AtMost(CompileAllocationBound, () => issues = Begin(null, elements));

        watch.Stop();
        await Assert.That(watch.Elapsed).IsLessThan(CompileTimeBound);
        await Assert.That(issues!.IsStopped).IsTrue();
        var result = issues.ToResult();
        await Assert.That(result.Issues.Count).IsEqualTo(1);
        await Assert.That(result.Issues[0].Code).IsEqualTo(DtdLimitCodes.XML1118.Code);
    }

    [Test]
    public async Task ContentModel_MoreThanMaxContentModelStatesPositions_StopsWithXml1118()
    {
        var options = DtdValidationOptions.Default.WithDtdLimits(
            DtdValidationLimits.Default.WithMaxContentModelStates(3)
        );

        var within = Begin(options, [Children("r", "(a,b,c)")]);
        var beyond = Begin(options, [Children("r", "(a,b,(c|d))")]);

        await Assert.That(within.IsStopped).IsFalse();
        await Assert.That(beyond.IsStopped).IsTrue();
        await Assert.That(beyond.ToResult().Issues[0].Code).IsEqualTo(DtdLimitCodes.XML1118.Code);
    }

    [Test]
    public async Task ContentModel_AfterALimitStop_LaterEventsAreIgnored_AndNoFurtherModelIsCompiled()
    {
        var options = DtdValidationOptions.Default.WithDtdLimits(
            DtdValidationLimits.Default.WithMaxContentModelStates(2)
        );

        var run = Run(
            "<r><a/><b/><b/></r>",
            options,
            Children("r", "(a,b,c)"),
            Children("s", "((a,b)|(a,c))"),
            Empty("a"),
            Empty("b")
        );

        await Assert.That(run.Collector.IsStopped).IsTrue();
        await Assert.That(run.Issues.Count).IsEqualTo(1);
        await Assert.That(run.Issues[0].Code).IsEqualTo(DtdLimitCodes.XML1118.Code);
    }
}
