namespace NetEvolve.Schemas.XPath.Tests.Unit.Casting.Other;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Casting.Core;
using NetEvolve.Schemas.XPath.Tests.Unit.Casting.Core;
using NetEvolve.Schemas.XPath.Tests.Unit.Casting.Temporal;
using static NetEvolve.Schemas.XPath.Tests.Unit.Casting.Temporal.CastMatrix;

/// <summary>Every cell of the casting table for <c>xs:hexBinary</c>, <c>xs:base64Binary</c>, and <c>xs:anyURI</c>: allowed, value dependent, or XPTY0004.</summary>
public class OtherTableTests
{
    public static IEnumerable<Func<(int Source, int Target)>> BinaryAndUriPairs() => Pairs(IsBinaryOrUri, IsQName);

    [Test]
    [MethodDataSource(nameof(BinaryAndUriPairs))]
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
    [MethodDataSource(nameof(BinaryAndUriPairs))]
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
        else
        {
            var literal = source == 0 ? "'" + Lexicals[target] + "'" : "xs:untypedAtomic('" + Lexicals[target] + "')";
            await Assert.That(CastShow.KindOf(literal + " cast as xs:" + Names[target])).IsEqualTo(Kinds[target]);
        }
    }

    [Test]
    [Arguments(18)]
    [Arguments(19)]
    [Arguments(20)]
    public async Task EveryType_IsATarget_AndHasItsConstructor(int index)
    {
        var table = CastingCatalog.Default.Table;

        await Assert.That(table.IsTarget(Type(index))).IsTrue();
        await Assert.That(CastShow.KindOf(Samples[index])).IsEqualTo(Kinds[index]);
        await Assert.That(CastFixture.Evaluate(Samples[index])[0].TypeId).IsEqualTo(Type(index));
    }
}
