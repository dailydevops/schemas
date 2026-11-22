namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Contracts;

using System;
using System.Reflection;
using System.Threading;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Contracts.Fakes;

public class DtdValidatorEntryPointTests
{
    [Test]
    public async Task ForDtd_Builder_DefaultsDenyEverythingExternal()
    {
        var builder = SchemaValidator.ForDtd();

        await Assert.That(builder.Limits).IsSameReferenceAs(ValidationLimits.Default);
        await Assert.That(builder.Resolver).IsSameReferenceAs(DenyAllResourceResolver.Instance);
        await Assert.That(builder.AllowExternalSubset).IsFalse();
        await Assert.That(builder.AllowExternalGeneralEntities).IsFalse();
        await Assert.That(builder.AllowExternalParameterEntities).IsFalse();
        await Assert.That(builder.NonDeterministicContentModels).IsEqualTo(DtdNonDeterminism.Warn);
        await Assert.That(builder.DtdLimits).IsSameReferenceAs(DtdValidationLimits.Default);
    }

    [Test]
    public async Task ForDtd_Options_CombineResolverLimitsFlagsAndMode()
    {
        var resolver = new FakeDtdResolver();
        var limits = ValidationLimits.Default.WithMaxIssues(3);
        var dtdLimits = DtdValidationLimits.Default.WithMaxDeclarations(9);

        var options = SchemaValidator
            .ForDtd()
            .WithResolver(resolver)
            .WithLimits(limits)
            .WithDtdLimits(dtdLimits)
            .WithAllowExternalSubset(true)
            .WithAllowExternalGeneralEntities(true)
            .WithAllowExternalParameterEntities(true)
            .WithNonDeterministicContentModels(DtdNonDeterminism.Reject)
            .Options;

        await Assert.That(options.Resolver).IsSameReferenceAs(resolver);
        await Assert.That(options.Limits).IsSameReferenceAs(limits);
        await Assert.That(options.DtdLimits).IsSameReferenceAs(dtdLimits);
        await Assert.That(options.AllowExternalSubset).IsTrue();
        await Assert.That(options.AllowExternalGeneralEntities).IsTrue();
        await Assert.That(options.AllowExternalParameterEntities).IsTrue();
        await Assert.That(options.NonDeterministicContentModels).IsEqualTo(DtdNonDeterminism.Reject);
    }

    [Test]
    public async Task ForDtd_With_ReturnsTheSameBuilder()
    {
        var builder = SchemaValidator.ForDtd();

        var chained = builder
            .WithAllowExternalSubset(true)
            .WithAllowExternalGeneralEntities(false)
            .WithAllowExternalParameterEntities(true)
            .WithNonDeterministicContentModels(DtdNonDeterminism.Accept)
            .WithDtdLimits(DtdValidationLimits.Default);

        await Assert.That(chained).IsSameReferenceAs(builder);
        await Assert.That(builder.AllowExternalSubset).IsTrue();
        await Assert.That(builder.AllowExternalGeneralEntities).IsFalse();
        await Assert.That(builder.AllowExternalParameterEntities).IsTrue();
        await Assert.That(builder.NonDeterministicContentModels).IsEqualTo(DtdNonDeterminism.Accept);
    }

    [Test]
    public async Task ForDtd_Guards_RejectNullAndOutOfRangeValues()
    {
        var builder = SchemaValidator.ForDtd();

        await Assert.That(() => builder.WithDtdLimits(null!)).Throws<ArgumentNullException>();
        await Assert
            .That(() => builder.WithNonDeterministicContentModels((DtdNonDeterminism)3))
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task ForDtd_Build_ReturnsAValidatorOfTheMarkerType(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var validator = await SchemaValidator.ForDtd().BuildAsync(cancellationToken);

        await Assert.That(validator).IsAssignableTo<IDtdValidator>();
    }

    [Test]
    public async Task ForDtd_ExtensionMember_And_NamedClass_ReturnSameBuilderTypeAndDefaults()
    {
        var viaMember = SchemaValidator.ForDtd();
        var viaClass = DtdSchemaValidatorExtensions.ForDtd();
        var method = typeof(DtdSchemaValidatorExtensions).GetMethod(
            "ForDtd",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: [],
            modifiers: null
        );

        await Assert.That(method).IsNotNull();
        await Assert.That(method!.ReturnType).IsEqualTo(typeof(DtdValidatorBuilder));
        await Assert.That(viaMember.GetType()).IsEqualTo(viaClass.GetType());
        await Assert.That(viaMember).IsNotSameReferenceAs(viaClass);
        await Assert.That(viaMember.Limits).IsSameReferenceAs(viaClass.Limits);
    }

    [Test]
    public async Task EntryPoint_Types_AreInTheEntryPointNamespaceAndTheXmlAssembly()
    {
        var type = typeof(DtdSchemaValidatorExtensions);

        await Assert.That(type.IsPublic).IsTrue();
        await Assert.That(type.IsAbstract && type.IsSealed).IsTrue();
        await Assert.That(type.Namespace).IsEqualTo("NetEvolve.Schemas");
        await Assert.That(typeof(DtdValidatorBuilder).Namespace).IsEqualTo("NetEvolve.Schemas");
        await Assert.That(typeof(IDtdValidator).Namespace).IsEqualTo("NetEvolve.Schemas");
        await Assert.That(type.Assembly.GetName().Name).IsEqualTo("NetEvolve.Schemas.Xml");
        await Assert.That(typeof(DtdValidatorBuilder).Assembly).IsEqualTo(type.Assembly);
        await Assert.That(typeof(IDtdValidator).Assembly).IsEqualTo(type.Assembly);
    }

    [Test]
    public async Task Builder_IsSealed_WithoutPublicConstructor_AndValidatorIsAMarkerOfTheCoreContract()
    {
        var type = typeof(DtdValidatorBuilder);

        await Assert.That(type.IsSealed).IsTrue();
        await Assert.That(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)).IsEmpty();
        await Assert.That(typeof(IDtdValidator).IsInterface).IsTrue();
        await Assert.That(typeof(IDtdValidator).GetInterface(nameof(ISchemaValidator))).IsNotNull();
        await Assert.That(typeof(IDtdValidator).GetMembers()).IsEmpty();
    }
}
