namespace NetEvolve.Schemas.XPath.Tests.Unit.Casting.Derived;

using NetEvolve.Schemas.XPath.Casting.Core;
using NetEvolve.Schemas.XPath.Casting.Derived;
using NetEvolve.Schemas.XPath.Types;
using static DerivedFixture;

/// <summary>The registration file of the derived types: one row from each source that casts to the primitive ancestor, for every derived target.</summary>
public class DerivedRegistrationTests
{
    // 12 integer types from the 7 types that cast to xs:integer, and 9 string types from the 22 types that cast to xs:string.
    private const int IntegerRows = 12 * 7;
    private const int StringRows = 9 * 22;

    [Test]
    public async Task RegisterRows_HoldsOneValueDependentRowPerSourceAndDerivedTarget()
    {
        var catalog = new CastingCatalog();
        catalog.Stage(DerivedRegistration.RegisterRows);
        var table = catalog.Table;

        await Assert.That(table.RowCount).IsEqualTo(IntegerRows + StringRows);
        await Assert.That(table.Targets.Count).IsEqualTo(IntegerTypes.Length + StringTypes.Length);
        await Assert
            .That(table.GetCell(XdmTypes.Get("string"), XdmTypes.Get("byte")))
            .IsEqualTo(CastCell.ValueDependent);
        await Assert
            .That(table.GetCell(XdmTypes.Get("date"), XdmTypes.Get("token")))
            .IsEqualTo(CastCell.ValueDependent);
        await Assert.That(table.GetCell(XdmTypes.Get("date"), XdmTypes.Get("byte"))).IsEqualTo(CastCell.NotAllowed);
    }

    [Test]
    public async Task RegisterRows_ASourceRowIsNeverStagedForADerivedType()
    {
        var catalog = new CastingCatalog();
        catalog.Stage(DerivedRegistration.RegisterRows);
        var table = catalog.Table;

        // A derived type as a source has no row of its own: the table casts it as its ancestor, which this table does not hold.
        await Assert.That(table.TryGetRow(XdmTypes.Get("byte"), XdmTypes.Get("short"), out _)).IsFalse();
        await Assert.That(table.TryGetRow(XdmTypes.Get("token"), XdmTypes.Get("NCName"), out _)).IsFalse();
    }
}
