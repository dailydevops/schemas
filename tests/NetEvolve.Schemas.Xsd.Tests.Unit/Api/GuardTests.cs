namespace NetEvolve.Schemas.Xsd.Tests.Unit.Api;

using System;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;

public class GuardTests
{
    private static readonly DiagnosticCode Code = new(4000);

    [Test]
    public async Task ForXsd_WithVersion_UndefinedValue_Throws() =>
        await Assert
            .That(() => SchemaValidator.ForXsd().WithVersion((XsdVersion)2))
            .Throws<ArgumentOutOfRangeException>();

    [Test]
    public async Task ForXsd_WithVersion_NegativeUndefinedValue_Throws() =>
        await Assert
            .That(() => SchemaValidator.ForXsd().WithVersion((XsdVersion)(-1)))
            .Throws<ArgumentOutOfRangeException>();

    [Test]
    public async Task XsdRule_NullId_Throws() =>
        await Assert.That(() => NewRule(id: null!)).Throws<ArgumentNullException>();

    [Test]
    public async Task XsdRule_EmptyId_Throws() => await Assert.That(() => NewRule(id: "")).Throws<ArgumentException>();

    [Test]
    public async Task XsdRule_NullTitle_Throws() =>
        await Assert.That(() => NewRule(title: null!)).Throws<ArgumentNullException>();

    [Test]
    public async Task XsdRule_EmptyTitle_Throws() =>
        await Assert.That(() => NewRule(title: "")).Throws<ArgumentException>();

    [Test]
    public async Task XsdRule_NullSection_Throws() =>
        await Assert.That(() => NewRule(section: null!)).Throws<ArgumentNullException>();

    [Test]
    public async Task XsdRule_EmptySection_IsAllowed() =>
        await Assert.That(NewRule(section: "").Section).IsEqualTo(string.Empty);

    [Test]
    public async Task XsdRule_UndefinedKind_Throws() =>
        await Assert.That(() => NewRule(kind: (XsdRuleKind)99)).Throws<ArgumentOutOfRangeException>();

    [Test]
    public async Task XsdRule_UndefinedSeverity_Throws() =>
        await Assert.That(() => NewRule(severity: (IssueSeverity)99)).Throws<ArgumentOutOfRangeException>();

    [Test]
    public async Task XsdRule_UndefinedVersions_Throws() =>
        await Assert.That(() => NewRule(versions: (XsdVersionMask)4)).Throws<ArgumentOutOfRangeException>();

    [Test]
    public async Task XsdIssueReporter_NullCollector_Throws() =>
        await Assert.That(() => new XsdIssueReporter(null!)).Throws<ArgumentNullException>();

    [Test]
    public async Task XsdIssueReporter_Report_NullRule_Throws()
    {
        var reporter = new XsdIssueReporter(new ValidationIssueCollector(ValidationLimits.Default));

        await Assert.That(() => reporter.Report(null!, default, "m")).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task XsdIssueReporter_Report_NullMessage_Throws()
    {
        var reporter = new XsdIssueReporter(new ValidationIssueCollector(ValidationLimits.Default));
        var rule = XsdRuleCatalog.All[0];

        await Assert.That(() => reporter.Report(rule, default, null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task DerivationResult_Invalid_NullRule_Throws() =>
        await Assert.That(() => DerivationResult.Invalid(null!)).Throws<ArgumentNullException>();

    private static XsdRule NewRule(
        string id = "id",
        string title = "title",
        string section = "s",
        XsdRuleKind kind = XsdRuleKind.Engine,
        XsdVersionMask versions = XsdVersionMask.V10,
        IssueSeverity severity = IssueSeverity.Error
    ) => new(Code, id, title, kind, versions, section, severity);
}
