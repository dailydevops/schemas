namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Attributes;

using System;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Attributes;
using NetEvolve.Schemas.Xml.Dtd.Declarations;
using NetEvolve.Schemas.Xml.Dtd.Model;

/// <summary>The pieces behind the attribute declarations of the reader: the collapse, the compiled definitions of an element, and the table.</summary>
public class AttributeCompilationTests
{
    [Test]
    [Arguments("", "")]
    [Arguments("a", "a")]
    [Arguments(" ", "")]
    [Arguments("   ", "")]
    [Arguments(" a", "a")]
    [Arguments("a ", "a")]
    [Arguments(" a ", "a")]
    [Arguments("a  b", "a b")]
    [Arguments("a b", "a b")]
    [Arguments("  a   b   c  ", "a b c")]
    [Arguments("a\tb", "a\tb")]
    [Arguments("\ta ", "\ta")]
    [Arguments("a \n b", "a \n b")]
    public async Task Collapse_TrimsAndCollapsesOnlyTheSpaceCharacter(string value, string expected)
    {
        ArgumentNullException.ThrowIfNull(value);
        var buffer = value.ToCharArray();

        var length = DtdAttributeNormalizer.Collapse(buffer);

        await Assert.That(new string(buffer, 0, length)).IsEqualTo(expected);
    }

    private static DtdAttributeDefinition Definition(
        string name,
        DtdDefaultKind kind = DtdDefaultKind.Implied,
        string? value = null
    ) => new(name, DtdAttributeType.Cdata, null, kind, value);

    [Test]
    public async Task ElementAttributes_FindsADefinitionByTheNameAsWritten()
    {
        var attributes = new DtdElementAttributes([
            Definition("a"),
            Definition("p:b", DtdDefaultKind.Default, "x"),
            Definition("c"),
        ]);

        var found = attributes.TryFind("p:b".AsSpan(), out var index);
        var missing = attributes.TryFind("b".AsSpan(), out var none);

        await Assert.That(attributes.Count).IsEqualTo(3);
        await Assert.That(found).IsTrue();
        await Assert.That(attributes[index].Name).IsEqualTo("p:b");
        await Assert.That(missing).IsFalse();
        await Assert.That(none).IsEqualTo(-1);
    }

    [Test]
    public async Task ElementAttributes_ListsTheDefaultedDefinitionsInDeclarationOrder()
    {
        var attributes = new DtdElementAttributes([
            Definition("a", DtdDefaultKind.Default, "1"),
            Definition("b", DtdDefaultKind.Required),
            Definition("c", DtdDefaultKind.Fixed, "3"),
            Definition("d"),
        ]);

        var defaulted = attributes.Defaulted.ToArray();

        await Assert.That(string.Join(",", defaulted)).IsEqualTo("0,2");
        await Assert.That(attributes.DefaultText(0)).IsEqualTo("1");
        await Assert.That(attributes.DefaultText(2)).IsEqualTo("3");
        await Assert.That(attributes.DefaultText(1)).IsEmpty();
    }

    [Test]
    public async Task ElementAttributes_NormalizesTheLineEndsOfADefaultLiteral()
    {
        var attributes = new DtdElementAttributes([
            Definition("a", DtdDefaultKind.Default, "x\r\ny\rz\n"),
            Definition("b", DtdDefaultKind.Default, "\r"),
        ]);

        await Assert.That(attributes.DefaultText(0)).IsEqualTo("x\ny\nz\n");
        await Assert.That(attributes.DefaultText(1)).IsEqualTo("\n");
    }

    [Test]
    public async Task ElementAttributes_RejectsNullDefinitionsAndIndexesOutOfRange()
    {
        var attributes = new DtdElementAttributes([Definition("a")]);

        await Assert.That(() => new DtdElementAttributes(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => attributes[-1]).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => attributes[1]).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => attributes.DefaultText(-1)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => attributes.DefaultText(1)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Table_CompilesTheDefinitionsOfAnElementOnceAndFollowsTheDeclarations()
    {
        var handler = new DtdDoctypeHandler(XmlReaderLimits.Default);
        var table = new DtdAttributeTable(handler);

        var before = table.Find("a".AsSpan());
        _ = handler.Handle(
            new XmlDoctypeInfo("a", default, default, "<!ATTLIST a x CDATA \"1\"><!ATTLIST b y CDATA \"2\">")
        );
        var first = table.Find("a".AsSpan());
        var again = table.Find("a".AsSpan());
        var other = table.Find("b".AsSpan());
        var undeclared = table.Find("c".AsSpan());
        _ = handler.Handle(new XmlDoctypeInfo("a", default, default, "<!ATTLIST a z CDATA \"3\">"));
        var replaced = table.Find("a".AsSpan());
        var gone = table.Find("b".AsSpan());
        _ = handler.Handle(new XmlDoctypeInfo("a", default, default, "<!ELEMENT a ANY>"));
        var withoutLists = table.Find("a".AsSpan());

        await Assert.That(before).IsNull();
        await Assert.That(first).IsNotNull();
        await Assert.That(ReferenceEquals(first, again)).IsTrue();
        await Assert.That(other).IsNotNull();
        await Assert.That(undeclared).IsNull();
        await Assert.That(replaced!.TryFind("z".AsSpan(), out _)).IsTrue();
        await Assert.That(ReferenceEquals(first, replaced)).IsFalse();
        await Assert.That(gone).IsNull();
        await Assert.That(withoutLists).IsNull();
    }

    [Test]
    public async Task Table_RejectsANullHandler() =>
        await Assert.That(() => new DtdAttributeTable(null!)).Throws<ArgumentNullException>();

    [Test]
    public async Task DefaultReferences_RejectsNullDeclarations() =>
        await Assert.That(() => DtdDefaultReferences.Validate(null!)).Throws<ArgumentNullException>();
}
