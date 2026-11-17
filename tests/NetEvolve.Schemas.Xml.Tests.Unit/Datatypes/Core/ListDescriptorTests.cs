namespace NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Core;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Contracts.Fakes;
using NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Core.Fakes;

public class ListDescriptorTests
{
    private static AtomicDescriptor<int> Digits() => new("digits", XsdVersion.V11, new FakeDigitsMapping());

    private static ListDescriptor DigitsList() => new("digitsList", Digits());

    [Test]
    public async Task Shape_IsAListOfTheItemType()
    {
        var list = DigitsList();

        await Assert.That(list.Variety).IsEqualTo(DatatypeVariety.List);
        await Assert.That(list.ItemType.Name).IsEqualTo("digits");
        await Assert.That(list.LexicalMode).IsEqualTo(XsdVersion.V11);
        await Assert.That(list.IsOrdered).IsFalse();
        await Assert.That(list.Whitespace).IsEqualTo(WhitespaceMode.Collapse);
        await Assert.That(list.Facets.Count).IsEqualTo(0);
    }

    [Test]
    [Arguments("1 22 333", 3)]
    [Arguments("  1\t22\n\r333  ", 3)]
    [Arguments("7", 1)]
    [Arguments("", 0)]
    [Arguments(" \t ", 0)]
    public async Task Parse_SplitsOnWhitespaceAndCountsItems(string lexical, int count)
    {
        var result = DigitsList().Parse(lexical);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Value.ItemCount).IsEqualTo(count);
        await Assert.That(result.Value.Range).IsEqualTo(new TextRange(0, lexical.Length));
        await Assert.That(DigitsList().Validate(lexical).IsValid).IsTrue();
    }

    [Test]
    public async Task Validate_FailingItem_ReportsTheItemIndexAndTheOffsetInTheInput()
    {
        var result = DigitsList().Validate("12  34x 56", out var itemIndex);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(itemIndex).IsEqualTo(1);
        await Assert.That(result.Error).IsEqualTo(DatatypeError.InvalidListItem);
        await Assert.That(result.Offset).IsEqualTo(6);
    }

    [Test]
    public async Task Validate_FailingItemFacet_ReportsTheItemFacet()
    {
        var item = new AtomicDescriptor<int>("small", Digits(), null, [new FakeMaxInclusiveFacet(9)]);

        var result = new ListDescriptor("smallList", item).Validate("1 2 30", out var itemIndex);

        await Assert.That(itemIndex).IsEqualTo(2);
        await Assert.That(result.Error).IsEqualTo(DatatypeError.InvalidListItem);
        await Assert.That(result.FailedFacet).IsEqualTo(FacetKind.MaxInclusive);
        await Assert.That(result.Offset).IsEqualTo(4);
    }

    [Test]
    public async Task Validate_Valid_ReportsNoFailingItem()
    {
        var result = DigitsList().Validate("1 2", out var itemIndex);

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(itemIndex).IsEqualTo(-1);
    }

    [Test]
    public async Task Parse_FailingItem_ReportsTheItemErrorAndOffset()
    {
        var result = DigitsList().Parse("1 x", out var itemIndex);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Error).IsEqualTo(DatatypeError.InvalidListItem);
        await Assert.That(result.ErrorOffset).IsEqualTo(2);
        await Assert.That(itemIndex).IsEqualTo(1);
    }

    [Test]
    public async Task Cursor_YieldsTheItemRangesInOrder()
    {
        const string lexical = "  ab\t c  d ";
        var cursor = new XsdListCursor(new TextRange(0, lexical.Length));
        var items = new List<string>();

        while (cursor.TryNext(lexical.AsSpan(), out var item))
        {
            items.Add(item.Slice(lexical.AsSpan()).ToString());
        }

        await Assert.That(items.Count).IsEqualTo(3);
        await Assert.That(items[0]).IsEqualTo("ab");
        await Assert.That(items[1]).IsEqualTo("c");
        await Assert.That(items[2]).IsEqualTo("d");
    }

    [Test]
    public async Task Constructor_NullItemType_Throws() =>
        await Assert.That(() => new ListDescriptor("l", null!)).Throws<ArgumentNullException>();
}
