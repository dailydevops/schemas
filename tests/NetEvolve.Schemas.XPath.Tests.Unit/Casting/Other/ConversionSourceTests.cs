namespace NetEvolve.Schemas.XPath.Tests.Unit.Casting.Other;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Datatypes.Binary;
using NetEvolve.Schemas.Xml.Datatypes.QNames;
using NetEvolve.Schemas.XPath.Casting.Core;
using NetEvolve.Schemas.XPath.Casting.Other;
using NetEvolve.Schemas.XPath.Casting.Temporal;
using NetEvolve.Schemas.XPath.Tests.Unit.Casting.Core;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The conversions are told the source type by the item alone, so a conversion that a custom row hands an item of another type reports
/// <see cref="CastStatus.NotAllowed"/>; the value types of the new items answer only for their own kinds.
/// </summary>
public class ConversionSourceTests
{
    private static readonly CastConversion[] TemporalTargets =
    [
        TemporalConversions.ToDateTime,
        TemporalConversions.ToDate,
        TemporalConversions.ToTime,
        TemporalConversions.ToGYearMonth,
        TemporalConversions.ToGYear,
        TemporalConversions.ToGMonthDay,
        TemporalConversions.ToGDay,
        TemporalConversions.ToGMonth,
        TemporalConversions.ToDuration,
        TemporalConversions.ToYearMonthDuration,
        TemporalConversions.ToDayTimeDuration,
        TemporalConversions.ToStringType,
        TemporalConversions.ToUntypedAtomic,
    ];

    private static readonly CastConversion[] OtherTargets =
    [
        OtherConversions.ToHexBinary,
        OtherConversions.ToBase64Binary,
        OtherConversions.ToAnyUri,
        OtherConversions.ToStringType,
        OtherConversions.ToUntypedAtomic,
    ];

    // A value of a family that no row of the new types takes as a source: numbers, booleans, and a node-less default item.
    public static IEnumerable<Func<XdmItem>> ForeignSources()
    {
        XdmItem[] items =
        [
            XdmItem.FromBoolean(true),
            XdmItem.FromInteger(1),
            XdmItem.FromDouble(1.5),
            XdmItem.FromFloat(1.5f),
            default,
        ];
        foreach (var item in items)
        {
            var captured = item;
            yield return () => captured;
        }
    }

    [Test]
    [MethodDataSource(nameof(ForeignSources))]
    public async Task TemporalConversion_OfAForeignSource_IsNotAllowed(XdmItem source)
    {
        foreach (var conversion in TemporalTargets)
        {
            await Assert.That(conversion(in source, out var result)).IsEqualTo(CastStatus.NotAllowed);
            await Assert.That(result).IsEqualTo(default(XdmItem));
        }
    }

    [Test]
    [MethodDataSource(nameof(ForeignSources))]
    public async Task OtherConversion_OfAForeignSource_IsNotAllowed(XdmItem source)
    {
        foreach (var conversion in OtherTargets)
        {
            await Assert.That(conversion(in source, out var result)).IsEqualTo(CastStatus.NotAllowed);
            await Assert.That(result).IsEqualTo(default(XdmItem));
        }

        await Assert
            .That(QNameConversions.ToQName(in source, NoNamespaceContext.Instance, out _))
            .IsEqualTo(CastStatus.NotAllowed);
        await Assert.That(TemporalConversions.TryFormat(in source, out var temporal)).IsFalse();
        await Assert.That(temporal).IsNull();
        await Assert.That(OtherConversions.TryFormat(in source, out var other)).IsFalse();
        await Assert.That(other).IsNull();
    }

    // A source of a sibling type: the conversion of the specification has no such cell, so a row that hands it over is refused by the conversion.
    [Test]
    public async Task TemporalConversion_OfASiblingSource_IsNotAllowed()
    {
        var date = CastFixture.Evaluate("xs:date('2004-10-13')")[0];
        var time = CastFixture.Evaluate("xs:time('12:30:00')")[0];
        var duration = CastFixture.Evaluate("xs:duration('P1Y')")[0];
        var uri = CastFixture.Evaluate("xs:anyURI('urn:x')")[0];

        await Assert.That(TemporalConversions.ToTime(in date, out _)).IsEqualTo(CastStatus.NotAllowed);
        await Assert.That(TemporalConversions.ToDate(in time, out _)).IsEqualTo(CastStatus.NotAllowed);
        await Assert.That(TemporalConversions.ToGYear(in time, out _)).IsEqualTo(CastStatus.NotAllowed);
        await Assert.That(TemporalConversions.ToDate(in duration, out _)).IsEqualTo(CastStatus.NotAllowed);
        await Assert.That(TemporalConversions.ToDuration(in date, out _)).IsEqualTo(CastStatus.NotAllowed);
        await Assert.That(TemporalConversions.ToStringType(in uri, out _)).IsEqualTo(CastStatus.NotAllowed);
        await Assert.That(OtherConversions.ToHexBinary(in date, out _)).IsEqualTo(CastStatus.NotAllowed);
        await Assert.That(OtherConversions.ToAnyUri(in duration, out _)).IsEqualTo(CastStatus.NotAllowed);
    }

    [Test]
    public async Task Items_AnswerOnlyForTheirOwnKind()
    {
        var text = XdmItem.FromString("x");
        var date = CastFixture.Evaluate("xs:date('2004-10-13')")[0];
        var hex = CastFixture.Evaluate("xs:hexBinary('0F')")[0];
        var name = CastFixture.Evaluate("xs:QName('local')")[0];

        await Assert.That(text.TryGetTemporal(out var temporal)).IsFalse();
        await Assert.That(temporal).IsEqualTo(default(NetEvolve.Schemas.Xml.Datatypes.DateTime.XsdDateTime));
        await Assert.That(text.TryGetBinary(out var binary)).IsFalse();
        await Assert.That(binary).IsEqualTo(default(XsdBinary));
        await Assert.That(text.TryGetQName(out var qname)).IsFalse();
        await Assert.That(qname).IsNull();
        await Assert.That(date.TryGetTemporal(out _)).IsTrue();
        await Assert.That(date.TryGetBinary(out _)).IsFalse();
        await Assert.That(hex.TryGetBinary(out _)).IsTrue();
        await Assert.That(hex.TryGetQName(out _)).IsFalse();
        await Assert.That(name.TryGetQName(out _)).IsTrue();
        await Assert.That(name.TryGetTemporal(out _)).IsFalse();
    }

    [Test]
    public async Task Factories_Guard_TheirArguments()
    {
        await Assert.That(() => XdmItem.FromQName(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => XdmItem.FromBinary(default, (BinaryKind)2)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => XdmItem.FromBinary(default, (BinaryKind)(-1))).Throws<ArgumentOutOfRangeException>();
        await Assert.That(XdmItem.FromBinary(default, BinaryKind.HexBinary).Kind).IsEqualTo(XdmItemKind.HexBinary);
        await Assert
            .That(XdmItem.FromBinary(default, BinaryKind.Base64Binary).Kind)
            .IsEqualTo(XdmItemKind.Base64Binary);
    }

    [Test]
    public async Task QNameValue_IsEqualByTheExpandedName_NotByThePrefix()
    {
        var first = new XdmQName(Parse("a:x"), "a");
        var second = new XdmQName(Parse("a:x"), "b");
        var other = new XdmQName(Parse("a:y"), "a");

        await Assert.That(first.Equals(second)).IsTrue();
        await Assert.That(first.Equals((object)second)).IsTrue();
        await Assert.That(first.GetHashCode()).IsEqualTo(second.GetHashCode());
        await Assert.That(first.Equals(other)).IsFalse();
        await Assert.That(first.Equals(Missing())).IsFalse();
        await Assert.That(first.Equals("a:x")).IsFalse();
        await Assert.That(first.ToString()).IsEqualTo("a:x");
        await Assert.That(second.ToString()).IsEqualTo("b:x");
        await Assert.That(new XdmQName(Parse("x"), null).ToString()).IsEqualTo("x");
    }

    [Test]
    [Arguments("a b  c", "a b c")]
    [Arguments(" a", "a")]
    [Arguments("a ", "a")]
    [Arguments("a\tb\nc", "a b c")]
    [Arguments("abc", "abc")]
    [Arguments("", "")]
    public async Task Collapse_NormalizesTheWhitespaceOfTheLexicalForm(string lexical, string expected) =>
        await Assert.That(OtherConversions.Collapse(lexical)).IsEqualTo(expected);

    [Test]
    public async Task Collapse_ReturnsTheSameString_WhenNothingChanges()
    {
        var text = new string('x', 5);

        await Assert.That(OtherConversions.Collapse(text)).IsSameReferenceAs(text);
    }

    private static XdmQName? Missing() => null;

    private static XsdQName Parse(string lexical) => XsdQName.Parse(lexical.AsSpan(), new Fixed("a", "urn:a")).Value;

    private sealed class Fixed(string prefix, string namespaceName) : INamespaceContext
    {
        public bool TryResolvePrefix(ReadOnlySpan<char> name, out string? resolved)
        {
            var match = name.SequenceEqual(prefix.AsSpan());
            resolved = match ? namespaceName : null;
            return match;
        }
    }
}
