namespace NetEvolve.Schemas.Xsd.Tests.Unit.Api;

using System;
using System.Collections.Generic;
using System.Reflection;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xsd.Diagnostics;

public class RuleCodeTests
{
    private static readonly Dictionary<XsdRuleKind, (int First, int Last)> Blocks = new()
    {
        [XsdRuleKind.Engine] = (4000, 4019),
        [XsdRuleKind.Strict10Rejection] = (4020, 4059),
        [XsdRuleKind.NotYetSupported] = (4060, 4099),
        [XsdRuleKind.SchemaRepresentation] = (4100, 4299),
        [XsdRuleKind.SchemaComponent] = (4300, 4599),
        [XsdRuleKind.Validation] = (4600, 4899),
    };

    private static List<string> CodeFields()
    {
        var lines = new List<string>();
        foreach (var field in typeof(XsdRuleCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            var code = (DiagnosticCode)field.GetValue(null)!;
            lines.Add($"{code.Number} {field.Name}");
        }

        return lines;
    }

    [Test]
    public async Task Codes_AreUniqueAndInTheXsdRange()
    {
        var seen = new HashSet<int>();
        var ids = new HashSet<string>(StringComparer.Ordinal);

        foreach (var rule in XsdRuleCatalog.All)
        {
            await Assert.That(DiagnosticRanges.Xsd.Contains(rule.Code)).IsTrue();
            await Assert.That(seen.Add(rule.Code.Number)).IsTrue();
            await Assert.That(ids.Add(rule.Id)).IsTrue();
        }
    }

    [Test]
    public async Task Codes_BlocksMatchTheScheme()
    {
        foreach (var rule in XsdRuleCatalog.All)
        {
            var (first, last) = Blocks[rule.Kind];

            await Assert.That(rule.Code.Number).IsGreaterThanOrEqualTo(first);
            await Assert.That(rule.Code.Number).IsLessThanOrEqualTo(last);
        }
    }

    [Test]
    public async Task Codes_CatalogIsOrderedByCode()
    {
        var previous = 0;

        foreach (var rule in XsdRuleCatalog.All)
        {
            await Assert.That(rule.Code.Number).IsGreaterThan(previous);
            previous = rule.Code.Number;
        }
    }

    [Test]
    public async Task Codes_AreStableSnapshot_NeverRenumbered()
    {
        var fields = CodeFields();
        var actual = new List<string>();
        foreach (var rule in XsdRuleCatalog.All)
        {
            var field = fields.Find(line => line.StartsWith(rule.Code.Number + " ", StringComparison.Ordinal))!;
            actual.Add($"{field} {rule.Id}");
        }

        await Assert.That(string.Join("\n", actual)).IsEqualTo(string.Join("\n", RecordedRuleCodes.All));
    }

    [Test]
    public async Task Codes_EveryCodeConstant_BelongsToExactlyOneRule()
    {
        await Assert.That(CodeFields()).Count().IsEqualTo(XsdRuleCatalog.All.Count);
        await Assert.That(RecordedRuleCodes.All).Count().IsEqualTo(XsdRuleCatalog.All.Count);
    }

    [Test]
    public async Task Codes_EveryRuleCarriesItsMetadata()
    {
        foreach (var rule in XsdRuleCatalog.All)
        {
            await Assert.That(rule.Title).IsNotEmpty();
            await Assert.That(rule.Versions).IsNotEqualTo(XsdVersionMask.None);
            await Assert.That(rule.DefaultSeverity).IsEqualTo(IssueSeverity.Error);
            await Assert.That(rule.ToString()).IsEqualTo($"{rule.Code} {rule.Id}");
        }
    }

    [Test]
    public async Task Codes_SpecificationRules_NameTheirSectionAndVersions()
    {
        var elt = Find("cvc-elt");
        var particleRestrict = Find("cos-particle-restrict");
        var assertion = Find("cvc-assertion");

        await Assert.That(elt.Kind).IsEqualTo(XsdRuleKind.Validation);
        await Assert.That(elt.Versions).IsEqualTo(XsdVersionMask.All);
        await Assert.That(elt.Section).IsEqualTo("Structures 1.0, 3.3.4");
        await Assert.That(particleRestrict.Versions).IsEqualTo(XsdVersionMask.V10);
        await Assert.That(assertion.Versions).IsEqualTo(XsdVersionMask.V11);
        await Assert.That(assertion.Section).IsEqualTo("Structures 1.1, 3.13.4.1");
    }

    [Test]
    public async Task Codes_ConstructRules_PairTheStrictAndTheNotYetSupportedCode()
    {
        var strict = Find("strict10-assert");
        var notYet = Find("not-yet-supported-assert");

        await Assert.That(strict.Code).IsEqualTo(XsdRuleCodes.Strict10Assert);
        await Assert.That(strict.Versions).IsEqualTo(XsdVersionMask.V10);
        await Assert.That(notYet.Code).IsEqualTo(XsdRuleCodes.NotYetSupportedAssert);
        await Assert.That(notYet.Versions).IsEqualTo(XsdVersionMask.V11);
        await Assert.That(notYet.Code.Number - strict.Code.Number).IsEqualTo(40);
    }

    [Test]
    public async Task Reporter_UsesTheRuleCodeSeverityAndClause()
    {
        var collector = new ValidationIssueCollector(ValidationLimits.Default);
        var reporter = new XsdIssueReporter(collector);
        var rule = Find("cvc-complex-type");
        var location = new DocumentLocation("file:///a.xml", 4, 2);

        reporter.Report(rule, location, "wrong content", "cvc-complex-type.2.4");
        reporter.Report(rule, location, "other");
        var issues = collector.ToResult().Issues;

        await Assert.That(issues).Count().IsEqualTo(2);
        await Assert.That(issues[0].Code).IsEqualTo(rule.Code);
        await Assert.That(issues[0].Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(issues[0].Constraint).IsEqualTo("cvc-complex-type.2.4");
        await Assert.That(issues[0].Location).IsEqualTo(location);
        await Assert.That(issues[1].Constraint).IsEqualTo("cvc-complex-type");
    }

    private static XsdRule Find(string id)
    {
        foreach (var rule in XsdRuleCatalog.All)
        {
            if (string.Equals(rule.Id, id, StringComparison.Ordinal))
            {
                return rule;
            }
        }

        throw new InvalidOperationException($"No rule with id {id}.");
    }
}
