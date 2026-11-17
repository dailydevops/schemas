namespace NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Names;

using System;
using System.Reflection;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Datatypes.Names;
using NetEvolve.Schemas.Xml.Datatypes.Strings;
using NetEvolve.Schemas.Xml.Text.CharClasses;

public class NameDatatypesTests
{
    private static IDatatypeDescriptor Get(string name, XsdVersion version)
    {
        _ = BuiltInDatatypes.For(version).TryGet(name, out var descriptor);
        return descriptor!;
    }

    private static bool IsValid(string type, string lexical, XsdVersion version) =>
        Get(type, version).Validate(lexical).IsValid;

    [Test]
    [Arguments(XsdVersion.V10)]
    [Arguments(XsdVersion.V11)]
    public async Task AllTypes_AreRegisteredInBothVersions(XsdVersion version)
    {
        var registry = BuiltInDatatypes.For(version);

        foreach (var name in new[] { "language", "Name", "NCName", "ID", "IDREF", "ENTITY", "NMTOKEN" })
        {
            await Assert.That(registry.TryGet(name, out var d) && d is IDatatypeDescriptor<XsdString>).IsTrue();
        }

        foreach (var name in new[] { "IDREFS", "ENTITIES", "NMTOKENS" })
        {
            await Assert.That(registry.TryGet(name, out var d) && d is NameListDescriptor).IsTrue();
        }
    }

    [Test]
    [Arguments("ID")]
    [Arguments("IDREF")]
    [Arguments("ENTITY")]
    public async Task IdTypes_DeriveFromNCName(string type)
    {
        await Assert.That(Get(type, XsdVersion.V11).BaseType!.Name).IsEqualTo("NCName");
        await Assert.That(Get("NCName", XsdVersion.V11).BaseType!.Name).IsEqualTo("Name");
    }

    [Test]
    [Arguments(XsdVersion.V10)]
    [Arguments(XsdVersion.V11)]
    public async Task Name_AcceptsColonAndRejectsBadStart(XsdVersion version)
    {
        foreach (var ok in new[] { "a", "a:b", ":a", "_x", "x-1.2", "été", "  a  " })
        {
            await Assert.That(IsValid("Name", ok, version)).IsTrue();
        }

        foreach (var bad in new[] { "", "   ", "1a", "-a", ".a", "a b", "a$", "a\uD800" })
        {
            await Assert.That(IsValid("Name", bad, version)).IsFalse();
        }
    }

    [Test]
    [Arguments("NCName")]
    [Arguments("ID")]
    [Arguments("IDREF")]
    [Arguments("ENTITY")]
    public async Task NCNameFamily_RejectsColon(string type)
    {
        foreach (var version in new[] { XsdVersion.V10, XsdVersion.V11 })
        {
            await Assert.That(IsValid(type, "a.b-c_d", version)).IsTrue();
            await Assert.That(IsValid(type, "a:b", version)).IsFalse();
            await Assert.That(IsValid(type, ":a", version)).IsFalse();
            await Assert.That(IsValid(type, "a:", version)).IsFalse();
            await Assert.That(IsValid(type, "1a", version)).IsFalse();
            await Assert.That(IsValid(type, "", version)).IsFalse();
        }
    }

    [Test]
    [Arguments(XsdVersion.V10)]
    [Arguments(XsdVersion.V11)]
    public async Task NmToken_AcceptsNameCharactersInAnyPosition(XsdVersion version)
    {
        foreach (var ok in new[] { "a", "1a", "-x", ".x", "a:b", " a ", "·" })
        {
            await Assert.That(IsValid("NMTOKEN", ok, version)).IsTrue();
        }

        foreach (var bad in new[] { "", "a b", "a$", "a\uD800" })
        {
            await Assert.That(IsValid("NMTOKEN", bad, version)).IsFalse();
        }
    }

    [Test]
    public async Task ErrorOffset_PointsAtTheFirstBadCharacterOfTheSource()
    {
        var result = Get("Name", XsdVersion.V11).Validate("  ab$c");

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Error).IsEqualTo(DatatypeError.InvalidLexicalForm);
        await Assert.That(result.Offset).IsEqualTo(4);
    }

    [Test]
    public async Task SupplementaryPlanes_FollowTheEditionProductions()
    {
        const string LowestSupplementary = "𐀀"; // U+10000
        const string HighestName = "󯿿"; // U+EFFFF
        const string PastName = "󰀀"; // U+F0000

        foreach (var type in new[] { "Name", "NCName", "NMTOKEN", "ID" })
        {
            await Assert.That(IsValid(type, LowestSupplementary, XsdVersion.V11)).IsTrue();
            await Assert.That(IsValid(type, "a" + HighestName, XsdVersion.V11)).IsTrue();
            await Assert.That(IsValid(type, HighestName, XsdVersion.V11)).IsTrue();
            await Assert.That(IsValid(type, PastName, XsdVersion.V11)).IsFalse();
            await Assert.That(IsValid(type, "a" + PastName, XsdVersion.V11)).IsFalse();
            // The Second Edition classes are closed and have no supplementary members.
            await Assert.That(IsValid(type, LowestSupplementary, XsdVersion.V10)).IsFalse();
        }
    }

    [Test]
    [Arguments("en")]
    [Arguments("en-US")]
    [Arguments("x-private")]
    [Arguments("de-1996")]
    [Arguments("abcdefgh")]
    [Arguments("a-b-c")]
    [Arguments("  en-US  ")]
    public async Task Language_AcceptsRfcStyleForms(string lexical)
    {
        await Assert.That(IsValid("language", lexical, XsdVersion.V10)).IsTrue();
        await Assert.That(IsValid("language", lexical, XsdVersion.V11)).IsTrue();
    }

    [Test]
    [Arguments("")]
    [Arguments("-en")]
    [Arguments("en-")]
    [Arguments("en--US")]
    [Arguments("abcdefghi")]
    [Arguments("en-abcdefghi")]
    [Arguments("1en")]
    [Arguments("e1")]
    [Arguments("en_US")]
    [Arguments("en US")]
    [Arguments("én")]
    public async Task Language_RejectsMalformedForms(string lexical)
    {
        await Assert.That(IsValid("language", lexical, XsdVersion.V10)).IsFalse();
        await Assert.That(IsValid("language", lexical, XsdVersion.V11)).IsFalse();
    }

    [Test]
    public async Task Language_DerivesFromToken() =>
        await Assert.That(Get("language", XsdVersion.V11).BaseType!.Name).IsEqualTo("token");

    [Test]
    [Arguments("IDREFS")]
    [Arguments("ENTITIES")]
    [Arguments("NMTOKENS")]
    public async Task Lists_SplitOnCollapsedWhitespace(string type)
    {
        var list = (NameListDescriptor)Get(type, XsdVersion.V11);
        const string Source = "  a \t b\n\nc  ";

        var parsed = list.Parse(Source, out var index);

        await Assert.That(parsed.Success).IsTrue();
        await Assert.That(parsed.Value.ItemCount).IsEqualTo(3);
        await Assert.That(index).IsEqualTo(-1);
        await Assert.That(list.Validate(Source).IsValid).IsTrue();
    }

    [Test]
    [Arguments("IDREFS")]
    [Arguments("ENTITIES")]
    [Arguments("NMTOKENS")]
    public async Task Lists_RejectAnEmptyList(string type)
    {
        var list = (NameListDescriptor)Get(type, XsdVersion.V11);

        foreach (var lexical in new[] { "", "   ", "\t\n" })
        {
            var validation = list.Validate(lexical, out var index);
            await Assert.That(validation.IsValid).IsFalse();
            await Assert.That(validation.FailedFacet).IsEqualTo(FacetKind.MinLength);
            await Assert.That(index).IsEqualTo(-1);
            await Assert.That(list.Parse(lexical).Success).IsFalse();
        }
    }

    [Test]
    [Arguments("IDREFS", "a 1b c", 1, 2)]
    [Arguments("ENTITIES", "a b c:d", 2, 5)]
    [Arguments("IDREFS", "1a b", 0, 0)]
    public async Task NameLists_ReportTheFailingItemIndex(string type, string lexical, int itemIndex, int offset)
    {
        var list = (NameListDescriptor)Get(type, XsdVersion.V11);

        var validation = list.Validate(lexical, out var index);
        var parsed = list.Parse(lexical, out var parsedIndex);

        await Assert.That(validation.IsValid).IsFalse();
        await Assert.That(validation.Error).IsEqualTo(DatatypeError.InvalidListItem);
        await Assert.That(validation.Offset).IsEqualTo(offset);
        await Assert.That(index).IsEqualTo(itemIndex);
        await Assert.That(parsed.Success).IsFalse();
        await Assert.That(parsedIndex).IsEqualTo(itemIndex);
    }

    [Test]
    public async Task NmTokens_AcceptNameCharactersThatNamesRefuse()
    {
        var list = (NameListDescriptor)Get("NMTOKENS", XsdVersion.V11);

        await Assert.That(list.Validate("1a -b a:b").IsValid).IsTrue();
        await Assert.That(list.Validate("1a b$").IsValid).IsFalse();
    }

    [Test]
    [Arguments(XsdVersion.V10, NameEdition.Xml10SecondEdition)]
    [Arguments(XsdVersion.V11, NameEdition.Xml10FifthEdition)]
    public async Task NameChecks_AgreeWithTheSharedCharacterClassesOnEveryCodePoint(
        XsdVersion version,
        NameEdition edition
    )
    {
        var name = Get("Name", version);
        var ncName = Get("NCName", version);
        var nmToken = Get("NMTOKEN", version);
        var mismatches = 0;
        var buffer = new char[2];

        for (var cp = 0; cp <= 0x10FFFF; cp++)
        {
            if (cp is >= 0xD800 and <= 0xDFFF)
            {
                continue;
            }

            var length = 1;
            if (cp > 0xFFFF)
            {
                var offset = cp - 0x10000;
                buffer[0] = (char)(0xD800 + (offset >> 10));
                buffer[1] = (char)(0xDC00 + (offset & 0x3FF));
                length = 2;
            }
            else
            {
                buffer[0] = (char)cp;
            }

            ReadOnlySpan<char> text = buffer.AsSpan(0, length);
            var start = XmlNameClass.IsNameStartChar(edition, cp);
            var part = XmlNameClass.IsNameChar(edition, cp);
            if (
                name.Validate(text).IsValid != start
                || ncName.Validate(text).IsValid != (start && cp != ':')
                || nmToken.Validate(text).IsValid != part
            )
            {
                mismatches++;
            }
        }

        await Assert.That(mismatches).IsEqualTo(0);
    }

    [Test]
    public async Task NamesFolder_HoldsNoSecondNameTable()
    {
        var tables = 0;
        foreach (var type in typeof(NameListDescriptor).Assembly.GetTypes())
        {
            if (!string.Equals(type.Namespace, typeof(NameListDescriptor).Namespace, StringComparison.Ordinal))
            {
                continue;
            }

            foreach (
                var field in type.GetFields(
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly
                )
            )
            {
                if (field.FieldType.IsArray)
                {
                    tables++;
                }
            }
        }

        await Assert.That(tables).IsEqualTo(0);
    }
}
