namespace NetEvolve.Schemas.Xml.Tests.Unit.Contracts;

using System;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Tests.Unit.Contracts.Fakes;

public class ValueTypeContractTests
{
    [Test]
    public async Task XmlSymbol_Negative_Throws() =>
        await Assert.That(() => new XmlSymbol(-1)).Throws<ArgumentOutOfRangeException>();

    [Test]
    public async Task XmlSymbol_Default_IsTheEmptyString()
    {
        await Assert.That(default(XmlSymbol).IsEmpty).IsTrue();
        await Assert.That(new XmlSymbol(3).IsEmpty).IsFalse();
        await Assert.That(new XmlSymbol(3).Value).IsEqualTo(3);
    }

    [Test]
    public async Task XmlSymbol_Equality_IsByValue()
    {
        var a = new XmlSymbol(5);
        object boxed = new XmlSymbol(5);

        await Assert.That(a == new XmlSymbol(5)).IsTrue();
        await Assert.That(a != new XmlSymbol(6)).IsTrue();
        await Assert.That(a.Equals(boxed)).IsTrue();
        await Assert.That(a.Equals("5")).IsFalse();
        await Assert.That(a.GetHashCode()).IsEqualTo(new XmlSymbol(5).GetHashCode());
        await Assert.That(a.ToString()).IsEqualTo("5");
    }

    [Test]
    public async Task XmlName_Equality_ComparesAllThreeSymbols()
    {
        var name = new XmlName(new XmlSymbol(1), new XmlSymbol(2), new XmlSymbol(3));
        object boxed = new XmlName(new XmlSymbol(1), new XmlSymbol(2), new XmlSymbol(3));

        await Assert.That(name == new XmlName(new XmlSymbol(1), new XmlSymbol(2), new XmlSymbol(3))).IsTrue();
        await Assert.That(name != new XmlName(new XmlSymbol(9), new XmlSymbol(2), new XmlSymbol(3))).IsTrue();
        await Assert.That(name != new XmlName(new XmlSymbol(1), new XmlSymbol(9), new XmlSymbol(3))).IsTrue();
        await Assert.That(name != new XmlName(new XmlSymbol(1), new XmlSymbol(2), new XmlSymbol(9))).IsTrue();
        await Assert.That(name.Equals(boxed)).IsTrue();
        await Assert.That(name.Equals(new object())).IsFalse();
        await Assert
            .That(name.GetHashCode())
            .IsEqualTo(new XmlName(new XmlSymbol(1), new XmlSymbol(2), new XmlSymbol(3)).GetHashCode());
        await Assert.That(name.Prefix.Value).IsEqualTo(1);
        await Assert.That(name.Local.Value).IsEqualTo(2);
        await Assert.That(name.NamespaceUri.Value).IsEqualTo(3);
    }

    [Test]
    public async Task XmlPosition_Equality_ComparesLineColumnAndOffset()
    {
        var position = new XmlPosition(2, 3, 40);
        object boxed = new XmlPosition(2, 3, 40);

        await Assert.That(position == new XmlPosition(2, 3, 40)).IsTrue();
        await Assert.That(position != new XmlPosition(9, 3, 40)).IsTrue();
        await Assert.That(position != new XmlPosition(2, 9, 40)).IsTrue();
        await Assert.That(position != new XmlPosition(2, 3, 9)).IsTrue();
        await Assert.That(position.Equals(boxed)).IsTrue();
        await Assert.That(position.Equals(null)).IsFalse();
        await Assert.That(position.GetHashCode()).IsEqualTo(new XmlPosition(2, 3, 40).GetHashCode());
        await Assert.That(position.ToString()).IsEqualTo("2:3 (offset 40)");
        await Assert.That(position.Line).IsEqualTo(2);
        await Assert.That(position.Column).IsEqualTo(3);
        await Assert.That(position.CharOffset).IsEqualTo(40L);
    }

    private static string[] Describe()
    {
        var declaration = new XmlDeclaration("1.0".AsSpan(), "utf-8".AsSpan(), true);
        var doctype = new XmlDoctypeInfo("a".AsSpan(), "pub".AsSpan(), "sys".AsSpan(), "<!ENTITY e 'v'>".AsSpan());

        return
        [
            declaration.Version.ToString(),
            declaration.Encoding.ToString(),
            declaration.Standalone is true ? "standalone" : "other",
            doctype.Name.ToString(),
            doctype.PublicId.ToString(),
            doctype.SystemId.ToString(),
            doctype.InternalSubset.ToString(),
        ];
    }

    [Test]
    public async Task XmlDeclaration_And_XmlDoctypeInfo_ExposeTheirSpans() =>
        await Assert
            .That(Describe())
            .IsEquivalentTo(["1.0", "utf-8", "standalone", "a", "pub", "sys", "<!ENTITY e 'v'>"]);

    [Test]
    public async Task XmlDiagnostic_ExposesCodePositionAndMessage()
    {
        var diagnostic = new XmlDiagnostic(XmlErrorCodes.XML0001, new XmlPosition(1, 2, 3));

        await Assert.That(diagnostic.Code).IsSameReferenceAs(XmlErrorCodes.XML0001);
        await Assert.That(diagnostic.Position).IsEqualTo(new XmlPosition(1, 2, 3));
        await Assert.That(diagnostic.Message).IsEqualTo(XmlErrorCodes.XML0001.Message);
        await Assert.That(() => new XmlDiagnostic(null!, default)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task XmlParseException_CarriesTheDiagnostic()
    {
        var diagnostic = new XmlDiagnostic(XmlErrorCodes.XML0005, new XmlPosition(4, 5, 6));
        var inner = new InvalidOperationException("inner");

        var withDiagnostic = new XmlParseException(diagnostic);
        var withMessage = new XmlParseException("text");
        var withInner = new XmlParseException("text", inner);
        var empty = new XmlParseException();

        await Assert.That(withDiagnostic.Diagnostic).IsSameReferenceAs(diagnostic);
        await Assert.That(withDiagnostic.Message).IsEqualTo(XmlErrorCodes.XML0005.Message);
        await Assert.That(withMessage.Message).IsEqualTo("text");
        await Assert.That(withMessage.Diagnostic).IsNull();
        await Assert.That(withInner.InnerException).IsSameReferenceAs(inner);
        await Assert.That(empty.Message).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task XmlUnparsedEntity_And_XmlNotation_ValidateTheirNames()
    {
        var entity = new XmlUnparsedEntity("pic", "pic.png", null, "png");
        var notation = new XmlNotation("png", "viewer", "pub");

        await Assert.That(entity.Name).IsEqualTo("pic");
        await Assert.That(entity.SystemId).IsEqualTo("pic.png");
        await Assert.That(entity.PublicId).IsNull();
        await Assert.That(entity.NotationName).IsEqualTo("png");
        await Assert.That(notation.Name).IsEqualTo("png");
        await Assert.That(notation.SystemId).IsEqualTo("viewer");
        await Assert.That(notation.PublicId).IsEqualTo("pub");
        await Assert.That(() => new XmlUnparsedEntity("", "s", null, "n")).Throws<ArgumentException>();
        await Assert.That(() => new XmlUnparsedEntity("e", null!, null, "n")).Throws<ArgumentNullException>();
        await Assert.That(() => new XmlUnparsedEntity("e", "s", null, null!)).Throws<ArgumentNullException>();
        await Assert.That(() => new XmlNotation(null!, null, null)).Throws<ArgumentNullException>();
        await Assert.That(() => new XmlNotation("", null, null)).Throws<ArgumentException>();
    }

    [Test]
    public async Task XmlNameTable_Intern_ReturnsHandlesThatResolveBack()
    {
        XmlNameTable table = new FakeNameTable();

        var name = table.Intern("p", "local", "urn:x");
        var again = table.Intern("p", "local", "urn:x");

        await Assert.That(name).IsEqualTo(again);
        await Assert.That(table.GetString(name.Prefix)).IsEqualTo("p");
        await Assert.That(table.GetString(name.Local)).IsEqualTo("local");
        await Assert.That(table.GetString(name.NamespaceUri)).IsEqualTo("urn:x");
        await Assert.That(table.Intern(ReadOnlySpan<char>.Empty).IsEmpty).IsTrue();
        await Assert.That(table.Count).IsEqualTo(4);
    }

    [Test]
    public async Task XmlNameTable_Lookup_FindsInternedStringsWithoutInterning()
    {
        XmlNameTable table = new FakeNameTable();
        var symbol = table.Intern("known".AsSpan());
        var count = table.Count;

        var found = table.Lookup("known".AsSpan());
        var missing = table.Lookup("unknown".AsSpan());

        await Assert.That(found).IsEqualTo("known");
        await Assert.That(missing).IsNull();
        await Assert.That(table.Count).IsEqualTo(count);
        await Assert.That(table.TryGet("known".AsSpan(), out var again)).IsTrue();
        await Assert.That(again).IsEqualTo(symbol);
        await Assert.That(table.TryGetString(new XmlSymbol(999), out _)).IsFalse();
        await Assert.That(() => table.GetString(new XmlSymbol(999))).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task XmlNameTable_Intern_WhenFrozen_ThrowsForNewNamesOnly()
    {
        XmlNameTable table = new FakeNameTable();
        var existing = table.Intern("kept".AsSpan());
        table.Freeze();

        var status = table.TryIntern("new".AsSpan(), out var symbol);

        await Assert.That(table.IsFrozen).IsTrue();
        await Assert.That(status).IsEqualTo(XmlNameTableStatus.Frozen);
        await Assert.That(symbol).IsEqualTo(default(XmlSymbol));
        await Assert.That(table.Intern("kept".AsSpan())).IsEqualTo(existing);
        await Assert.That(() => table.Intern("new".AsSpan())).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task XmlNameTable_Intern_WhenFull_Throws()
    {
        XmlNameTable table = new FakeNameTable(maxNames: 2);
        _ = table.Intern("one".AsSpan());

        var status = table.TryIntern("two".AsSpan(), out _);

        await Assert.That(status).IsEqualTo(XmlNameTableStatus.LimitExceeded);
        await Assert.That(() => table.Intern("two".AsSpan())).Throws<InvalidOperationException>();
        await Assert
            .That(() => table.Intern("p".AsSpan(), "l".AsSpan(), "u".AsSpan()))
            .Throws<InvalidOperationException>();
    }
}
