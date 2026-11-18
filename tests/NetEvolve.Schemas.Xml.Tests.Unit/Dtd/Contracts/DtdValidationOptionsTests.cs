namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Contracts;

using System;
using System.Threading;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Contracts.Fakes;

public class DtdValidationOptionsTests
{
    private static ResourceRequest Request(ResourceKind kind) =>
        new(kind, "ext.dtd", "http://example.org/doc.xml", "http://example.org/ext.dtd");

    [Test]
    public async Task Options_Default_HasDenyAllResolverNoExternalAccessAndWarnForNonDeterminism()
    {
        var options = DtdValidationOptions.Default;

        await Assert.That(options.Resolver).IsSameReferenceAs(DenyAllResourceResolver.Instance);
        await Assert.That(options.AllowExternalSubset).IsFalse();
        await Assert.That(options.AllowExternalGeneralEntities).IsFalse();
        await Assert.That(options.AllowExternalParameterEntities).IsFalse();
        await Assert.That(options.NonDeterministicContentModels).IsEqualTo(DtdNonDeterminism.Warn);
        await Assert.That(options.Limits).IsSameReferenceAs(ValidationLimits.Default);
        await Assert.That(options.DtdLimits).IsSameReferenceAs(DtdValidationLimits.Default);
    }

    [Test]
    public async Task Options_NonDeterministicDefault_IsWarn()
    {
        await Assert.That(DtdValidationOptions.Default.NonDeterministicContentModels).IsEqualTo(DtdNonDeterminism.Warn);

        var modes = Enum.GetValues<DtdNonDeterminism>();
        await Assert
            .That(modes)
            .IsEquivalentTo([DtdNonDeterminism.Accept, DtdNonDeterminism.Warn, DtdNonDeterminism.Reject]);
        await Assert.That(modes[0]).IsEqualTo(default(DtdNonDeterminism));
    }

    [Test]
    [Arguments(ResourceKind.ExternalSubset)]
    [Arguments(ResourceKind.ExternalEntity)]
    [Arguments(ResourceKind.ExternalParameterEntity)]
    public async Task Options_Default_DeniesEveryRequest_FakeResolverReceivesNoCall(
        ResourceKind kind,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var fake = new FakeDtdResolver();
        var withFakeResolver = DtdValidationOptions.Default.WithResolver(fake);

        using var result = withFakeResolver.Resolve(Request(kind), cancellationToken);

        await Assert.That(result.Status).IsEqualTo(ResourceStatus.Denied);
        await Assert.That(fake.Requests).IsEmpty();
    }

    [Test]
    public async Task Options_ResolverWithoutFlag_DeniesExternalSubset(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var fake = new FakeDtdResolver();
        var options = DtdValidationOptions.Default.WithResolver(fake).WithAllowExternalGeneralEntities(true);

        using var result = options.Resolve(Request(ResourceKind.ExternalSubset), cancellationToken);

        await Assert.That(result.Status).IsEqualTo(ResourceStatus.Denied);
        await Assert.That(fake.Requests).IsEmpty();
    }

    [Test]
    public async Task Options_FlagWithoutResolver_IsDeniedByDefaultResolver_Xml2002(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var options = DtdValidationOptions.Default.WithAllowExternalSubset(true);

        using var result = options.Resolve(Request(ResourceKind.ExternalSubset), cancellationToken);

        await Assert.That(result.Status).IsEqualTo(ResourceStatus.Denied);
        await Assert.That(result.Message).Contains(XmlErrorCodes.XML2002.Alias);
    }

    [Test]
    [Arguments(ResourceKind.ExternalSubset)]
    [Arguments(ResourceKind.ExternalEntity)]
    [Arguments(ResourceKind.ExternalParameterEntity)]
    public async Task Options_DeniedRequest_IsFatalXml2002_WithUriBaseAndKind(
        ResourceKind kind,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var byFlag = DtdValidationOptions.Default.Resolve(Request(kind), cancellationToken);
        var byResolver = DtdValidationOptions
            .Default.WithResolver(new FakeDtdResolver(content: null))
            .WithAllowExternalSubset(true)
            .WithAllowExternalGeneralEntities(true)
            .WithAllowExternalParameterEntities(true)
            .Resolve(Request(kind), cancellationToken);

        foreach (var result in new[] { byFlag, byResolver })
        {
            await Assert.That(result.Status).IsEqualTo(ResourceStatus.Denied);
            await Assert.That(result.Message).Contains("XML2002");
            await Assert.That(result.Message).Contains(kind.ToString());
            await Assert.That(result.Message).Contains("http://example.org/ext.dtd");
            await Assert.That(result.Message).Contains("http://example.org/doc.xml");
        }
    }

    [Test]
    public async Task Options_EachFlag_EnablesOnlyItsKind(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var kinds = new[]
        {
            ResourceKind.ExternalSubset,
            ResourceKind.ExternalEntity,
            ResourceKind.ExternalParameterEntity,
        };
        var flags = new Func<DtdValidationOptions, DtdValidationOptions>[]
        {
            static o => o.WithAllowExternalSubset(true),
            static o => o.WithAllowExternalGeneralEntities(true),
            static o => o.WithAllowExternalParameterEntities(true),
        };

        for (var flag = 0; flag < flags.Length; flag++)
        {
            var fake = new FakeDtdResolver();
            var options = flags[flag](DtdValidationOptions.Default.WithResolver(fake));

            for (var kind = 0; kind < kinds.Length; kind++)
            {
                using var result = options.Resolve(Request(kinds[kind]), cancellationToken);

                await Assert.That(result.Status).IsEqualTo(kind == flag ? ResourceStatus.Found : ResourceStatus.Denied);
            }

            await Assert.That(fake.Requests).IsEquivalentTo([kinds[flag]]);
        }
    }

    [Test]
    public async Task Options_FoundResult_IsPassedThroughWithItsContent(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var options = DtdValidationOptions
            .Default.WithResolver(new FakeDtdResolver("<!ELEMENT b ANY>"))
            .WithAllowExternalSubset(true);

        using var result = options.Resolve(Request(ResourceKind.ExternalSubset), cancellationToken);

        await Assert.That(result.Status).IsEqualTo(ResourceStatus.Found);
        await Assert.That(result.SystemId).IsEqualTo("http://example.org/ext.dtd");
        await Assert.That(result.Content.Length).IsEqualTo("<!ELEMENT b ANY>".Length);
    }

    [Test]
    [Arguments(ResourceKind.SchemaDocument)]
    [Arguments(ResourceKind.XPathText)]
    public async Task Options_Resolve_RejectsKindsThatAreNotDtdResources(
        ResourceKind kind,
        CancellationToken cancellationToken
    ) =>
        await Assert
            .That(() => DtdValidationOptions.Default.Resolve(Request(kind), cancellationToken))
            .Throws<ArgumentOutOfRangeException>();

    [Test]
    public async Task Options_Limits_AreSnapshotted()
    {
        var defaults = DtdValidationLimits.Default;
        var limits = defaults.WithMaxExternalEntityRequests(3).WithMaxContentModelStates(4).WithMaxDeclarations(5);
        var coreLimits = ValidationLimits.Default.WithMaxIssues(7);

        var options = DtdValidationOptions.Default.WithDtdLimits(limits).WithLimits(coreLimits);
        var changed = options.WithDtdLimits(defaults).WithLimits(ValidationLimits.Default);

        await Assert.That(defaults.MaxExternalEntityRequests).IsEqualTo(32);
        await Assert.That(defaults.MaxContentModelStates).IsEqualTo(65_536);
        await Assert.That(defaults.MaxDeclarations).IsEqualTo(100_000);
        await Assert.That(options.DtdLimits).IsSameReferenceAs(limits);
        await Assert.That(options.Limits).IsSameReferenceAs(coreLimits);
        await Assert.That(limits.MaxExternalEntityRequests).IsEqualTo(3);
        await Assert.That(limits.MaxContentModelStates).IsEqualTo(4);
        await Assert.That(limits.MaxDeclarations).IsEqualTo(5);
        await Assert.That(changed.DtdLimits).IsSameReferenceAs(defaults);
        await Assert.That(options.DtdLimits).IsSameReferenceAs(limits);
    }

    [Test]
    public async Task Options_With_ReturnsACopyAndLeavesTheOriginalUntouched()
    {
        var original = DtdValidationOptions.Default;

        var changed = original
            .WithAllowExternalSubset(true)
            .WithAllowExternalGeneralEntities(true)
            .WithAllowExternalParameterEntities(true)
            .WithNonDeterministicContentModels(DtdNonDeterminism.Reject);

        await Assert.That(changed).IsNotSameReferenceAs(original);
        await Assert.That(changed.AllowExternalSubset).IsTrue();
        await Assert.That(changed.AllowExternalGeneralEntities).IsTrue();
        await Assert.That(changed.AllowExternalParameterEntities).IsTrue();
        await Assert.That(changed.NonDeterministicContentModels).IsEqualTo(DtdNonDeterminism.Reject);
        await Assert.That(original.AllowExternalSubset).IsFalse();
        await Assert.That(original.NonDeterministicContentModels).IsEqualTo(DtdNonDeterminism.Warn);
    }

    [Test]
    public async Task Options_Guards_RejectNullAndOutOfRangeValues()
    {
        var options = DtdValidationOptions.Default;

        await Assert.That(() => options.WithResolver(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => options.WithLimits(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => options.WithDtdLimits(null!)).Throws<ArgumentNullException>();
        await Assert
            .That(() => options.WithNonDeterministicContentModels((DtdNonDeterminism)3))
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task DtdLimits_Guards_RejectZeroAndNegativeValues()
    {
        var limits = DtdValidationLimits.Default;

        await Assert.That(() => limits.WithMaxExternalEntityRequests(0)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => limits.WithMaxContentModelStates(0)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => limits.WithMaxDeclarations(-1)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Options_AreSealedWithoutPublicConstructors()
    {
        foreach (var type in new[] { typeof(DtdValidationOptions), typeof(DtdValidationLimits) })
        {
            await Assert.That(type.IsPublic && type.IsSealed).IsTrue();
            await Assert.That(type.GetConstructors()).IsEmpty();
        }
    }
}
