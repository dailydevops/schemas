namespace NetEvolve.Schemas.XPath.Tests.Unit.Casting.Core;

using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.XPath.Casting.Core;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>Casts between the value-type families allocate nothing: the item is a tagged struct, and the table lookup is a dictionary read.</summary>
public class CastAllocationTests
{
    private static async Task AssertNoAllocation(XdmItem source, XdmTypeId target, string expected)
    {
        var table = CastingCatalog.Default.Table;
        var status = CastStatus.NotAllowed;
        var result = default(XdmItem);

        AllocationAssert.NoAllocation(() => status = table.TryCast(source, target, out result));

        await Assert.That(status).IsEqualTo(CastStatus.Ok);
        await Assert.That(CastFixture.Render(result)).IsEqualTo(expected);
    }

    [Test]
    public async Task IntegerToDouble_AllocatesNothing() =>
        await AssertNoAllocation(XdmItem.FromInteger(12), XdmTypeId.XsDouble, "double:12");

    [Test]
    public async Task DoubleToInteger_AllocatesNothing() =>
        await AssertNoAllocation(XdmItem.FromDouble(12.9), XdmTypeId.XsInteger, "integer:12");

    [Test]
    public async Task DoubleToBoolean_AllocatesNothing() =>
        await AssertNoAllocation(XdmItem.FromDouble(0.5), XdmTypeId.Boolean, "boolean:true");

    [Test]
    public async Task BooleanToInteger_AllocatesNothing() =>
        await AssertNoAllocation(XdmItem.FromBoolean(true), XdmTypeId.XsInteger, "integer:1");

    [Test]
    public async Task FloatToDouble_AllocatesNothing() =>
        await AssertNoAllocation(XdmItem.FromFloat(1.5f), XdmTypeId.XsDouble, "double:1.5");

    [Test]
    public async Task StringToString_AllocatesNothing() =>
        await AssertNoAllocation(XdmItem.FromString("a"), XdmTypeId.XsString, "string:a");
}
