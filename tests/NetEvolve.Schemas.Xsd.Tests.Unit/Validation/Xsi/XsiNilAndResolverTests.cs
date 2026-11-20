namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Xsi;

using System;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.QNames;
using NetEvolve.Schemas.Xml.Text.NameTable;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Validation.Xsi;

public class XsiNilAndResolverTests
{
    private static XsdSchemaSet EmptySet() => new ComponentRegistry(new XsiFixture().Reporter).Freeze(XsdVersion.V11);

    [Test]
    [Arguments("true", true)]
    [Arguments("1", true)]
    [Arguments("false", false)]
    [Arguments("0", false)]
    [Arguments(" true ", true)]
    [Arguments("\t0\n", false)]
    public async Task Nil_ValidBoolean_IsParsed(string lexical, bool expected)
    {
        var parsed = new XsiNil(XsdVersion.V11).TryParse(lexical.AsSpan(), out var nilled);

        await Assert.That(parsed).IsTrue();
        await Assert.That(nilled).IsEqualTo(expected);
    }

    [Test]
    [Arguments("")]
    [Arguments("TRUE")]
    [Arguments("yes")]
    [Arguments("tru e")]
    public async Task Nil_InvalidBoolean_IsRejected(string lexical)
    {
        var parsed = new XsiNil(XsdVersion.V10).TryParse(lexical.AsSpan(), out var nilled);

        await Assert.That(parsed).IsFalse();
        await Assert.That(nilled).IsFalse();
    }

    [Test]
    [Arguments(-1)]
    [Arguments(2)]
    public async Task Nil_VersionOutOfRange_Throws(int version) =>
        await Assert.That(() => new XsiNil((XsdVersion)version)).Throws<ArgumentOutOfRangeException>();

    [Test]
    public async Task Resolver_NullSchema_Throws() =>
        await Assert
            .That(() => new XsiTypeResolver(null!, new XsiNamespaceScope(new SpanInterningNameTable(10)), null))
            .Throws<ArgumentNullException>();

    [Test]
    public async Task Resolver_NullNamespaces_Throws()
    {
        var set = EmptySet();

        await Assert.That(() => new XsiTypeResolver(set, null!, null)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Resolver_AValueThatIsNoQName_IsInvalid()
    {
        var set = EmptySet();
        var resolver = new XsiTypeResolver(set, new XsiNamespaceScope(new SpanInterningNameTable(10)), null);

        var result = resolver.Resolve("1x".AsSpan(), null, XsdDerivationMethod.None);

        await Assert.That(result.Status).IsEqualTo(XsiTypeStatus.InvalidQName);
        await Assert.That(result.Type).IsNull();
        await Assert.That(result.ViolatedRule).IsNull();
    }

    [Test]
    public async Task Resolver_AnUnknownType_NamesTheExpandedName()
    {
        var set = EmptySet();
        var resolver = new XsiTypeResolver(set, new XsiNamespaceScope(new SpanInterningNameTable(10)), null);

        var result = resolver.Resolve("nope".AsSpan(), null, XsdDerivationMethod.None);

        await Assert.That(result.Status).IsEqualTo(XsiTypeStatus.UnknownType);
        await Assert.That(result.Name.LocalName).IsEqualTo("nope");
    }

    [Test]
    public async Task Resolver_ANamespaceContextWithoutAPrefix_IsUnbound()
    {
        var set = EmptySet();
        var resolver = new XsiTypeResolver(set, new NoNamespaces(), null);

        var result = resolver.Resolve("p:x".AsSpan(), null, XsdDerivationMethod.None);

        await Assert.That(result.Status).IsEqualTo(XsiTypeStatus.UnboundPrefix);
    }

    private sealed class NoNamespaces : INamespaceContext
    {
        public bool TryResolvePrefix(ReadOnlySpan<char> prefix, out string? namespaceName)
        {
            namespaceName = null;
            return false;
        }
    }
}
