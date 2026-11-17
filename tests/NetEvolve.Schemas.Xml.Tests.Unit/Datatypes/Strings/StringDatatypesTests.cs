namespace NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Strings;

using System;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Datatypes.Strings;

public class StringDatatypesTests
{
    private static IDatatypeDescriptor<T> Get<T>(string name, XsdVersion version = XsdVersion.V10)
    {
        _ = BuiltInDatatypes.For(version).TryGet(name, out var descriptor);
        return (IDatatypeDescriptor<T>)descriptor!;
    }

    private static string View(XsdString value, string source)
    {
        var buffer = new char[source.Length];
        return new string(buffer, 0, value.CopyTo(source, buffer));
    }

    [Test]
    [Arguments(XsdVersion.V10)]
    [Arguments(XsdVersion.V11)]
    public async Task AllFourTypes_AreRegisteredInBothVersions(XsdVersion version)
    {
        var registry = BuiltInDatatypes.For(version);

        await Assert.That(registry.TryGet("string", out _)).IsTrue();
        await Assert.That(registry.TryGet("normalizedString", out _)).IsTrue();
        await Assert.That(registry.TryGet("token", out _)).IsTrue();
        await Assert.That(registry.TryGet("boolean", out _)).IsTrue();
    }

    [Test]
    public async Task String_PreservesEveryCharacterAndIsUnordered()
    {
        var type = Get<XsdString>("string");
        const string Source = " a\tb\r\n c ";

        var parsed = type.Parse(Source);

        await Assert.That(parsed.Success).IsTrue();
        await Assert.That(parsed.Value.Mode).IsEqualTo(WhitespaceMode.Preserve);
        await Assert.That(parsed.Value.Range).IsEqualTo(new TextRange(0, Source.Length));
        await Assert.That(View(parsed.Value, Source)).IsEqualTo(Source);
        await Assert.That(((IDatatypeDescriptor)type).IsOrdered).IsFalse();
    }

    [Test]
    public async Task NormalizedString_ReplacesTabLineFeedAndCarriageReturn()
    {
        var type = Get<XsdString>("normalizedString");
        const string Source = "a\tb\nc\rd  e";

        var parsed = type.Parse(Source);

        await Assert.That(parsed.Success).IsTrue();
        await Assert.That(parsed.Value.Mode).IsEqualTo(WhitespaceMode.Replace);
        await Assert.That(View(parsed.Value, Source)).IsEqualTo("a b c d  e");
    }

    [Test]
    public async Task Token_CollapsesRunsAndTrims()
    {
        var type = Get<XsdString>("token");
        const string Source = "\t a \r\n  b\t";

        var parsed = type.Parse(Source);

        await Assert.That(parsed.Success).IsTrue();
        await Assert.That(parsed.Value.Mode).IsEqualTo(WhitespaceMode.Collapse);
        await Assert.That(parsed.Value.Range).IsEqualTo(new TextRange(0, 3));
        await Assert.That(View(parsed.Value, Source)).IsEqualTo("a b");
    }

    [Test]
    [Arguments("string")]
    [Arguments("normalizedString")]
    [Arguments("token")]
    public async Task Strings_RejectACharacterOutsideTheXmlCharProduction(string name)
    {
        var type = Get<XsdString>(name);

        var control = type.Parse("ab\u0001c");
        var loneSurrogate = type.Parse("a\uD800");
        var pair = type.Parse("a😀");

        await Assert.That(control.Success).IsFalse();
        await Assert.That(control.Error).IsEqualTo(DatatypeError.InvalidCharacter);
        await Assert.That(control.ErrorOffset).IsEqualTo(2);
        await Assert.That(loneSurrogate.Error).IsEqualTo(DatatypeError.InvalidCharacter);
        await Assert.That(pair.Success).IsTrue();
        await Assert.That(type.Validate("￾").IsValid).IsFalse();
    }

    [Test]
    public async Task Strings_AcceptTheEmptyString() =>
        await Assert.That(Get<XsdString>("string").Parse(string.Empty).Success).IsTrue();

    [Test]
    public async Task StringEquality_IsOverTheNormalizedView()
    {
        var token = Get<XsdString>("token");
        var text = Get<XsdString>("string");
        const string A = " a  b ";
        const string B = "a b";

        var tokenA = token.Parse(A).Value;
        var tokenB = token.Parse(B).Value;

        await Assert.That(tokenA.Equals(A, tokenB, B)).IsTrue();
        await Assert.That(tokenA.Equals(A, text.Parse(A).Value, A)).IsFalse();
        await Assert.That(tokenA.Equals(A, token.Parse("a c").Value, "a c")).IsFalse();
    }

    [Test]
    public async Task CopyTo_ADestinationThatIsTooShort_Throws()
    {
        var value = Get<XsdString>("string").Parse("abc").Value;

        await Assert.That(() => value.CopyTo("abc", new char[2])).ThrowsException();
    }

    [Test]
    [Arguments("true", true)]
    [Arguments("false", false)]
    [Arguments("1", true)]
    [Arguments("0", false)]
    [Arguments(" \t1\n", true)]
    public async Task Boolean_MapsTheFourLexicalFormsAndCollapsesWhitespace(string lexical, bool expected)
    {
        var parsed = Get<bool>("boolean").Parse(lexical);

        await Assert.That(parsed.Success).IsTrue();
        await Assert.That(parsed.Value).IsEqualTo(expected);
        await Assert.That(XsdBoolean.ToCanonical(parsed.Value)).IsEqualTo(expected ? "true" : "false");
    }

    [Test]
    [Arguments("TRUE")]
    [Arguments("True")]
    [Arguments("yes")]
    [Arguments("")]
    [Arguments("tru")]
    [Arguments("10")]
    public async Task Boolean_RejectsEverythingElse(string lexical)
    {
        var parsed = Get<bool>("boolean").Parse(lexical);

        await Assert.That(parsed.Success).IsFalse();
        await Assert.That(parsed.Error).IsEqualTo(DatatypeError.InvalidLexicalForm);
    }

    [Test]
    public async Task Boolean_EqualityIsByValue_SoOneEqualsTrue()
    {
        var type = Get<bool>("boolean");
        var one = type.Parse("1").Value;
        var yes = type.Parse("true").Value;
        var zero = type.Parse("0").Value;

        await Assert.That(type.IsIdentical(one, yes)).IsTrue();
        await Assert.That(type.Compare(one, yes)).IsEqualTo(XsdOrder.Equal);
        await Assert.That(type.IsIdentical(one, zero)).IsFalse();
        await Assert.That(type.Compare(one, zero)).IsEqualTo(XsdOrder.Incomparable);
        await Assert.That(((IDatatypeDescriptor)type).IsOrdered).IsFalse();
    }
}
