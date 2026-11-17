namespace NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.QNames;

using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Datatypes.QNames;

public class XsdQNameTests
{
    private static readonly FakeNamespaceContext Context = new(
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["a"] = "urn:one",
            ["b"] = "urn:one",
            ["c"] = "urn:two",
        }
    );

    private sealed class FakeNamespaceContext : INamespaceContext
    {
        private readonly Dictionary<string, string> _bindings;

        public FakeNamespaceContext(Dictionary<string, string> bindings) => _bindings = bindings;

        public bool TryResolvePrefix(ReadOnlySpan<char> prefix, out string? namespaceName)
        {
            var found = _bindings.TryGetValue(prefix.ToString(), out var uri);
            namespaceName = uri;
            return found;
        }
    }

    [Test]
    [Arguments("QName", XsdVersion.V10)]
    [Arguments("QName", XsdVersion.V11)]
    [Arguments("NOTATION", XsdVersion.V10)]
    [Arguments("NOTATION", XsdVersion.V11)]
    public async Task Registry_ContainsTheTypes_InBothVersions(string name, XsdVersion version)
    {
        var descriptor = BuiltInDatatypes.For(version).Get(name);

        await Assert.That(descriptor.Name).IsEqualTo(name);
        await Assert.That(descriptor.LexicalMode).IsEqualTo(version);
    }

    [Test]
    public async Task Parse_ResolvesThePrefixThroughTheContext()
    {
        var result = XsdQName.Parse("a:item", Context);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Value.NamespaceName).IsEqualTo("urn:one");
        await Assert.That(result.Value.LocalName).IsEqualTo("item");
        await Assert.That(result.Value.ToString()).IsEqualTo("{urn:one}item");
    }

    [Test]
    public async Task Parse_TwoPrefixesForOneNamespace_GiveEqualValues()
    {
        var first = XsdQName.Parse("a:item", Context).Value;
        var second = XsdQName.Parse("b:item", Context).Value;
        var otherNamespace = XsdQName.Parse("c:item", Context).Value;
        var otherLocal = XsdQName.Parse("a:other", Context).Value;

        await Assert.That(first.Equals(second)).IsTrue();
        await Assert.That(first.Equals((object)second)).IsTrue();
        await Assert.That(first.GetHashCode()).IsEqualTo(second.GetHashCode());
        await Assert.That(first.Equals(otherNamespace)).IsFalse();
        await Assert.That(first.Equals(otherLocal)).IsFalse();
        await Assert.That(first.Equals("a:item")).IsFalse();
    }

    [Test]
    public async Task Parse_UnboundPrefix_IsACodedError()
    {
        var result = XsdQName.Parse("zz:item", Context);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Error).IsEqualTo(DatatypeError.UnboundPrefix);
        await Assert.That((int)result.Error).IsEqualTo(14);
        await Assert.That(result.ErrorOffset).IsEqualTo(0);
    }

    [Test]
    public async Task Parse_WithoutAPrefix_UsesTheDefaultNamespaceWhenBound()
    {
        var withDefault = new FakeNamespaceContext(
            new Dictionary<string, string>(StringComparer.Ordinal) { [""] = "urn:default" }
        );

        await Assert.That(XsdQName.Parse("item", withDefault).Value.NamespaceName).IsEqualTo("urn:default");
        await Assert.That(XsdQName.Parse("item", Context).Value.NamespaceName).IsNull();
        await Assert.That(XsdQName.Parse("item", Context).Value.ToString()).IsEqualTo("item");
    }

    [Test]
    [Arguments("", DatatypeError.InvalidLexicalForm, 0)]
    [Arguments(":item", DatatypeError.InvalidLexicalForm, 0)]
    [Arguments("a:", DatatypeError.InvalidLexicalForm, 2)]
    [Arguments("a:b:c", DatatypeError.InvalidCharacter, 3)]
    [Arguments("1a", DatatypeError.InvalidCharacter, 0)]
    [Arguments("a:1b", DatatypeError.InvalidCharacter, 2)]
    [Arguments("a b", DatatypeError.InvalidCharacter, 1)]
    public async Task Parse_RejectsMalformedForms(string lexical, DatatypeError error, int offset)
    {
        var result = XsdQName.Parse(lexical, Context);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Error).IsEqualTo(error);
        await Assert.That(result.ErrorOffset).IsEqualTo(offset);
    }

    [Test]
    public async Task Parse_NullContext_Throws() =>
        await Assert.That(() => XsdQName.Parse("a:item", null!)).Throws<ArgumentNullException>();

    [Test]
    [Arguments("QName")]
    [Arguments("NOTATION")]
    public async Task Descriptor_ChecksTheLexicalFormAfterWhitespaceCollapse(string name)
    {
        var descriptor = BuiltInDatatypes.For(XsdVersion.V11).Get(name);

        await Assert.That(descriptor.Validate("  a:item \n").IsValid).IsTrue();
        await Assert.That(descriptor.Validate("item").IsValid).IsTrue();
        await Assert.That(descriptor.Validate("a:b:c").Error).IsEqualTo(DatatypeError.InvalidCharacter);
        await Assert.That(descriptor.Validate("a:b:c").Offset).IsEqualTo(3);
        await Assert.That(descriptor.Validate("").IsValid).IsFalse();
    }

    [Test]
    public async Task Descriptor_ValuesAreUnordered()
    {
        var descriptor = (IDatatypeDescriptor<TextRange>)BuiltInDatatypes.For(XsdVersion.V11).Get("QName");
        var value = descriptor.Parse("a:item").Value;

        await Assert.That(descriptor.IsOrdered).IsFalse();
        await Assert.That(descriptor.Compare(value, value)).IsEqualTo(XsdOrder.Incomparable);
        await Assert.That(descriptor.IsIdentical(value, value)).IsFalse();
    }
}
