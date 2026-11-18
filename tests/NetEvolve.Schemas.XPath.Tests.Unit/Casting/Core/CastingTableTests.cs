namespace NetEvolve.Schemas.XPath.Tests.Unit.Casting.Core;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Casting.Core;
using NetEvolve.Schemas.XPath.Xdm.Values;

public class CastingTableTests
{
    private static readonly XdmTypeId[] Types =
    [
        XdmTypeId.XsString,
        XdmTypeId.UntypedAtomic,
        XdmTypeId.XsFloat,
        XdmTypeId.XsDouble,
        XdmTypeId.XsDecimal,
        XdmTypeId.XsInteger,
        XdmTypeId.Boolean,
    ];

    private static readonly string[] Names =
    [
        "string",
        "untypedAtomic",
        "float",
        "double",
        "decimal",
        "integer",
        "boolean",
    ];

    // XQuery 1.0 and XPath 2.0 Functions and Operators, 17.1: Y always succeeds, M depends on the value. There is no N among these types.
    // The rows are the source types, the columns the target types in the order of Types.
    private static readonly string[] Matrix =
    [
        "YYMMMMM", // xs:string
        "YYMMMMM", // xs:untypedAtomic
        "YYYYMMY", // xs:float
        "YYYYMMY", // xs:double
        "YYYYYYY", // xs:decimal
        "YYYYYYY", // xs:integer
        "YYYYYYY", // xs:boolean
    ];

    public static IEnumerable<Func<(int Source, int Target)>> EveryCell()
    {
        for (var s = 0; s < Types.Length; s++)
        {
            for (var t = 0; t < Types.Length; t++)
            {
                var pair = (s, t);
                yield return () => pair;
            }
        }
    }

    [Test]
    [MethodDataSource(nameof(EveryCell))]
    public async Task Cell_MatchesTheSpecificationTable(int source, int target)
    {
        var table = CastingCatalog.Default.Table;
        var expected = Matrix[source][target] == 'Y' ? CastCell.Always : CastCell.ValueDependent;

        var found = table.TryGetRow(Types[source], Types[target], out var row);

        await Assert.That(found).IsTrue().Because($"{Names[source]} to {Names[target]} has a row");
        await Assert.That(row!.Cell).IsEqualTo(expected).Because($"{Names[source]} to {Names[target]}");
        await Assert.That(table.GetCell(Types[source], Types[target])).IsEqualTo(expected);
    }

    [Test]
    public async Task Table_HoldsExactlyTheRowsOfTheSevenTypes()
    {
        var table = CastingCatalog.Default.Table;

        await Assert.That(table.RowCount).IsEqualTo(49);
        await Assert.That(table.Targets.Count).IsEqualTo(7);
        foreach (var type in Types)
        {
            await Assert.That(table.Targets.Contains(type)).IsTrue();
        }
    }

    [Test]
    [Arguments(0)]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(3)]
    [Arguments(4)]
    [Arguments(5)]
    [Arguments(6)]
    public async Task NoCell_OfTheSevenTypes_IsNotAllowed(int source)
    {
        var table = CastingCatalog.Default.Table;

        for (var t = 0; t < Types.Length; t++)
        {
            await Assert.That(table.GetCell(Types[source], Types[t])).IsNotEqualTo(CastCell.NotAllowed);
        }
    }

    [Test]
    [Arguments(12)] // dateTime
    [Arguments(13)] // date
    [Arguments(14)] // time
    [Arguments(9)] // duration
    [Arguments(10)] // yearMonthDuration
    [Arguments(11)] // dayTimeDuration
    [Arguments(15)] // anyURI
    public async Task SourceWithoutRow_ToANumericOrBooleanTarget_IsNotAllowed(int source)
    {
        var table = CastingCatalog.Default.Table;
        var sourceType = new XdmTypeId(source);

        foreach (
            var target in (XdmTypeId[])
                [XdmTypeId.XsFloat, XdmTypeId.XsDouble, XdmTypeId.XsDecimal, XdmTypeId.XsInteger, XdmTypeId.Boolean]
        )
        {
            await Assert.That(table.TryGetRow(sourceType, target, out _)).IsFalse();
            await Assert.That(table.GetCell(sourceType, target)).IsEqualTo(CastCell.NotAllowed);
        }
    }

    [Test]
    public async Task Catalog_StagedRowsAreAddedWithoutEditingTheCoreRows()
    {
        var catalog = new CastingCatalog();
        catalog.Stage(CastingRegistration.RegisterRows);
        catalog.Stage(static builder => builder.Add(DateRow()));

        var table = catalog.Table;

        await Assert.That(table.RowCount).IsEqualTo(50);
        await Assert.That(table.TryGetRow(XdmTypeId.XsString, XdmTypeId.Date, out var row)).IsTrue();
        await Assert.That(row!.Cell).IsEqualTo(CastCell.ValueDependent);
        await Assert.That(table.Targets.Contains(XdmTypeId.Date)).IsTrue();
    }

    [Test]
    public async Task Catalog_StagedRow_RunsItsConversion()
    {
        var catalog = new CastingCatalog();
        catalog.Stage(static builder => builder.Add(DateRow()));

        var status = catalog.Table.TryCast(XdmItem.FromString("x"), XdmTypeId.Date, out var result);

        await Assert.That(status).IsEqualTo(CastStatus.Ok);
        await Assert.That(CastFixture.Render(result)).IsEqualTo("string:converted");
    }

    [Test]
    public async Task Catalog_StagedAfterTheFreeze_Throws()
    {
        var catalog = new CastingCatalog();
        _ = catalog.Table;

        await Assert.That(() => catalog.Stage(static _ => { })).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Catalog_TwoRowsForOnePair_ThrowOnTheFreeze()
    {
        var catalog = new CastingCatalog();
        catalog.Stage(static builder => builder.Add(DateRow()));
        catalog.Stage(static builder => builder.Add(DateRow()));

        await Assert.That(() => catalog.Table).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Catalog_Guards_RejectNull()
    {
        var catalog = new CastingCatalog();
        var builder = new CastingTableBuilder();

        await Assert.That(() => catalog.Stage(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => builder.Add(null!)).Throws<ArgumentNullException>();
        await Assert
            .That(() => new CastRow(XdmTypeId.XsString, XdmTypeId.Date, CastCell.Always, null!))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => CastingCatalog.Default.Table.TryCast(XdmItem.FromString("x"), default, out _))
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Table_Constructor_RejectsNull()
    {
        await Assert.That(() => new CastingTable(null!, [])).Throws<ArgumentNullException>();
        await Assert.That(() => new CastingTable(new(), null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Row_NotAllowedCell_IsRejected() =>
        await Assert
            .That(() => new CastRow(XdmTypeId.XsString, XdmTypeId.Date, CastCell.NotAllowed, Convert))
            .Throws<ArgumentOutOfRangeException>();

    [Test]
    public async Task Default_IsFrozen_AndStagedByTheRegistrationFile()
    {
        var first = CastingCatalog.Default.Table;

        await Assert.That(CastingCatalog.Default.Table).IsSameReferenceAs(first);
        await Assert.That(() => CastingCatalog.Default.Stage(static _ => { })).Throws<InvalidOperationException>();
    }

    private static CastRow DateRow() => new(XdmTypeId.XsString, XdmTypeId.Date, CastCell.ValueDependent, Convert);

    private static CastStatus Convert(in XdmItem source, out XdmItem result)
    {
        result = XdmItem.FromString("converted");
        return CastStatus.Ok;
    }
}
