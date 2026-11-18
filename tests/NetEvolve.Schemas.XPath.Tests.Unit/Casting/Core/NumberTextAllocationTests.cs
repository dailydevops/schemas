namespace NetEvolve.Schemas.XPath.Tests.Unit.Casting.Core;

using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.Xml.Datatypes.Floating;
using NetEvolve.Schemas.XPath.Casting.Core;

/// <summary>The plain notation of a double or float adds nothing to the canonical form but the result string: the digits are moved in a stack buffer.</summary>
public class NumberTextAllocationTests
{
    // A string of this length: 8 bytes header, 8 bytes method table, 4 bytes length, and the characters with their terminator, in 8 bytes steps.
    private static long StringBytes(int length) => ((8 + 8 + 4 + (2 * (length + 1))) + 7) / 8 * 8;

    [Test]
    [Arguments(0.5, false)]
    [Arguments(123456.789, false)]
    [Arguments(-0.0000015, false)]
    [Arguments(100.0, false)]
    [Arguments(0.1, true)]
    [Arguments(-12345.5, true)]
    public async Task FormatFloating_InPlainNotation_AllocatesTheCanonicalFormAndTheResultOnly(
        double value,
        bool isFloat
    )
    {
        var result = NumberText.FormatFloating(value, isFloat);
        var canonicalBytes = AllocationAssert.Measure(() =>
            _ = isFloat ? XsdFloatingPoint.ToCanonicalString((float)value) : XsdFloatingPoint.ToCanonicalString(value)
        );

        AllocationAssert.AtMost(canonicalBytes + StringBytes(result.Length), () =>
            _ = NumberText.FormatFloating(value, isFloat)
        );

        await Assert.That(result).IsNotEmpty();
    }
}
