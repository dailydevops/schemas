namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Binding;

using System.Threading.Tasks;
using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The cast of an <c>xs:untypedAtomic</c> operand to a double, float, boolean, or integer allocates no managed memory in steady
/// state, also when the lexical form carries whitespace that the collapse step removes (hot-path rule of #123). A decimal item is
/// boxed by <see cref="XdmItem"/>, a documented limitation; the cast adds nothing on top of that box.
/// </summary>
public class UntypedAtomicCastAllocationTests
{
    private const int Warmups = 50;

    // XPathItemType is internal, so the targets travel as their ordinal.
    private const int Boolean = (int)XPathItemType.Boolean;
    private const int Integer = (int)XPathItemType.Integer;
    private const int Float = (int)XPathItemType.Float;
    private const int Double = (int)XPathItemType.Double;

    [Test]
    [Arguments("2.5", Double)]
    [Arguments("1E3", Double)]
    [Arguments("  2.5\t", Double)]
    [Arguments("\n2.5\r\n", Double)]
    [Arguments("2.5", Float)]
    [Arguments(" 2.5 ", Float)]
    [Arguments("true", Boolean)]
    [Arguments(" 1 ", Boolean)]
    [Arguments("42", Integer)]
    [Arguments("-7", Integer)]
    [Arguments("\t42 ", Integer)]
    public async Task Cast_ToDoubleFloatBooleanOrInteger_AllocatesNothing(string lexical, int type)
    {
        var target = (XPathItemType)type;
        var item = UntypedAtomicCast.Cast(lexical, target);

        AllocationAssert.NoAllocation(() => _ = UntypedAtomicCast.Cast(lexical, target), warmups: Warmups);

        await Assert.That(item.IsNode).IsFalse();
    }

    [Test]
    [Arguments("2.5", "  2.5  ")]
    [Arguments("100", "\t100\n")]
    public async Task Cast_ToDecimal_AddsNothingToTheBoxOfTheItem(string plain, string padded)
    {
        var item = UntypedAtomicCast.Cast(padded, XPathItemType.Decimal);
        var boxBytes = AllocationAssert.Measure(
            () => _ = UntypedAtomicCast.Cast(plain, XPathItemType.Decimal),
            warmups: Warmups
        );

        AllocationAssert.AtMost(
            boxBytes,
            () => _ = UntypedAtomicCast.Cast(padded, XPathItemType.Decimal),
            warmups: Warmups
        );

        await Assert.That(item.TryGetDecimal(out _)).IsTrue();
    }

    [Test]
    public async Task Cast_WhitespaceInsideTheValue_RaisesFORG0001()
    {
        var failure = await Assert
            .That(() => UntypedAtomicCast.Cast("1 5", XPathItemType.Integer))
            .Throws<XPathException>();

        await Assert.That(failure!.Diagnostic.Code).IsEqualTo("FORG0001");
    }
}
