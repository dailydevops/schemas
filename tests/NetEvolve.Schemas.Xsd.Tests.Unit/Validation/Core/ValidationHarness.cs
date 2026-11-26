namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Reader.Pipeline;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Validation.Core;

/// <summary>Runs an instance through the real reader into the streaming validator and collects the issues.</summary>
internal static class ValidationHarness
{
    public const string Source = "file:///i.xml";

    public static IReadOnlyList<ValidationIssue> Run(
        SchemaFixture schema,
        string xml,
        ISimpleValueAssessor? assessor = null,
        params ValidationListener[] listeners
    ) => Run(schema, xml, XmlReaderOptions.Default, assessor, listeners);

    public static IReadOnlyList<ValidationIssue> Run(
        SchemaFixture schema,
        string xml,
        XmlReaderOptions options,
        ISimpleValueAssessor? assessor = null,
        params ValidationListener[] listeners
    )
    {
        var collector = new ValidationIssueCollector(ValidationLimits.Default);
        var reporter = new XsdIssueReporter(collector);
        var validator = new StreamingValidator(
            schema.Model(),
            assessor ?? new ScriptedAssessor(),
            reporter,
            ValidationLimits.Default,
            Source,
            listeners
        );
        using var reader = XmlPipelineReader.Create(xml.AsSpan(), options);
        validator.UsePositions(reader);
        var status = reader.Pump(ref validator);
        if (status != XmlReadStatus.Completed)
        {
            throw new InvalidOperationException("The instance is not well-formed: " + reader.Diagnostic?.Message);
        }

        return collector.ToResult().Issues;
    }

    public static DiagnosticCode Code(string ruleId)
    {
        foreach (var rule in XsdRuleCatalog.All)
        {
            if (string.Equals(rule.Id, ruleId, StringComparison.Ordinal))
            {
                return rule.Code;
            }
        }

        throw new InvalidOperationException("No rule " + ruleId);
    }
}
