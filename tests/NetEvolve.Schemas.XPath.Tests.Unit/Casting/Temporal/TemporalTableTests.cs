namespace NetEvolve.Schemas.XPath.Tests.Unit.Casting.Temporal;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Casting.Core;
using NetEvolve.Schemas.XPath.Tests.Unit.Casting.Core;
using static CastMatrix;

/// <summary>Every cell of the casting table for the date, time, Gregorian, and duration types: allowed, value dependent, or XPTY0004.</summary>
public class TemporalTableTests
{
    public static IEnumerable<Func<(int Source, int Target)>> TemporalPairs() => Pairs(IsTemporal, IsOther);

    [Test]
    [MethodDataSource(nameof(TemporalPairs))]
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
    [MethodDataSource(nameof(TemporalPairs))]
    public async Task Cell_BehavesAsTheTableSays(int source, int target)
    {
        var expression = Samples[source] + " cast as xs:" + Names[target];
        var cell = Cell(source, target);

        if (cell == 'N')
        {
            // A target that is not a table target is rejected while compiling; a pair without a row is XPTY0004 at run time.
            await Assert.That(CastFixture.Code(expression)).IsEqualTo("XPTY0004");
            await Assert.That(CastShow.Castable(Samples[source], Names[target])).IsFalse();
        }
        else if (cell == 'Y')
        {
            await Assert.That(CastShow.KindOf(expression)).IsEqualTo(Kinds[target]);
            await Assert.That(CastShow.Castable(Samples[source], Names[target])).IsTrue();
        }
        else
        {
            // M: a string or untypedAtomic source; the value decides.
            var literal = source == 0 ? "'" + Lexicals[target] + "'" : "xs:untypedAtomic('" + Lexicals[target] + "')";
            var valid = literal + " cast as xs:" + Names[target];
            await Assert.That(CastShow.KindOf(valid)).IsEqualTo(Kinds[target]);
        }
    }

    [Test]
    [Arguments(7)]
    [Arguments(8)]
    [Arguments(9)]
    [Arguments(10)]
    [Arguments(11)]
    [Arguments(12)]
    [Arguments(13)]
    [Arguments(14)]
    [Arguments(15)]
    [Arguments(16)]
    [Arguments(17)]
    public async Task EveryTemporalType_IsATarget_AndHasItsConstructor(int index)
    {
        var table = CastingCatalog.Default.Table;

        await Assert.That(table.IsTarget(Type(index))).IsTrue();
        await Assert.That(table.Targets.Contains(Type(index))).IsTrue();
        await Assert.That(CastShow.KindOf(Samples[index])).IsEqualTo(Kinds[index]);
        await Assert.That(CastFixture.Evaluate(Samples[index])[0].TypeId).IsEqualTo(Type(index));
    }
}
