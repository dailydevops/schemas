namespace NetEvolve.Schemas.Xsd.Tests.Unit.SimpleTypes.Checks;

using System;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Facets.Core;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.SimpleTypes.Checks;

/// <summary>The family of a base type decides which facets apply and which value space orders the bounds.</summary>
public class FacetBaseTests
{
    [Test]
    [Arguments("string", PrimitiveKind.StringType)]
    [Arguments("normalizedString", PrimitiveKind.StringType)]
    [Arguments("token", PrimitiveKind.StringType)]
    [Arguments("language", PrimitiveKind.StringType)]
    [Arguments("NCName", PrimitiveKind.StringType)]
    [Arguments("ID", PrimitiveKind.StringType)]
    [Arguments("boolean", PrimitiveKind.Boolean)]
    [Arguments("decimal", PrimitiveKind.DecimalType)]
    [Arguments("integer", PrimitiveKind.DecimalType)]
    [Arguments("unsignedByte", PrimitiveKind.DecimalType)]
    [Arguments("float", PrimitiveKind.FloatType)]
    [Arguments("double", PrimitiveKind.DoubleType)]
    [Arguments("duration", PrimitiveKind.Duration)]
    [Arguments("yearMonthDuration", PrimitiveKind.Duration)]
    [Arguments("dateTime", PrimitiveKind.DateTime)]
    [Arguments("dateTimeStamp", PrimitiveKind.DateTime)]
    [Arguments("time", PrimitiveKind.Time)]
    [Arguments("date", PrimitiveKind.Date)]
    [Arguments("gYearMonth", PrimitiveKind.GYearMonth)]
    [Arguments("gYear", PrimitiveKind.GYear)]
    [Arguments("gMonthDay", PrimitiveKind.GMonthDay)]
    [Arguments("gDay", PrimitiveKind.GDay)]
    [Arguments("gMonth", PrimitiveKind.GMonth)]
    [Arguments("hexBinary", PrimitiveKind.HexBinary)]
    [Arguments("base64Binary", PrimitiveKind.Base64Binary)]
    [Arguments("anyURI", PrimitiveKind.AnyUri)]
    [Arguments("QName", PrimitiveKind.QName)]
    [Arguments("NOTATION", PrimitiveKind.Notation)]
    public async Task ABuiltInType_ResolvesToItsPrimitive(string name, PrimitiveKind primitive)
    {
        var fixture = new FacetFixture();

        var viaComponent = FacetBase.Resolve(fixture.BuiltIn(name), XsdVersion.V11);
        var viaDefinition = FacetBase.Resolve(fixture.Base(name), XsdVersion.V11);

        await Assert.That(viaComponent).IsNotNull();
        await Assert.That(viaComponent!.Primitive).IsEqualTo(primitive);
        await Assert.That(viaComponent.Variety).IsEqualTo(DatatypeVariety.Atomic);
        await Assert.That(viaDefinition).IsNotNull();
        await Assert.That(viaDefinition!.Primitive).IsEqualTo(primitive);
    }

    [Test]
    [Arguments(XsdVersion.V10)]
    [Arguments(XsdVersion.V11)]
    public async Task EveryAtomicBuiltInTypeOfTheRegistry_HasAPrimitive(XsdVersion version)
    {
        var types = XsdBuiltInTypes.For(version);
        var resolved = 0;
        for (var i = 0; i < types.Count; i++)
        {
            if (
                types[i] is not XsdBuiltInType { Descriptor: { } descriptor }
                || descriptor.Variety != DatatypeVariety.Atomic
                || descriptor.Name is "anySimpleType" or "anyAtomicType"
            )
            {
                continue;
            }

            var facts = FacetBase.Resolve(types[i], version);

            await Assert.That(facts).IsNotNull();
            await Assert.That(facts!.Primitive).IsNotEqualTo(PrimitiveKind.None);
            resolved++;
        }

        await Assert.That(resolved).IsGreaterThan(40);
    }

    [Test]
    [Arguments("NMTOKENS")]
    [Arguments("IDREFS")]
    [Arguments("ENTITIES")]
    public async Task ABuiltInListType_ResolvesWithoutAPrimitive(string name)
    {
        var fixture = new FacetFixture();

        var facts = FacetBase.Resolve(fixture.BuiltIn(name), XsdVersion.V11);

        await Assert.That(facts).IsNotNull();
        await Assert.That(facts!.Variety).IsEqualTo(DatatypeVariety.List);
        await Assert.That(facts.Primitive).IsEqualTo(PrimitiveKind.None);
    }

    [Test]
    public async Task AListAndAUnionDefinition_ResolveWithoutAPrimitive()
    {
        var fixture = new FacetFixture();

        var list = FacetBase.Resolve(fixture.Base("list:int"), XsdVersion.V11);
        var union = FacetBase.Resolve(fixture.Base("union:int+string"), XsdVersion.V11);

        await Assert.That(list!.Variety).IsEqualTo(DatatypeVariety.List);
        await Assert.That(list.Primitive).IsEqualTo(PrimitiveKind.None);
        await Assert.That(union!.Variety).IsEqualTo(DatatypeVariety.Union);
        await Assert.That(union.Primitive).IsEqualTo(PrimitiveKind.None);
    }

    [Test]
    public async Task ARestrictionOfARestriction_ResolvesToThePrimitiveOfTheRoot()
    {
        var fixture = new FacetFixture();
        var first = fixture.Base("integer");
        var second = fixture.Derive(first);
        var third = fixture.Derive(second);

        var facts = FacetBase.Resolve(third, XsdVersion.V11);

        await Assert.That(facts!.Primitive).IsEqualTo(PrimitiveKind.DecimalType);
    }

    [Test]
    [Arguments("anySimpleType")]
    [Arguments("anyAtomicType")]
    [Arguments("anyType")]
    public async Task TheUrTypes_AreNotSeenThrough(string name)
    {
        var fixture = new FacetFixture();

        var facts = FacetBase.Resolve(fixture.BuiltIn(name), XsdVersion.V11);

        await Assert.That(facts).IsNull();
    }

    [Test]
    public async Task TheGuard_RejectsANullBase() =>
        await Assert.That(() => FacetBase.Resolve(null!, XsdVersion.V11)).Throws<ArgumentNullException>();
}
