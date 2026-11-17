namespace NetEvolve.Schemas.Xsd.Tests.Unit.Api;

using System;
using System.Reflection;
using System.Threading;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;

public class ForXsdBuilderTests
{
    [Test]
    public async Task ForXsd_Builder_DefaultsToVersion11()
    {
        var builder = SchemaValidator.ForXsd();

        await Assert.That(builder.Version).IsEqualTo(XsdVersion.V11);
        await Assert.That(builder.Limits).IsSameReferenceAs(ValidationLimits.Default);
        await Assert.That(builder.Resolver).IsSameReferenceAs(DenyAllResourceResolver.Instance);
    }

    [Test]
    public async Task ForXsd_WithVersion10_SelectsStrictConfiguration()
    {
        var builder = SchemaValidator.ForXsd().WithVersion(XsdVersion.V10);

        await Assert.That(builder.Version).IsEqualTo(XsdVersion.V10);
    }

    [Test]
    public async Task ForXsd_WithVersion_ReturnsSameBuilderAndCanSwitchBack()
    {
        var builder = SchemaValidator.ForXsd();

        var chained = builder.WithVersion(XsdVersion.V10).WithVersion(XsdVersion.V11);

        await Assert.That(chained).IsSameReferenceAs(builder);
        await Assert.That(builder.Version).IsEqualTo(XsdVersion.V11);
    }

    [Test]
    public async Task ForXsd_WithLimitsAndResolver_KeepsTheValues()
    {
        var limits = ValidationLimits.Default.WithMaxIssues(3);
        var resolver = new DenyAllResolverProbe();

        var builder = SchemaValidator.ForXsd().WithLimits(limits).WithResolver(resolver);

        await Assert.That(builder.Limits).IsSameReferenceAs(limits);
        await Assert.That(builder.Resolver).IsSameReferenceAs(resolver);
    }

    [Test]
    public async Task ForXsd_Build_ThrowsUntilTheValidatorAssemblyLands()
    {
        var builder = SchemaValidator.ForXsd();

        await Assert.That(() => builder.Build()).Throws<NotSupportedException>();
        await Assert.That(async () => await builder.BuildAsync()).Throws<NotSupportedException>();
    }

    [Test]
    public async Task ForXsd_ExtensionMember_And_NamedClass_ReturnSameBuilderTypeAndDefaults()
    {
        var viaMember = SchemaValidator.ForXsd();
        var viaClass = XsdSchemaValidatorExtensions.ForXsd();
        var method = typeof(XsdSchemaValidatorExtensions).GetMethod(
            "ForXsd",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: [],
            modifiers: null
        );

        await Assert.That(method).IsNotNull();
        await Assert.That(method!.ReturnType).IsEqualTo(typeof(XsdValidatorBuilder));
        await Assert.That(viaMember.GetType()).IsEqualTo(viaClass.GetType());
        await Assert.That(viaMember.Version).IsEqualTo(viaClass.Version);
        await Assert.That(viaMember.Limits).IsSameReferenceAs(viaClass.Limits);
    }

    [Test]
    public async Task EntryPoint_NamedClass_IsPublicStaticInTheEntryPointNamespace()
    {
        var type = typeof(XsdSchemaValidatorExtensions);

        await Assert.That(type.IsPublic).IsTrue();
        await Assert.That(type.IsAbstract && type.IsSealed).IsTrue();
        await Assert.That(type.Namespace).IsEqualTo("NetEvolve.Schemas");
        await Assert.That(typeof(XsdValidatorBuilder).Namespace).IsEqualTo("NetEvolve.Schemas");
        await Assert.That(typeof(IXsdValidator).Namespace).IsEqualTo("NetEvolve.Schemas");
    }

    [Test]
    public async Task Builder_IsSealed_WithoutPublicConstructor()
    {
        var type = typeof(XsdValidatorBuilder);

        await Assert.That(type.IsSealed).IsTrue();
        await Assert.That(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)).IsEmpty();
        await Assert.That(typeof(IXsdValidator).GetInterface(nameof(ISchemaValidator))).IsNotNull();
    }

    private sealed class DenyAllResolverProbe : IResourceResolver
    {
        public ResourceResult Resolve(in ResourceRequest request, CancellationToken cancellationToken) =>
            DenyAllResourceResolver.Instance.Resolve(request, cancellationToken);
    }
}
