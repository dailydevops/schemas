namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Arithmetic;

using NetEvolve.Schemas.XPath.Errors;
using static ArithmeticFixture;

public class ArithmeticErrorTests
{
    private static async Task AssertRaises(string left, string op, string right, XPathErrorCode code)
    {
        var error = Raised(() => Run(left, op, right));

        await Assert.That(error).IsNotNull();
        await Assert.That(error!.Diagnostic.QName).IsEqualTo(code);
        await Assert.That(error.Diagnostic.Kind).IsEqualTo(XPathErrorKind.Function);
    }

    [Test]
    [Arguments("i:1", "div", "i:0")]
    [Arguments("i:1", "idiv", "i:0")]
    [Arguments("i:1", "mod", "i:0")]
    [Arguments("w:100000000000000000000", "mod", "i:0")]
    [Arguments("w:100000000000000000000", "idiv", "i:0")]
    [Arguments("d:1.5", "div", "d:0")]
    [Arguments("d:1.5", "idiv", "d:0.0")]
    [Arguments("d:1.5", "mod", "d:0")]
    [Arguments("i:1", "div", "d:0")]
    [Arguments("d:1", "mod", "i:0")]
    [Arguments("w:100000000000000000000", "div", "i:0")]
    public async Task DivisionByZero_ForIntegerAndDecimal_RaisesFoar0001(string left, string op, string right) =>
        await AssertRaises(left, op, right, XPathErrorCodes.FOAR0001);

    [Test]
    [Arguments("e:1", "idiv", "e:0")]
    [Arguments("e:1", "idiv", "e:-0")]
    [Arguments("i:1", "idiv", "e:0")]
    [Arguments("f:1", "idiv", "f:0")]
    [Arguments("e:NaN", "idiv", "e:0")]
    [Arguments("e:INF", "idiv", "e:0")]
    public async Task Idiv_ByZero_RaisesFoar0001ForEveryType(string left, string op, string right) =>
        await AssertRaises(left, op, right, XPathErrorCodes.FOAR0001);

    [Test]
    [Arguments("e:NaN", "idiv", "i:1")]
    [Arguments("i:1", "idiv", "e:NaN")]
    [Arguments("e:INF", "idiv", "i:1")]
    [Arguments("e:-INF", "idiv", "i:1")]
    [Arguments("f:NaN", "idiv", "f:1")]
    [Arguments("f:INF", "idiv", "f:1")]
    [Arguments("e:INF", "idiv", "e:INF")]
    [Arguments("e:1E308", "idiv", "e:1E-308")]
    public async Task Idiv_WithNaNOrAnInfiniteDividend_RaisesFoar0002(string left, string op, string right) =>
        await AssertRaises(left, op, right, XPathErrorCodes.FOAR0002);

    [Test]
    [Arguments("e:5", "idiv", "e:INF", "integer:0")]
    [Arguments("e:-5", "idiv", "e:-INF", "integer:0")]
    [Arguments("i:5", "idiv", "e:INF", "integer:0")]
    [Arguments("f:5", "idiv", "f:INF", "integer:0")]
    public async Task Idiv_OfAFiniteDividendByAnInfinity_IsZero(
        string left,
        string op,
        string right,
        string expected
    ) => await Assert.That(Describe(Run(left, op, right))).IsEqualTo(expected);

    [Test]
    [Arguments("e:1", "div", "e:0", "double:INF")]
    [Arguments("e:-1", "div", "e:0", "double:-INF")]
    [Arguments("e:1", "div", "e:-0", "double:-INF")]
    [Arguments("e:0", "div", "e:0", "double:NaN")]
    [Arguments("e:NaN", "div", "e:0", "double:NaN")]
    [Arguments("i:1", "div", "e:0", "double:INF")]
    [Arguments("d:-1", "div", "e:0", "double:-INF")]
    [Arguments("f:1", "div", "f:0", "float:INF")]
    [Arguments("f:-1", "div", "f:0", "float:-INF")]
    [Arguments("f:0", "div", "f:0", "float:NaN")]
    [Arguments("i:1", "div", "f:0", "float:INF")]
    public async Task Division_OfFloatAndDoubleByZero_FollowsIeee(
        string left,
        string op,
        string right,
        string expected
    ) => await Assert.That(Describe(Run(left, op, right))).IsEqualTo(expected);

    [Test]
    [Arguments("e:1", "mod", "e:0", "double:NaN")]
    [Arguments("f:1", "mod", "f:0", "float:NaN")]
    [Arguments("e:INF", "mod", "e:2", "double:NaN")]
    [Arguments("e:NaN", "mod", "e:2", "double:NaN")]
    [Arguments("e:5", "mod", "e:INF", "double:5")]
    [Arguments("e:-5", "mod", "e:-INF", "double:-5")]
    [Arguments("e:-0", "mod", "e:5", "double:-0")]
    public async Task Mod_OfFloatAndDouble_FollowsIeee(string left, string op, string right, string expected) =>
        await Assert.That(Describe(Run(left, op, right))).IsEqualTo(expected);

    [Test]
    [Arguments("e:INF", "+", "e:1", "double:INF")]
    [Arguments("e:INF", "-", "e:INF", "double:NaN")]
    [Arguments("e:INF", "*", "e:0", "double:NaN")]
    [Arguments("e:NaN", "+", "i:1", "double:NaN")]
    [Arguments("e:1E308", "*", "e:10", "double:INF")]
    [Arguments("f:3E38", "*", "f:10", "float:INF")]
    [Arguments("e:-0", "+", "e:-0", "double:-0")]
    [Arguments("e:0", "*", "e:-1", "double:-0")]
    public async Task Overflow_AndSpecialValues_OfFloatAndDouble_FollowIeee(
        string left,
        string op,
        string right,
        string expected
    ) => await Assert.That(Describe(Run(left, op, right))).IsEqualTo(expected);

    [Test]
    public async Task Idiv_ChecksTheZeroDivisorBeforeTheNaNOperand()
    {
        // F&O 6.2.5 lists FOAR0001 for a zero divisor before FOAR0002 for NaN; both apply to NaN idiv 0.
        var error = Raised(() => Run("e:NaN", "idiv", "e:0"));

        await Assert.That(error!.Diagnostic.QName).IsEqualTo(XPathErrorCodes.FOAR0001);
    }
}
