namespace NetEvolve.Schemas.XPath.Tests.Unit.Casting.Core;

using System.Text;
using NetEvolve.Schemas.XPath.Casting.Core;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static CastFixture;

/// <summary>
/// The QT3 test sets <c>xs-double</c> and <c>xs-float</c>, the only <c>xs/*</c> sets for the seven types, as compiled expressions. The cases that
/// need XSD 1.1 (a plus sign before <c>INF</c>, the sign of zero as a distinct value through division) are not part of XPath 2.0, which refers
/// to XML Schema 1.0, and the cases that need the operators <c>div</c> and <c>ne</c> or the function <c>count</c> belong to the packages of those.
/// </summary>
public class XsConstructorSetTests
{
    [Test]
    public async Task XsDouble001_NegativeZero_HasTheStringValueMinusZero() =>
        await Assert.That(EvaluateRendered("xs:double('-0') cast as xs:string")).IsEqualTo("string:-0");

    [Test]
    public async Task XsDouble004_PlusInf_IsRejectedByXsd10() =>
        await Assert.That(Code("xs:double('+INF')")).IsEqualTo("FORG0001");

    [Test]
    public async Task XsDouble005_TheValueThatHangsSomeParsers_IsADouble()
    {
        var result = Evaluate("xs:double('2.2250738585072012e-308')");

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0].Kind).IsEqualTo(XdmItemKind.XsDouble);
        _ = result[0].TryGetDouble(out var value);
        await Assert.That(value).IsEqualTo(2.2250738585072012e-308);
    }

    [Test]
    public async Task XsFloat001_NegativeZero_HasTheStringValueMinusZero() =>
        await Assert.That(EvaluateRendered("xs:float('-0') cast as xs:string")).IsEqualTo("string:-0");

    [Test]
    public async Task XsFloat004_PlusInf_IsRejectedByXsd10() =>
        await Assert.That(Code("xs:float('+INF')")).IsEqualTo("FORG0001");

    [Test]
    public async Task XsFloat005_TheEmptySequence_GivesTheEmptySequence() =>
        await Assert.That(Evaluate("xs:float(())").Count).IsEqualTo(0);

    [Test]
    public async Task XsFloat006_FloatOfDoubleInfinities_HasTheStringValue()
    {
        var result = Evaluate("xs:float(xs:double('-INF')), xs:float(xs:double('INF'))");
        var text = new StringBuilder();
        for (var i = 0; i < result.Count; i++)
        {
            _ = NumberText.TryFormat(result[i], out var item);
            _ = text.Append(i > 0 ? " " : string.Empty).Append(item);
        }

        await Assert.That(text.ToString()).IsEqualTo("-INF INF");
    }
}
