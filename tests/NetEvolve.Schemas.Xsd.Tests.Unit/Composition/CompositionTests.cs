namespace NetEvolve.Schemas.Xsd.Tests.Unit.Composition;

using System;
using System.Threading;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xsd.Composition;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.SchemaDocuments;
using static NetEvolve.Schemas.Xsd.Tests.Unit.Composition.CompositionFixture;

/// <summary>Include, import, and redefine go through the resolver, load each document once, and report located errors.</summary>
public class CompositionTests
{
    [Test]
    [Arguments("<xs:include schemaLocation=\"a.xsd\"/>")]
    [Arguments("<xs:redefine schemaLocation=\"a.xsd\"/>")]
    public async Task DefaultResolver_RefusesEveryLocation_WithACodedLocatedError(string directive)
    {
        ArgumentNullException.ThrowIfNull(directive);

        var (set, issues) = Load(Schema("\n  " + directive), DenyAllResourceResolver.Instance);

        await Assert.That(set.Units.Count).IsEqualTo(1);
        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(CoreDiagnosticCodes.ResourceDenied);
        await Assert.That(issues[0].Location.Source).IsEqualTo(RootUri);
        await Assert.That(issues[0].Location.Line).IsEqualTo(3);
        await Assert.That(issues[0].Location.Column).IsEqualTo(3);
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Import_WhoseLocationCannotBeResolved_IsIgnored(bool denied)
    {
        var resolver = denied ? (IResourceResolver)DenyAllResourceResolver.Instance : new MapResolver();

        var (set, issues) = Load(Schema("\n  <xs:import namespace=\"urn:b\" schemaLocation=\"gone.xsd\"/>"), resolver);

        await Assert.That(set.Units.Count).IsEqualTo(1);
        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task DefaultResolver_WithoutBaseUri_StillRefuses()
    {
        var (set, issues) = LoadWithoutBase(
            Schema("<xs:include schemaLocation=\"a.xsd\"/>"),
            DenyAllResourceResolver.Instance
        );

        await Assert.That(set.Units.Count).IsEqualTo(1);
        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(CoreDiagnosticCodes.ResourceDenied);
    }

    [Test]
    public async Task Requests_CarryKindBaseResolvedUriAndImportNamespace()
    {
        var resolver = new MapResolver()
            .Add("file:///sub/a.xsd", Schema(string.Empty))
            .Add("file:///b.xsd", Schema(string.Empty, "urn:b"));

        _ = Load(
            Schema(
                "<xs:include schemaLocation=\"sub/a.xsd\"/><xs:import namespace=\"urn:b\" schemaLocation=\"b.xsd\"/>"
            ),
            resolver
        );

        await Assert.That(resolver.Calls.Count).IsEqualTo(2);
        await Assert.That(resolver.Calls[0].Kind).IsEqualTo(ResourceKind.SchemaDocument);
        await Assert.That(resolver.Calls[0].Reference).IsEqualTo("sub/a.xsd");
        await Assert.That(resolver.Calls[0].BaseUri).IsEqualTo(RootUri);
        await Assert.That(resolver.Calls[0].ResolvedUri).IsEqualTo("file:///sub/a.xsd");
        await Assert.That(resolver.Calls[0].NamespaceUri).IsNull();
        await Assert.That(resolver.Calls[1].NamespaceUri).IsEqualTo("urn:b");
    }

    [Test]
    public async Task IncludeCycle_Terminates_AndLoadsEachDocumentOnce()
    {
        var resolver = new MapResolver()
            .Add("file:///a.xsd", Schema("<xs:include schemaLocation=\"b.xsd\"/><xs:element name=\"a\"/>"))
            .Add(
                "file:///b.xsd",
                Schema(
                    "<xs:include schemaLocation=\"a.xsd\"/><xs:include schemaLocation=\"root.xsd\"/><xs:element name=\"b\"/>"
                )
            );

        var (set, issues) = Load(
            Schema("<xs:include schemaLocation=\"a.xsd\"/><xs:include schemaLocation=\"b.xsd\"/>"),
            resolver
        );

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(set.Units.Count).IsEqualTo(3);
        await Assert.That(string.Join(",", resolver.Requests)).IsEqualTo("file:///a.xsd,file:///b.xsd");
    }

    [Test]
    public async Task ImportCycle_Terminates_AndLoadsEachDocumentOnce()
    {
        var resolver = new MapResolver()
            .Add("file:///a.xsd", Schema("<xs:import namespace=\"urn:b\" schemaLocation=\"b.xsd\"/>", "urn:a"))
            .Add("file:///b.xsd", Schema("<xs:import namespace=\"urn:a\" schemaLocation=\"a.xsd\"/>", "urn:b"));

        var (set, issues) = Load(
            Schema("<xs:import namespace=\"urn:a\" schemaLocation=\"a.xsd\"/>", "urn:root"),
            resolver
        );

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(set.Units.Count).IsEqualTo(3);
        await Assert.That(string.Join(",", resolver.Requests)).IsEqualTo("file:///a.xsd,file:///b.xsd");
    }

    [Test]
    public async Task ImportWithoutLocation_LoadsNothing()
    {
        var resolver = new MapResolver();

        var (set, issues) = Load(Schema("<xs:import namespace=\"urn:b\"/>", "urn:a"), resolver);

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(set.Units.Count).IsEqualTo(1);
        await Assert.That(resolver.Requests.Count).IsEqualTo(0);
    }

    [Test]
    [Arguments("include")]
    [Arguments("redefine")]
    public async Task MissingDocument_IsALocatedSchemaError(string directive)
    {
        ArgumentNullException.ThrowIfNull(directive);

        var (set, issues) = Load(Schema("\n  <xs:" + directive + " schemaLocation=\"gone.xsd\"/>"), new MapResolver());

        await Assert.That(set.Units.Count).IsEqualTo(1);
        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(CoreDiagnosticCodes.ResourceNotFound);
        await Assert.That(issues[0].Location.Source).IsEqualTo(RootUri);
        await Assert.That(issues[0].Location.Line).IsEqualTo(3);
    }

    [Test]
    public async Task Include_WithAnotherTargetNamespace_IsASchemaError()
    {
        var resolver = new MapResolver().Add("file:///a.xsd", Schema(string.Empty, "urn:other"));

        var (set, issues) = Load(Schema("\n  <xs:include schemaLocation=\"a.xsd\"/>", "urn:root"), resolver);

        await Assert.That(set.Units.Count).IsEqualTo(1);
        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(CodeOf("src-include"));
        await Assert.That(issues[0].Location.Line).IsEqualTo(3);
    }

    [Test]
    public async Task Include_OfADocumentWithANamespace_IntoAnAbsentNamespace_IsASchemaError()
    {
        var resolver = new MapResolver().Add("file:///a.xsd", Schema(string.Empty, "urn:other"));

        var (_, issues) = Load(Schema("<xs:include schemaLocation=\"a.xsd\"/>"), resolver);

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(CodeOf("src-include"));
    }

    [Test]
    public async Task Include_WithoutTargetNamespace_IsChameleon_AndTakesTheNamespaceOfTheIncluder()
    {
        var resolver = new MapResolver().Add("file:///a.xsd", Schema("<xs:element name=\"a\"/>"));

        var (set, issues) = Load(Schema("<xs:include schemaLocation=\"a.xsd\"/>", "urn:root"), resolver);

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(set.Units.Count).IsEqualTo(2);
        await Assert.That(set.Units[0].EffectiveNamespace).IsEqualTo("urn:root");
        await Assert.That(set.Units[1].EffectiveNamespace).IsEqualTo("urn:root");
        await Assert.That(Names(set.Units[1])).IsEqualTo("element:a");
    }

    [Test]
    public async Task Include_OfTheSameChameleonDocument_UnderTwoNamespaces_IsTwoUnits()
    {
        var resolver = new MapResolver()
            .Add("file:///c.xsd", Schema("<xs:element name=\"c\"/>"))
            .Add("file:///b.xsd", Schema("<xs:include schemaLocation=\"c.xsd\"/>", "urn:b"));

        var (set, issues) = Load(
            Schema(
                "<xs:import namespace=\"urn:b\" schemaLocation=\"b.xsd\"/><xs:include schemaLocation=\"c.xsd\"/>",
                "urn:root"
            ),
            resolver
        );

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(set.Units.Count).IsEqualTo(4);
        await Assert.That(set.Units[2].EffectiveNamespace).IsEqualTo("urn:root");
        await Assert.That(set.Units[3].EffectiveNamespace).IsEqualTo("urn:b");
    }

    [Test]
    public async Task Import_OfADocumentWithAnotherNamespace_IsASchemaError()
    {
        var resolver = new MapResolver().Add("file:///b.xsd", Schema(string.Empty, "urn:wrong"));

        var (set, issues) = Load(
            Schema("\n  <xs:import namespace=\"urn:b\" schemaLocation=\"b.xsd\"/>", "urn:a"),
            resolver
        );

        await Assert.That(set.Units.Count).IsEqualTo(1);
        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(CodeOf("src-import"));
        await Assert.That(issues[0].Location.Line).IsEqualTo(3);
    }

    [Test]
    public async Task Import_WithoutNamespace_InASchemaWithATargetNamespace_LoadsTheNoNamespaceDocument()
    {
        var resolver = new MapResolver().Add("file:///b.xsd", Schema(string.Empty));

        var (set, issues) = Load(Schema("<xs:import schemaLocation=\"b.xsd\"/>", "urn:a"), resolver);

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(set.Units.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Import_WithoutNamespace_OfADocumentWithANamespace_IsASchemaError()
    {
        var resolver = new MapResolver().Add("file:///b.xsd", Schema(string.Empty, "urn:b"));

        var (_, issues) = Load(Schema("<xs:import schemaLocation=\"b.xsd\"/>", "urn:a"), resolver);

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(CodeOf("src-import"));
    }

    [Test]
    [Arguments("<xs:import namespace=\"urn:a\"/>", "urn:a")]
    [Arguments("<xs:import/>", "")]
    [Arguments("<xs:import schemaLocation=\"b.xsd\"/>", "")]
    public async Task Import_OfTheOwnNamespace_OrWithoutNamespaceInASchemaWithoutOne_IsASchemaError(
        string directive,
        string targetNamespace
    )
    {
        ArgumentNullException.ThrowIfNull(directive);
        ArgumentNullException.ThrowIfNull(targetNamespace);

        var (_, issues) = Load(
            targetNamespace.Length == 0 ? Schema(directive) : Schema(directive, targetNamespace),
            new MapResolver()
        );

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(CodeOf("src-import"));
    }

    [Test]
    public async Task DocumentCount_AboveTheLimit_YieldsTheLimitRuleCode_AndStopsLoading()
    {
        var resolver = new MapResolver()
            .Add("file:///a.xsd", Schema(string.Empty))
            .Add("file:///b.xsd", Schema(string.Empty))
            .Add("file:///c.xsd", Schema(string.Empty));

        var (set, issues) = Load(
            Schema(
                "<xs:include schemaLocation=\"a.xsd\"/><xs:include schemaLocation=\"b.xsd\"/>\n<xs:include schemaLocation=\"c.xsd\"/>"
            ),
            resolver,
            ValidationLimits.Default.WithMaxExternalResources(2)
        );

        await Assert.That(set.Units.Count).IsEqualTo(3);
        await Assert.That(resolver.Requests.Count).IsEqualTo(2);
        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(CoreDiagnosticCodes.ExternalResourceLimitExceeded);
        await Assert.That(issues[0].Location.Line).IsEqualTo(3);
    }

    [Test]
    public async Task DocumentCount_AtTheLimit_IsAccepted()
    {
        var resolver = new MapResolver().Add("file:///a.xsd", Schema(string.Empty));

        var (set, issues) = Load(
            Schema("<xs:include schemaLocation=\"a.xsd\"/>"),
            resolver,
            ValidationLimits.Default.WithMaxExternalResources(1)
        );

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(set.Units.Count).IsEqualTo(2);
    }

    [Test]
    public async Task ResourceSize_AboveTheLimit_YieldsTheSizeRuleCode()
    {
        var resolver = new MapResolver().Add("file:///a.xsd", Schema(new string(' ', 4096)));

        var (set, issues) = Load(
            Schema("<xs:include schemaLocation=\"a.xsd\"/>"),
            resolver,
            ValidationLimits.Default.WithMaxResourceSize(1024)
        );

        await Assert.That(set.Units.Count).IsEqualTo(1);
        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(CoreDiagnosticCodes.ResourceSizeLimitExceeded);
    }

    [Test]
    public async Task UnparsableDocument_IsALocatedSchemaError()
    {
        var resolver = new MapResolver().Add("file:///a.xsd", "<xs:schema");

        var (set, issues) = Load(Schema("<xs:include schemaLocation=\"a.xsd\"/>"), resolver);

        await Assert.That(set.Units.Count).IsEqualTo(1);
        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(CodeOf("schema_reference"));
    }

    [Test]
    public async Task Load_ObservesCancellation(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolver = new MapResolver().Add("file:///a.xsd", Schema(string.Empty));
        var limits = ValidationLimits.Default;
        var collector = new ValidationIssueCollector(limits);
        var document = SchemaDocumentParser.Parse(
            Schema("<xs:include schemaLocation=\"a.xsd\"/>").AsSpan(),
            RootUri,
            new XsdIssueReporter(collector)
        );
        using var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
#if NET8_0_OR_GREATER
        await source.CancelAsync();
#else
        source.Cancel();
#endif

        var load = () => new SchemaDocumentLoader(resolver, limits, collector).Load(document, source.Token);

        await Assert.That(load).Throws<OperationCanceledException>();
        await Assert.That(resolver.Requests.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Guards_RejectNull()
    {
        var limits = ValidationLimits.Default;
        var collector = new ValidationIssueCollector(limits);
        var resolver = DenyAllResourceResolver.Instance;

        await Assert.That(() => new SchemaDocumentLoader(null!, limits, collector)).Throws<ArgumentNullException>();
        await Assert.That(() => new SchemaDocumentLoader(resolver, null!, collector)).Throws<ArgumentNullException>();
        await Assert.That(() => new SchemaDocumentLoader(resolver, limits, null!)).Throws<ArgumentNullException>();
        await Assert
            .That(() => new SchemaDocumentLoader(resolver, limits, collector).Load(null!, CancellationToken.None))
            .Throws<ArgumentNullException>();
    }
}
