namespace NetEvolve.Schemas.Xsd.Tests.Unit.IdentityConstraints.Evaluation;

using System;
using System.Text;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Reader.Pipeline;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.IdentityConstraints.Evaluation;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;
using NetEvolve.Schemas.Xsd.Validation.Core;

public class EvaluationMemoryTests
{
    private const long GrowthBound = 32 * 1024;

    private static string Document(int count, Func<int, string> attributes)
    {
        var builder = new StringBuilder((count * 20) + 16);
        _ = builder.Append("<r>");
        for (var i = 0; i < count; i++)
        {
            _ = builder.Append("<e ").Append(attributes(i)).Append("/>");
        }

        return builder.Append("</r>").ToString();
    }

    private static long Measure(SchemaFixture schema, string xml, ConstraintSet set, out int issues)
    {
        var count = -1;
        var allocated = AllocationAssert.Measure(() =>
        {
            var collector = new ValidationIssueCollector(ValidationLimits.Default);
            var validator = new StreamingValidator(
                schema.Model(),
                new ScriptedAssessor(),
                new XsdIssueReporter(collector),
                ValidationLimits.Default,
                ValidationHarness.Source,
                [set.Listener()]
            );
            using var reader = XmlPipelineReader.Create(xml.AsSpan());
            var status = reader.Pump(ref validator);
            count = status == XmlReadStatus.Completed ? collector.ToResult().Issues.Count : -1;
        });
        issues = count;
        return allocated;
    }

    [Test]
    public async Task Elements_NoSelectorReaches_AllocateNoGrowthWithTheElementCount()
    {
        // The selector s/e reaches no element of this document, so nothing is stored per element.
        var set = new ConstraintSet().On("r", IdcFixture.Key("k", "s/e", "@a"));
        var schema = IdcFixture.Schema();
        var small = Document(1_000, static _ => "a=\"1\"");
        var large = Document(100_000, static _ => "a=\"1\"");

        var smallBytes = Measure(schema, small, set, out var smallIssues);
        var largeBytes = Measure(schema, large, set, out var largeIssues);

        await Assert.That(smallIssues).IsEqualTo(0);
        await Assert.That(largeIssues).IsEqualTo(0);
        await Assert.That(largeBytes - smallBytes).IsLessThan(GrowthBound);
    }

    [Test]
    public async Task Elements_OfADeclarationWithoutConstraints_AllocateNoGrowthWithTheElementCount()
    {
        var schema = IdcFixture.Schema();
        var small = Document(1_000, static i => "a=\"" + i + "\"");
        var large = Document(100_000, static i => "a=\"" + i + "\"");

        var smallBytes = Measure(schema, small, new ConstraintSet(), out _);
        var largeBytes = Measure(schema, large, new ConstraintSet(), out _);

        await Assert.That(largeBytes - smallBytes).IsLessThan(GrowthBound);
    }

    // Idc_KeyTableGrowth_IsProportionalToMatches
    [Test]
    public async Task KeyTable_GrowsInProportionToTheMatches_AndNoFasterThanThat()
    {
        var set = new ConstraintSet().On("r", IdcFixture.Key("k", "e", "@a"));
        var schema = IdcFixture.Schema();
        var small = Document(1_000, static i => "a=\"" + i + "\"");
        var large = Document(10_000, static i => "a=\"" + i + "\"");

        var smallBytes = Measure(schema, small, set, out var smallIssues);
        var largeBytes = Measure(schema, large, set, out var largeIssues);

        await Assert.That(smallIssues).IsEqualTo(0);
        await Assert.That(largeIssues).IsEqualTo(0);
        // Nine thousand more matches cost at most 640 bytes each (about 420 are measured), and more than nothing.
        await Assert.That(largeBytes - smallBytes).IsGreaterThan(0);
        await Assert.That(largeBytes - smallBytes).IsLessThan(9_000L * 640);
    }
}
