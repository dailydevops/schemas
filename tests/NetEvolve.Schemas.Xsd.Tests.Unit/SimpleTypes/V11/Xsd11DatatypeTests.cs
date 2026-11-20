namespace NetEvolve.Schemas.Xsd.Tests.Unit.SimpleTypes.V11;

using System;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.SimpleTypes.V11;
using NetEvolve.Schemas.Xsd.Tests.Unit.Components;
using static NetEvolve.Schemas.Xsd.Tests.Unit.Components.Fixture;

/// <summary>
/// The datatypes that XSD 1.1 adds (dateTimeStamp, yearMonthDuration, dayTimeDuration, anyAtomicType, and xs:error) resolve in 1.1 mode
/// and are unknown types in strict 1.0 mode.
/// </summary>
public class Xsd11DatatypeTests
{
    private const string Xs = "http://www.w3.org/2001/XMLSchema";

    private static ComponentRegistry Registered(XsdVersion version, out ValidationIssueCollector collector)
    {
        var registry = NewRegistry(out collector);
        XsdBuiltInTypes.Register(registry, version);
        ErrorType.Register(registry, version);
        return registry;
    }

    private static XsdReference ToType(string name) =>
        new(XsdSymbolSpace.Types, new XPathQName(Xs, name), new DocumentLocation("file:///a.xsd", 5, 2), true);

    [Test]
    [Arguments("dateTimeStamp")]
    [Arguments("yearMonthDuration")]
    [Arguments("dayTimeDuration")]
    [Arguments("anyAtomicType")]
    [Arguments("error")]
    public async Task AVersion11TypeName_ResolvesInVersion11(string name)
    {
        var registry = Registered(XsdVersion.V11, out var collector);
        var user = new RefComponent(XsdComponentKind.SimpleType, "user", 3, ToType(name));
        registry.Add(user);

        var issues = Resolve(registry, collector, XsdVersion.V11);

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(user.Bound[0]!.Name.LocalName).IsEqualTo(name);
        await Assert.That(user.Bound[0]!.Name.NamespaceName).IsEqualTo(Xs);
        await Assert.That(user.Bound[0]!.Kind).IsEqualTo(XsdComponentKind.SimpleType);
    }

    [Test]
    [Arguments("dateTimeStamp")]
    [Arguments("yearMonthDuration")]
    [Arguments("dayTimeDuration")]
    [Arguments("anyAtomicType")]
    [Arguments("error")]
    public async Task AVersion11TypeName_IsAnUnknownTypeInStrictVersion10(string name)
    {
        var registry = Registered(XsdVersion.V10, out var collector);
        registry.Add(new RefComponent(XsdComponentKind.SimpleType, "user", 3, ToType(name)));

        var issues = Resolve(registry, collector, XsdVersion.V10);

        await Assert.That(registry.TryGet(XsdComponentKind.SimpleType, new XPathQName(Xs, name), out _)).IsFalse();
        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(CodeOf("src-resolve"));
        await Assert.That(issues[0].Location.Line).IsEqualTo(5);
    }

    [Test]
    public async Task TheErrorType_IsAUnionOfNoTypesThatDerivesFromAnySimpleType()
    {
        var registry = Registered(XsdVersion.V11, out _);

        _ = registry.TryGet(XsdComponentKind.SimpleType, new XPathQName(Xs, "error"), out var found);
        var error = (XsdBuiltInType)found!;

        await Assert.That(error.BaseType!.Name.LocalName).IsEqualTo("anySimpleType");
        await Assert.That(error.Descriptor!.Name).IsEqualTo("error");
        await Assert.That(error.Descriptor.Variety).IsEqualTo(DatatypeVariety.Union);
        await Assert.That(error.Descriptor.BaseType).IsNull();
        await Assert.That(error.Descriptor.LexicalMode).IsEqualTo(XsdVersion.V11);
        await Assert.That(error.Descriptor.IsOrdered).IsFalse();
        await Assert.That(error.Descriptor.Facets.Count).IsEqualTo(0);
    }

    [Test]
    [Arguments("")]
    [Arguments(" ")]
    [Arguments("0")]
    [Arguments("true")]
    [Arguments("anything at all")]
    public async Task TheErrorType_HasNoValidValue(string lexical)
    {
        var registry = Registered(XsdVersion.V11, out _);
        _ = registry.TryGet(XsdComponentKind.SimpleType, new XPathQName(Xs, "error"), out var found);

        var result = ((XsdBuiltInType)found!).Descriptor!.Validate(lexical.AsSpan());

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Error).IsEqualTo(DatatypeError.NoUnionMember);
    }

    [Test]
    public async Task TheErrorType_IsOneInstanceSharedByEveryRegistry()
    {
        var one = Registered(XsdVersion.V11, out _);
        var two = Registered(XsdVersion.V11, out _);

        _ = one.TryGet(XsdComponentKind.SimpleType, new XPathQName(Xs, "error"), out var first);
        _ = two.TryGet(XsdComponentKind.SimpleType, new XPathQName(Xs, "error"), out var second);

        await Assert.That(first).IsSameReferenceAs(second);
        await Assert.That(ErrorType.Instance).IsSameReferenceAs(first);
    }

    [Test]
    public async Task AGuard_RejectsANullRegistryAndAnUndefinedVersion()
    {
        var registry = NewRegistry(out _);

        await Assert.That(() => ErrorType.Register(null!, XsdVersion.V11)).Throws<ArgumentNullException>();
        await Assert.That(() => ErrorType.Register(registry, (XsdVersion)7)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => ErrorType.Register(registry, (XsdVersion)(-1))).Throws<ArgumentOutOfRangeException>();
    }
}
