namespace NetEvolve.Schemas.Schematron.Tests.Unit.Pipeline;

using System;
using System.Linq;
using System.Threading.Tasks;
using NetEvolve.Schemas;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Pipeline.PipelineFixture;

/// <summary>
/// The public switch <c>WithDtdProcessing</c> on the real engines: a rule set with an internal DTD subset of literal entities is
/// rejected by default, loads when the switch is on, and keeps the entity expansion limits.
/// </summary>
public class DtdProcessingBuildTests
{
    private const string Doctype =
        "<!DOCTYPE schema [ <!ENTITY version \"2.1\"> <!ENTITY big \"&version;&version;&version;\"> ]>";

    private const string Attributes = " queryBinding=\"xslt2\" schematronEdition=\"2025\"";

    private static string Source(string message = "Version &version; failed.") =>
        Doctype
        + Wrap(
            "<title>Rules &version;</title><pattern><rule context=\"a\"><assert test=\"false()\" see=\"urn:v&version;\">"
                + message
                + "</assert></rule></pattern>",
            Attributes
        );

    [Test]
    public async Task Off_InternalSubset_IsRejectedWithNes1201()
    {
        var failure = BuildFailure(Source());

        await Assert
            .That(failure.Issues.Any(i => i.Code.ToString().Contains("1201", StringComparison.Ordinal)))
            .IsTrue();
    }

    [Test]
    public async Task On_InternalSubset_ExpandsEntitiesInTheRules()
    {
        var validator = Builder(Source()).WithDtdProcessing(true).BuildNow();
        var result = Validate(validator, "<a/>");

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Issues.Single().Message).Contains("Version 2.1 failed.");
    }

    [Test]
    public async Task On_EntityExpansionLimit_StaysEnforced()
    {
        var limits = ValidationLimits.Default.WithMaxEntityExpansion(2);
        var failure = Capture(() => _ = Builder(Source("&big;")).WithDtdProcessing(true).WithLimits(limits).Build());

        await Assert.That(failure.Issues).IsNotEmpty();
    }

    [Test]
    public async Task On_LastCallWins()
    {
        var failure = Capture(() => _ = Builder(Source()).WithDtdProcessing(true).WithDtdProcessing(false).Build());

        await Assert.That(failure.Issues).IsNotEmpty();
    }
}
