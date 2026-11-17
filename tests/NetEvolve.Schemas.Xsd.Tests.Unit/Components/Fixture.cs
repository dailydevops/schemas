namespace NetEvolve.Schemas.Xsd.Tests.Unit.Components;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;

internal static class Fixture
{
    public static XPathQName Name(string local) => new("urn:test", local);

    public static XsdReference Ref(XsdSymbolSpace space, string name, int line = 1, bool structural = true) =>
        new(space, Name(name), new DocumentLocation("file:///a.xsd", line, 2), structural);

    public static ComponentRegistry NewRegistry(out ValidationIssueCollector collector)
    {
        collector = new ValidationIssueCollector(ValidationLimits.Default);
        return new ComponentRegistry(new XsdIssueReporter(collector));
    }

    public static IReadOnlyList<ValidationIssue> Resolve(
        ComponentRegistry registry,
        ValidationIssueCollector collector,
        XsdVersion version = XsdVersion.V11
    )
    {
        new ComponentResolver(version).Resolve(registry, new XsdIssueReporter(collector));
        return collector.ToResult().Issues;
    }

    public static DiagnosticCode CodeOf(string ruleId)
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
