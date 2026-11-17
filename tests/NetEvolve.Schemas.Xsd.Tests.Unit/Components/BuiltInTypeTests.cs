namespace NetEvolve.Schemas.Xsd.Tests.Unit.Components;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using static NetEvolve.Schemas.Xsd.Tests.Unit.Components.Fixture;

public class BuiltInTypeTests
{
    private const string Xs = "http://www.w3.org/2001/XMLSchema";

    /// <summary>Datatypes 1.0 section 3: the 19 primitive and 25 derived types, plus the two ur-types.</summary>
    private static readonly string[] Xsd10Names =
    [
        "anyType", "anySimpleType",
        "string", "boolean", "decimal", "float", "double", "duration", "dateTime", "time", "date", "gYearMonth", "gYear",
        "gMonthDay", "gDay", "gMonth", "hexBinary", "base64Binary", "anyURI", "QName", "NOTATION",
        "normalizedString", "token", "language", "NMTOKEN", "NMTOKENS", "Name", "NCName", "ID", "IDREF", "IDREFS",
        "ENTITY", "ENTITIES", "integer", "nonPositiveInteger", "negativeInteger", "long", "int", "short", "byte",
        "nonNegativeInteger", "unsignedLong", "unsignedInt", "unsignedShort", "unsignedByte", "positiveInteger",
    ];

    private static ComponentRegistry Registered(XsdVersion version)
    {
        var registry = NewRegistry(out _);
        XsdBuiltInTypes.Register(registry, version);
        return registry;
    }

    [Test]
    [MethodDataSource(nameof(Names))]
    public async Task Xsd10BuiltInType_Resolves(string name)
    {
        var registry = Registered(XsdVersion.V10);

        await Assert.That(registry.TryGet(XsdComponentKind.SimpleType, new XPathQName(Xs, name), out var type)).IsTrue();
        await Assert.That(type!.Name.LocalName).IsEqualTo(name);
    }

    public static IEnumerable<Func<string>> Names() => Xsd10Names.Select(n => (Func<string>)(() => n));

    [Test]
    public async Task AnyType_IsAComplexType_AndTheRootOfTheChain()
    {
        var registry = Registered(XsdVersion.V10);

        _ = registry.TryGet(XsdComponentKind.ComplexType, new XPathQName(Xs, "anyType"), out var anyType);
        _ = registry.TryGet(XsdComponentKind.SimpleType, new XPathQName(Xs, "int"), out var type);
        var chain = new List<string>();
        for (var t = (XsdBuiltInType?)type; t is not null; t = t.BaseType)
        {
            chain.Add(t.Name.LocalName);
        }

        await Assert.That(anyType!.Kind).IsEqualTo(XsdComponentKind.ComplexType);
        await Assert
            .That(string.Join(">", chain))
            .IsEqualTo("int>long>integer>decimal>anySimpleType>anyType");
    }

    [Test]
    public async Task BuiltInTypes_AreCreatedOnceAndShared()
    {
        await Assert.That(XsdBuiltInTypes.For(XsdVersion.V11)).IsSameReferenceAs(XsdBuiltInTypes.For(XsdVersion.V11));

        var a = Registered(XsdVersion.V11);
        var b = Registered(XsdVersion.V11);
        _ = a.TryGet(XsdComponentKind.SimpleType, new XPathQName(Xs, "string"), out var one);
        _ = b.TryGet(XsdComponentKind.SimpleType, new XPathQName(Xs, "string"), out var two);

        await Assert.That(one).IsSameReferenceAs(two);
    }

    [Test]
    public async Task Version11Types_AreAbsentFromVersion10()
    {
        var anyAtomic = new XPathQName(Xs, "anyAtomicType");

        await Assert.That(Registered(XsdVersion.V10).TryGet(XsdComponentKind.SimpleType, anyAtomic, out _)).IsFalse();
        await Assert.That(Registered(XsdVersion.V11).TryGet(XsdComponentKind.SimpleType, anyAtomic, out _)).IsTrue();
    }
}
