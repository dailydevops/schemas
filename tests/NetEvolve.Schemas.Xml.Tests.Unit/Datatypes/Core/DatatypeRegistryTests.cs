namespace NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Core;

using System;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Core.Fakes;

public class DatatypeRegistryTests
{
    private static AtomicDescriptor<int> Fake(string name, XsdVersion version) =>
        new(name, version, new FakeDigitsMapping());

    [Test]
    public async Task BuiltIn_V10_DoesNotContainTheTypesAddedIn11()
    {
        var registry = BuiltInDatatypes.For(XsdVersion.V10);

        await Assert.That(registry.TryGet("anyAtomicType", out _)).IsFalse();
        await Assert.That(registry.TryGet("anySimpleType", out _)).IsTrue();
    }

    [Test]
    public async Task BuiltIn_V11_ContainsTheTypesAddedIn11()
    {
        var registry = BuiltInDatatypes.For(XsdVersion.V11);

        await Assert.That(registry.TryGet("anyAtomicType", out var atomic)).IsTrue();
        await Assert.That(registry.TryGet("anySimpleType", out var simple)).IsTrue();
        await Assert.That(atomic!.BaseType).IsSameReferenceAs(simple);
        await Assert.That(atomic.LexicalMode).IsEqualTo(XsdVersion.V11);
    }

    [Test]
    public async Task BuiltIn_ReturnsTheSameRegistryAndDescriptorsOnEveryCall()
    {
        _ = BuiltInDatatypes.For(XsdVersion.V11).TryGet("anySimpleType", out var first);
        _ = BuiltInDatatypes.For(XsdVersion.V11).TryGet("anySimpleType", out var second);

        await Assert.That(BuiltInDatatypes.For(XsdVersion.V11)).IsSameReferenceAs(BuiltInDatatypes.For(XsdVersion.V11));
        await Assert.That(first).IsSameReferenceAs(second);
    }

    [Test]
    public async Task BuiltIn_AnySimpleType_IsTheUnorderedRootThatKeepsTheLexicalForm()
    {
        _ = BuiltInDatatypes.For(XsdVersion.V10).TryGet("anySimpleType", out var simple);
        var typed = (IDatatypeDescriptor<TextRange>)simple!;

        var parsed = typed.Parse(" a\tb ");

        await Assert.That(simple!.BaseType).IsNull();
        await Assert.That(simple.IsOrdered).IsFalse();
        await Assert.That(parsed.Success).IsTrue();
        await Assert.That(parsed.Value).IsEqualTo(new TextRange(0, 5));
        await Assert.That(typed.Compare(parsed.Value, parsed.Value)).IsEqualTo(XsdOrder.Incomparable);
    }

    [Test]
    public async Task BuiltIn_UnknownName_IsNotFound() =>
        await Assert.That(BuiltInDatatypes.For(XsdVersion.V11).TryGet("nope", out _)).IsFalse();

    [Test]
    public async Task BuiltIn_UndefinedVersion_Throws() =>
        await Assert.That(() => BuiltInDatatypes.For((XsdVersion)7)).Throws<ArgumentOutOfRangeException>();

    [Test]
    public async Task Registry_FirstVersionGatesTheRegistration()
    {
        DatatypeRegistration[] registrations =
        [
            new("old", XsdVersion.V10, static r => Fake("old", r.Version)),
            new("new", XsdVersion.V11, static r => Fake("new", r.Version)),
        ];

        var v10 = new DatatypeRegistry(XsdVersion.V10, registrations);
        var v11 = new DatatypeRegistry(XsdVersion.V11, registrations);

        await Assert.That(v10.All.Count).IsEqualTo(1);
        await Assert.That(v10.TryGet("new", out _)).IsFalse();
        await Assert.That(v11.All.Count).IsEqualTo(2);
        await Assert.That(v11.TryGet("new", out var created)).IsTrue();
        await Assert.That(created!.LexicalMode).IsEqualTo(XsdVersion.V11);
    }

    [Test]
    public async Task Registry_ResolvesDependenciesRegardlessOfRegistrationOrder()
    {
        DatatypeRegistration[] registrations =
        [
            new(
                "derived",
                XsdVersion.V10,
                static r => new AtomicDescriptor<int>("derived", (AtomicDescriptor<int>)r.Get("base"), null, [])
            ),
            new("base", XsdVersion.V10, static r => Fake("base", r.Version)),
        ];

        var registry = new DatatypeRegistry(XsdVersion.V10, registrations);

        await Assert.That(registry.Get("derived").BaseType).IsSameReferenceAs(registry.Get("base"));
        await Assert.That(registry.All.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Registry_Get_UnknownName_Throws() =>
        await Assert
            .That(() => new DatatypeRegistry(XsdVersion.V10, []).Get("missing"))
            .Throws<InvalidOperationException>();
}
