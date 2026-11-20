namespace NetEvolve.Schemas.Xsd.Tests.Unit.OpenContent;

using System;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.OpenContent;
using NetEvolve.Schemas.Xsd.Wildcards.Core;

public class OpenContentElementTests
{
    private static readonly DocumentLocation Where = new("file:///a.xsd", 4, 9);

    private readonly ValidationIssueCollector _collector = new(ValidationLimits.Default);

    private XsdIssueReporter Reporter => new(_collector);

    private static Wildcard Any() => Wildcard.Create(NamespaceConstraint.Any, XsdProcessContents.Lax, Where);

    [Test]
    [Arguments(1)]
    [Arguments(2)]
    public async Task Create_WithAModeOtherThanNoneAndAWildcard_IsValid(int modeValue)
    {
        var mode = (OpenContentMode)modeValue;
        var wildcard = Any();

        var element = OpenContentElement.Create(mode, wildcard, Where, Reporter);

        await Assert.That(element).IsNotNull();
        await Assert.That(element!.Mode).IsEqualTo(mode);
        await Assert.That(element.Wildcard).IsSameReferenceAs(wildcard);
        await Assert.That(element.AppliesToEmpty).IsFalse();
        await Assert.That(element.Location).IsEqualTo(Where);
        await Assert.That(_collector.ToResult().Issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Create_WithModeNoneAndNoWildcard_IsValid()
    {
        var element = OpenContentElement.Create(OpenContentMode.None, null, Where, Reporter);

        await Assert.That(element).IsNotNull();
        await Assert.That(element!.Mode).IsEqualTo(OpenContentMode.None);
        await Assert.That(element.Wildcard).IsNull();
        await Assert.That(_collector.ToResult().Issues.Count).IsEqualTo(0);
    }

    [Test]
    [Arguments(1)]
    [Arguments(2)]
    public async Task Create_WithAModeOtherThanNoneAndNoWildcard_IsASchemaError(int modeValue)
    {
        var mode = (OpenContentMode)modeValue;
        var element = OpenContentElement.Create(mode, null, Where, Reporter);

        await Assert.That(element).IsNull();
        var issues = _collector.ToResult().Issues;
        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Constraint).IsEqualTo("src-ct.3");
        await Assert.That(issues[0].Code).IsEqualTo(XsdRuleCodes.SrcCt);
        await Assert.That(issues[0].Location).IsEqualTo(Where);
    }

    [Test]
    public async Task Create_WithModeNoneAndAWildcard_IsASchemaError()
    {
        var element = OpenContentElement.Create(OpenContentMode.None, Any(), Where, Reporter);

        await Assert.That(element).IsNull();
        var issues = _collector.ToResult().Issues;
        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Constraint).IsEqualTo("src-ct.4");
        await Assert.That(issues[0].Code).IsEqualTo(XsdRuleCodes.SrcCt);
    }

    [Test]
    public async Task CreateDefault_CarriesTheModeTheWildcardAndAppliesToEmpty()
    {
        var wildcard = Any();

        var element = OpenContentElement.CreateDefault(OpenContentMode.Suffix, wildcard, true, Where);

        await Assert.That(element.Mode).IsEqualTo(OpenContentMode.Suffix);
        await Assert.That(element.Wildcard).IsSameReferenceAs(wildcard);
        await Assert.That(element.AppliesToEmpty).IsTrue();
        await Assert.That(element.Location).IsEqualTo(Where);
    }

    [Test]
    public async Task Property_CarriesTheModeAndTheWildcard()
    {
        var wildcard = Any();

        var property = OpenContentProperty.Create(OpenContentMode.Interleave, wildcard);

        await Assert.That(property.Mode).IsEqualTo(OpenContentMode.Interleave);
        await Assert.That(property.Wildcard).IsSameReferenceAs(wildcard);
    }

    [Test]
    public async Task Factories_RejectNullArguments()
    {
        await Assert
            .That(() => OpenContentElement.Create(OpenContentMode.None, null, Where, null!))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => OpenContentElement.CreateDefault(OpenContentMode.Interleave, null!, false, Where))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => OpenContentProperty.Create(OpenContentMode.Interleave, null!))
            .Throws<ArgumentNullException>();
    }

    [Test]
    [Arguments(-1)]
    [Arguments(3)]
    public async Task Create_RejectsAModeOutOfRange(int mode) =>
        await Assert
            .That(() => OpenContentElement.Create((OpenContentMode)mode, null, Where, Reporter))
            .Throws<ArgumentOutOfRangeException>();

    [Test]
    [Arguments(0)]
    [Arguments(3)]
    public async Task CreateDefault_AndProperty_RejectNoneAndModesOutOfRange(int mode)
    {
        var wildcard = Any();

        await Assert
            .That(() => OpenContentElement.CreateDefault((OpenContentMode)mode, wildcard, false, Where))
            .Throws<ArgumentOutOfRangeException>();
        await Assert
            .That(() => OpenContentProperty.Create((OpenContentMode)mode, wildcard))
            .Throws<ArgumentOutOfRangeException>();
    }
}
