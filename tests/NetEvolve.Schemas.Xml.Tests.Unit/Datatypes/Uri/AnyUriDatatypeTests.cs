namespace NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Uri;

using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Datatypes.Strings;
using NetEvolve.Schemas.Xml.Datatypes.Uri;

public class AnyUriDatatypeTests
{
    private static AnyUriDescriptor Get(XsdVersion version) =>
        (AnyUriDescriptor)BuiltInDatatypes.For(version).Get("anyURI");

    [Test]
    [Arguments(XsdVersion.V10, AnyUriBasis.Rfc2396)]
    [Arguments(XsdVersion.V11, AnyUriBasis.Rfc3987)]
    public async Task Basis_FollowsTheVersion(XsdVersion version, AnyUriBasis basis)
    {
        var descriptor = Get(version);

        await Assert.That(descriptor.LexicalMode).IsEqualTo(version);
        await Assert.That(descriptor.Basis).IsEqualTo(basis);
    }

    [Test]
    [Arguments(XsdVersion.V10)]
    [Arguments(XsdVersion.V11)]
    public async Task Grammar_IsLaxInBothVersions(XsdVersion version) =>
        await Assert.That(Get(version).Grammar).IsEqualTo(AnyUriGrammar.Lax);

    [Test]
    [Arguments(XsdVersion.V10)]
    [Arguments(XsdVersion.V11)]
    public async Task Descriptor_IsAnUnorderedPrimitiveNamedAnyUri(XsdVersion version)
    {
        var descriptor = Get(version);

        await Assert.That(descriptor.Name).IsEqualTo("anyURI");
        await Assert.That(descriptor.Variety).IsEqualTo(DatatypeVariety.Atomic);
        await Assert.That(descriptor.IsOrdered).IsFalse();
        await Assert.That(descriptor.Facets.Count).IsEqualTo(0);
    }

    [Test]
    [Arguments(XsdVersion.V10, "http://[")]
    [Arguments(XsdVersion.V11, "http://[")]
    [Arguments(XsdVersion.V10, "")]
    [Arguments(XsdVersion.V11, "../a b/é?x=%zz#f")]
    public async Task Lax_AcceptsAnyStringOfXmlChars_InBothVersions(XsdVersion version, string lexical)
    {
        var descriptor = Get(version);

        await Assert.That(descriptor.Parse(lexical).Success).IsTrue();
        await Assert.That(descriptor.Validate(lexical).IsValid).IsTrue();
    }

    [Test]
    [Arguments(XsdVersion.V10)]
    [Arguments(XsdVersion.V11)]
    public async Task CharacterOutsideTheXmlCharProduction_IsRejectedWithACodedError(XsdVersion version)
    {
        var descriptor = Get(version);

        var parsed = descriptor.Parse("ab\u0001c");
        var validated = descriptor.Validate("ab\u0001c");

        await Assert.That(parsed.Success).IsFalse();
        await Assert.That(parsed.Error).IsEqualTo(DatatypeError.InvalidCharacter);
        await Assert.That(parsed.ErrorOffset).IsEqualTo(2);
        await Assert.That(validated.IsValid).IsFalse();
        await Assert.That(validated.Error).IsEqualTo(DatatypeError.InvalidCharacter);
    }

    [Test]
    public async Task Whitespace_IsCollapsedBeforeTheCheck()
    {
        var descriptor = Get(XsdVersion.V11);
        const string Source = "\t a \r\n  b\t";

        var parsed = descriptor.Parse(Source);
        var buffer = new char[Source.Length];
        var length = parsed.Value.CopyTo(Source, buffer);

        await Assert.That(parsed.Success).IsTrue();
        await Assert.That(parsed.Value.Mode).IsEqualTo(WhitespaceMode.Collapse);
        await Assert.That(new string(buffer, 0, length)).IsEqualTo("a b");
    }

    [Test]
    public async Task ErrorOffset_AddressesTheOriginalInputAfterCollapse()
    {
        var parsed = Get(XsdVersion.V10).Parse("  a\u0001");

        await Assert.That(parsed.Error).IsEqualTo(DatatypeError.InvalidCharacter);
        await Assert.That(parsed.ErrorOffset).IsEqualTo(3);
    }

    [Test]
    public async Task Compare_IsIncomparable_AndIdentityIsUndefined()
    {
        var descriptor = Get(XsdVersion.V10);
        var value = descriptor.Parse("a").Value;

        await Assert.That(descriptor.Compare(in value, in value)).IsEqualTo(XsdOrder.Incomparable);
        await Assert.That(descriptor.IsIdentical(in value, in value)).IsFalse();
    }

    [Test]
    public async Task Constructor_RejectsAnUndefinedVersion()
    {
        await Assert.That(() => new AnyUriDescriptor((XsdVersion)99)).Throws<ArgumentOutOfRangeException>();
    }
}
