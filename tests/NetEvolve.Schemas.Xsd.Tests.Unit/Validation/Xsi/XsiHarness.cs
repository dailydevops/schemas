namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Xsi;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Reader.Pipeline;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;
using NetEvolve.Schemas.Xsd.Validation.Core;

/// <summary>Runs an instance through the real reader into the streaming validator, with a derivation check of the test.</summary>
internal static class XsiHarness
{
    public static IReadOnlyList<ValidationIssue> Run(
        XsiFixture schema,
        string xml,
        IDerivationCheck? derivation = null,
        ISimpleValueAssessor? assessor = null,
        params ValidationListener[] listeners
    )
    {
        var collector = new ValidationIssueCollector(ValidationLimits.Default);
        var validator = new StreamingValidator(
            schema.Model(),
            assessor ?? new ScriptedAssessor(),
            new XsdIssueReporter(collector),
            ValidationLimits.Default,
            ValidationHarness.Source,
            listeners,
            derivation
        );
        using var reader = XmlPipelineReader.Create(xml.AsSpan(), XmlReaderOptions.Default);
        var status = reader.Pump(ref validator);
        if (status != XmlReadStatus.Completed)
        {
            throw new InvalidOperationException("The instance is not well-formed: " + reader.Diagnostic?.Message);
        }

        return collector.ToResult().Issues;
    }

    /// <summary>Asserts that the issues are exactly one, of the rule and the clause, at the line.</summary>
    public static async Task AssertIssue(
        IReadOnlyList<ValidationIssue> issues,
        string rule,
        string? clause,
        int line,
        string? messagePart = null
    )
    {
        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(ValidationHarness.Code(rule));
        await Assert.That(issues[0].Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(issues[0].Constraint).IsEqualTo(clause ?? rule);
        await Assert.That(issues[0].Location.Line).IsEqualTo(line);
        await Assert.That(issues[0].Location.Source).IsEqualTo(ValidationHarness.Source);
        if (messagePart is not null)
        {
            await Assert.That(issues[0].Message).Contains(messagePart);
        }
    }
}
