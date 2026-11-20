namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Annotations;

using System;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;
using NetEvolve.Schemas.Xsd.Validation.Annotations;

public class AnnotationTableTests
{
    private static AnnotationRecord Element(int ordinal, XsdValidity validity = XsdValidity.Valid) =>
        new(ordinal, -1, SchemaFixture.Simple("integer"), validity, false);

    private static AnnotationRecord Attribute(int ordinal, int index) =>
        new(ordinal, index, SchemaFixture.Simple("string"), XsdValidity.Invalid, false);

    [Test]
    public async Task Empty_HoldsNothing()
    {
        var table = new AnnotationTable();

        await Assert.That(table.ElementOrdinalLimit).IsEqualTo(0);
        await Assert.That(table.AttributeCount).IsEqualTo(0);
        await Assert.That(table.TryGetElement(1, out _)).IsFalse();
        await Assert.That(table.TryGetAttribute(1, 0, out _)).IsFalse();
    }

    [Test]
    public async Task Element_IsFoundByOrdinal_AndGapsAreNotAnnotated()
    {
        var table = new AnnotationTable();
        table.Element(Element(3, XsdValidity.Invalid));

        var found = table.TryGetElement(3, out var record);

        await Assert.That(found).IsTrue();
        await Assert.That(record.Ordinal).IsEqualTo(3);
        await Assert.That(record.Validity).IsEqualTo(XsdValidity.Invalid);
        await Assert.That(record.IsAttribute).IsFalse();
        await Assert.That(table.ElementOrdinalLimit).IsEqualTo(3);
        await Assert.That(table.TryGetElement(1, out _)).IsFalse();
        await Assert.That(table.TryGetElement(2, out _)).IsFalse();
        await Assert.That(table.TryGetElement(4, out _)).IsFalse();
        await Assert.That(table.TryGetElement(0, out _)).IsFalse();
        await Assert.That(table.TryGetElement(-1, out _)).IsFalse();
    }

    [Test]
    public async Task Element_RecordsInEndOrder_AreFoundByOrdinal()
    {
        var table = new AnnotationTable();
        table.Element(Element(3));
        table.Element(Element(2));
        table.Element(Element(1));

        await Assert.That(table.TryGetElement(1, out _)).IsTrue();
        await Assert.That(table.TryGetElement(2, out _)).IsTrue();
        await Assert.That(table.TryGetElement(3, out _)).IsTrue();
        await Assert.That(table.ElementOrdinalLimit).IsEqualTo(3);
    }

    [Test]
    public async Task Element_ManyRecords_GrowTheTable()
    {
        var table = new AnnotationTable();
        for (var i = 1; i <= 100; i++)
        {
            table.Element(Element(i));
        }

        var all = true;
        for (var i = 1; i <= 100; i++)
        {
            all &= table.TryGetElement(i, out var record) && record.Ordinal == i;
        }

        await Assert.That(all).IsTrue();
        await Assert.That(table.ElementOrdinalLimit).IsEqualTo(100);
    }

    [Test]
    public async Task Attribute_IsFoundByOrdinalAndIndex()
    {
        var table = new AnnotationTable();
        table.Attribute(Attribute(1, 0));
        table.Attribute(Attribute(1, 1));
        table.Attribute(Attribute(3, 0));

        var found = table.TryGetAttribute(1, 1, out var record);

        await Assert.That(found).IsTrue();
        await Assert.That(record.AttributeIndex).IsEqualTo(1);
        await Assert.That(record.IsAttribute).IsTrue();
        await Assert.That(table.TryGetAttribute(1, 0, out _)).IsTrue();
        await Assert.That(table.TryGetAttribute(3, 0, out _)).IsTrue();
        await Assert.That(table.TryGetAttribute(1, 2, out _)).IsFalse();
        await Assert.That(table.TryGetAttribute(2, 0, out _)).IsFalse();
        await Assert.That(table.TryGetAttribute(4, 0, out _)).IsFalse();
        await Assert.That(table.TryGetAttribute(0, 0, out _)).IsFalse();
        await Assert.That(table.AttributeCount).IsEqualTo(3);
    }

    [Test]
    public async Task Attribute_ManyRecords_GrowTheTable_AndEveryOneIsFound()
    {
        var table = new AnnotationTable();
        for (var owner = 1; owner <= 40; owner++)
        {
            for (var index = 0; index < 3; index++)
            {
                table.Attribute(Attribute(owner, index));
            }
        }

        var all = true;
        for (var owner = 1; owner <= 40; owner++)
        {
            for (var index = 0; index < 3; index++)
            {
                all &= table.TryGetAttribute(owner, index, out _);
            }
        }

        await Assert.That(all).IsTrue();
        await Assert.That(table.AttributeCount).IsEqualTo(120);
    }

    [Test]
    public async Task Element_OrdinalBelowOne_Throws() =>
        await Assert.That(() => new AnnotationTable().Element(Element(0))).Throws<ArgumentOutOfRangeException>();

    [Test]
    public async Task Attribute_OrdinalBelowOne_Throws() =>
        await Assert.That(() => new AnnotationTable().Attribute(Attribute(0, 0))).Throws<ArgumentOutOfRangeException>();

    [Test]
    public async Task Attribute_NegativeIndex_Throws() =>
        await Assert
            .That(() => new AnnotationTable().Attribute(Attribute(1, -1)))
            .Throws<ArgumentOutOfRangeException>();
}
