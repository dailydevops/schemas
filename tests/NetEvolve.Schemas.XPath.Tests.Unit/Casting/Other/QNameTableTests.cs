namespace NetEvolve.Schemas.XPath.Tests.Unit.Casting.Other;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Casting.Core;
using NetEvolve.Schemas.XPath.Casting.Other;
using NetEvolve.Schemas.XPath.Casting.Temporal;
using NetEvolve.Schemas.XPath.Tests.Unit.Casting.Core;
using NetEvolve.Schemas.XPath.Tests.Unit.Casting.Temporal;
using static NetEvolve.Schemas.XPath.Tests.Unit.Casting.Temporal.CastMatrix;

/// <summary>Every cell of the casting table for <c>xs:QName</c>, and the table as a whole: every cell that is not N has a row and every other pair has none.</summary>
public class QNameTableTests
{
    private const int UntypedAtomic = 1;
    private const int QName = 21;

    public static IEnumerable<Func<(int Source, int Target)>> QNamePairs() => Pairs(IsQName, static _ => false);

    [Test]
    [MethodDataSource(nameof(QNamePairs))]
    public async Task Cell_MatchesTheSpecificationTable(int source, int target)
    {
        var table = CastingCatalog.Default.Table;
        var expected = Expected(source, target);

        var found = table.TryGetRow(Type(source), Type(target), out var row);

        await Assert
            .That(found)
            .IsEqualTo(expected != CastCell.NotAllowed)
            .Because($"{Names[source]} to {Names[target]}");
        await Assert.That(table.GetCell(Type(source), Type(target))).IsEqualTo(expected);
        if (found)
        {
            await Assert.That(row!.Cell).IsEqualTo(expected);
        }
    }

    [Test]
    [MethodDataSource(nameof(QNamePairs))]
    public async Task Cell_BehavesAsTheTableSays(int source, int target)
    {
        var expression = Samples[source] + " cast as xs:" + Names[target];
        var cell = Cell(source, target);

        if (cell == 'N')
        {
            await Assert.That(CastFixture.Code(expression)).IsEqualTo("XPTY0004");
            await Assert.That(CastShow.Castable(Samples[source], Names[target])).IsFalse();
        }
        else if (cell == 'Y')
        {
            await Assert.That(CastShow.KindOf(expression)).IsEqualTo(Kinds[target]);
            await Assert.That(CastShow.Castable(Samples[source], Names[target])).IsTrue();
        }
        else if (source == UntypedAtomic)
        {
            // M, but XPath 2.0 casts a lexical QName only from a string literal, which a constructor call is not.
            await Assert.That(CastFixture.Code("xs:untypedAtomic('local') cast as xs:QName")).IsEqualTo("XPTY0004");
        }
        else
        {
            await Assert.That(CastShow.KindOf("'local' cast as xs:QName")).IsEqualTo(Kinds[target]);
        }
    }

    [Test]
    public async Task EveryType_IsATarget_AndHasItsConstructor()
    {
        var table = CastingCatalog.Default.Table;

        await Assert.That(table.IsTarget(Type(QName))).IsTrue();
        await Assert.That(CastShow.KindOf(Samples[QName])).IsEqualTo(Kinds[QName]);
        await Assert.That(CastFixture.Evaluate(Samples[QName])[0].TypeId).IsEqualTo(Type(QName));
    }

    // The cells of the whole matrix are exactly the rows of the table: a pair that is not N has a row and a pair that is N has none.
    [Test]
    public async Task Table_HoldsExactlyTheRowsOfTheMatrix()
    {
        var table = MatrixTable();

        await Assert.That(table.RowCount).IsEqualTo(RowCount());
        await Assert.That(table.Targets.Count).IsEqualTo(Names.Length);
        for (var s = 0; s < Names.Length; s++)
        {
            for (var t = 0; t < Names.Length; t++)
            {
                await Assert
                    .That(table.GetCell(Type(s), Type(t)))
                    .IsEqualTo(Expected(s, t))
                    .Because($"{Names[s]} to {Names[t]}");
            }
        }
    }

    // xs:NOTATION is abstract: it is no target of a cast and has no constructor function.
    [Test]
    [Arguments("'x' cast as xs:NOTATION", "XPST0080")]
    [Arguments("'x' castable as xs:NOTATION", "XPST0080")]
    [Arguments("'x' cast as xs:NOTATION?", "XPST0080")]
    [Arguments("xs:QName('local') cast as xs:NOTATION", "XPST0080")]
    [Arguments("'x' cast as xs:anyAtomicType", "XPST0080")]
    [Arguments("xs:NOTATION('local')", "XPST0017")]
    [Arguments("xs:anyAtomicType('local')", "XPST0017")]
    public async Task NotationAndAnyAtomicType_AreNoTargets(string expression, string code) =>
        await Assert.That(CastFixture.Code(expression)).IsEqualTo(code);

    [Test]
    public async Task Table_HasNoRowForNotation()
    {
        var table = MatrixTable();
        var notation = Type(QName);

        await Assert.That(table.Targets.Count).IsEqualTo(Names.Length);
        await Assert.That(table.IsTarget(NetEvolve.Schemas.XPath.Types.XdmTypes.Get("NOTATION"))).IsFalse();
        await Assert.That(notation).IsNotEqualTo(NetEvolve.Schemas.XPath.Types.XdmTypes.Get("NOTATION"));
    }

    // The table of the types of the matrix only: the default table also holds the rows of the derived types (XP-23), which are not part of it.
    private static CastingTable MatrixTable()
    {
        var catalog = new CastingCatalog();
        catalog.Stage(CastingRegistration.RegisterRows);
        catalog.Stage(TemporalRegistration.RegisterRows);
        catalog.Stage(OtherRegistration.RegisterRows);
        return catalog.Table;
    }
}
