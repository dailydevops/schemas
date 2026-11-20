namespace NetEvolve.Schemas.XPath.Tests.Unit.Casting.Core;

using NetEvolve.Schemas.XPath.Types;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static CastFixture;

/// <summary>An integer beyond 64 bits is an item of the decimal family, so the cast to <c>xs:decimal</c> has to replace its integer annotation (#744).</summary>
public class WideIntegerToDecimalTests
{
    [Test]
    [Arguments("(xs:integer('9223372036854775807') + 1) cast as xs:decimal", "decimal:9223372036854775808")]
    [Arguments("(xs:integer('-9223372036854775808') - 1) cast as xs:decimal", "decimal:-9223372036854775809")]
    [Arguments("xs:unsignedLong('18446744073709551615') cast as xs:decimal", "decimal:18446744073709551615")]
    [Arguments("xs:nonNegativeInteger('18446744073709551616') cast as xs:decimal", "decimal:18446744073709551616")]
    public async Task Cast_OfAnIntegerBeyond64Bits_IsAnXsDecimal(string expression, string expected)
    {
        var result = Evaluate(expression)[0];

        await Assert.That(Render(in result)).IsEqualTo(expected);
        await Assert.That(result.TypeId).IsEqualTo(XdmTypes.Get("decimal"));
    }

    [Test]
    public async Task Cast_OfAnInlineInteger_IsStillAnXsDecimal()
    {
        var result = Evaluate("xs:integer('5') cast as xs:decimal")[0];

        await Assert.That(result.TypeId).IsEqualTo(XdmTypes.Get("decimal"));
    }
}
