namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Math;

using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The functions of the <c>math</c> namespace (Functions and Operators 3.1, 4.8): registration, the empty sequence, the special
/// values of <c>math:pow</c> and <c>math:atan2</c>, and a table of fixed inputs whose results are compared bit for bit.
/// </summary>
public class MathFunctionTests
{
    private const double Inf = double.PositiveInfinity;
    private const double NegInf = double.NegativeInfinity;
    private const double NaN = double.NaN;
    private const double NegZero = -0.0;

    [Test]
    [Arguments("pi", 0)]
    [Arguments("exp", 1)]
    [Arguments("exp10", 1)]
    [Arguments("log", 1)]
    [Arguments("log10", 1)]
    [Arguments("pow", 2)]
    [Arguments("sqrt", 1)]
    [Arguments("sin", 1)]
    [Arguments("cos", 1)]
    [Arguments("tan", 1)]
    [Arguments("asin", 1)]
    [Arguments("acos", 1)]
    [Arguments("atan", 1)]
    [Arguments("atan2", 2)]
    public async Task Registry_ResolvesInXPath30AndLater_ButNotInXPath20(
        string local,
        int arity,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        // In XPath 2.0 the registry does not resolve the name, which the static check reports as XPST0017.
        await Assert.That(MathHarness.TryResolve(local, arity, out _, XPathVersion.V20)).IsFalse();
        await Assert.That(MathHarness.TryResolve(local, arity, out _, XPathVersion.V30)).IsTrue();
        await Assert.That(MathHarness.TryResolve(local, arity, out _, XPathVersion.V31)).IsTrue();
        await Assert.That(MathHarness.TryResolve(local, arity + 1, out _, XPathVersion.V31)).IsFalse();
    }

    [Test]
    [Arguments("exp")]
    [Arguments("exp10")]
    [Arguments("log")]
    [Arguments("log10")]
    [Arguments("sqrt")]
    [Arguments("sin")]
    [Arguments("cos")]
    [Arguments("tan")]
    [Arguments("asin")]
    [Arguments("acos")]
    [Arguments("atan")]
    public async Task UnaryFunction_ForTheEmptySequence_ReturnsTheEmptySequence(
        string local,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(MathHarness.Call(local, XdmSequence.Empty).Count).IsEqualTo(0);
    }

    [Test]
    public async Task Pow_ForAnEmptyBase_ReturnsTheEmptySequence(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = MathHarness.Call("pow", XdmSequence.Empty, XdmSequence.Create(XdmItem.FromDouble(2)));

        await Assert.That(result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Atan2_ForAnEmptyArgument_RaisesXPTY0004(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var one = XdmSequence.Create(XdmItem.FromDouble(1));

        await Assert.That(MathHarness.ErrorOf("atan2", XdmSequence.Empty, one)).IsEqualTo("XPTY0004");
        await Assert.That(MathHarness.ErrorOf("atan2", one, XdmSequence.Empty)).IsEqualTo("XPTY0004");
    }

    [Test]
    public async Task Arguments_AreConvertedToDouble(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var root = MathHarness.Call("sqrt", XdmSequence.Create(XdmItem.FromInteger(4)));
        var power = MathHarness.Call(
            "pow",
            XdmSequence.Create(XdmItem.FromInteger(2)),
            XdmSequence.Create(XdmItem.FromInteger(10))
        );

        await Assert.That(root[0].TryGetDouble(out var rootValue)).IsTrue();
        await Assert.That(rootValue).IsEqualTo(2d);
        await Assert.That(power[0].TryGetDouble(out var powerValue)).IsTrue();
        await Assert.That(powerValue).IsEqualTo(1024d);
    }

    [Test]
    // y = 0 and x = 1 give 1 for every other argument, NaN included.
    [Arguments(2.0, 0.0, 1.0)]
    [Arguments(NaN, 0.0, 1.0)]
    [Arguments(NaN, NegZero, 1.0)]
    [Arguments(Inf, 0.0, 1.0)]
    [Arguments(1.0, NaN, 1.0)]
    [Arguments(1.0, Inf, 1.0)]
    [Arguments(-1.0, Inf, 1.0)]
    [Arguments(-1.0, NegInf, 1.0)]
    [Arguments(NaN, 2.0, NaN)]
    [Arguments(2.0, NaN, NaN)]
    // x = +0 and x = -0.
    [Arguments(0.0, 3.0, 0.0)]
    [Arguments(0.0, 2.0, 0.0)]
    [Arguments(NegZero, 3.0, NegZero)]
    [Arguments(NegZero, 2.0, 0.0)]
    [Arguments(NegZero, 2.5, 0.0)]
    [Arguments(0.0, -3.0, Inf)]
    [Arguments(0.0, -2.0, Inf)]
    [Arguments(NegZero, -3.0, NegInf)]
    [Arguments(NegZero, -2.0, Inf)]
    [Arguments(NegZero, -2.5, Inf)]
    // x = infinity.
    [Arguments(Inf, 2.0, Inf)]
    [Arguments(Inf, -2.0, 0.0)]
    [Arguments(NegInf, 3.0, NegInf)]
    [Arguments(NegInf, 2.0, Inf)]
    [Arguments(NegInf, 2.5, Inf)]
    [Arguments(NegInf, -3.0, NegZero)]
    [Arguments(NegInf, -2.0, 0.0)]
    [Arguments(NegInf, -2.5, 0.0)]
    // y = infinity.
    [Arguments(0.5, NegInf, Inf)]
    [Arguments(0.5, Inf, 0.0)]
    [Arguments(-0.5, NegInf, Inf)]
    [Arguments(-0.5, Inf, 0.0)]
    [Arguments(2.0, NegInf, 0.0)]
    [Arguments(2.0, Inf, Inf)]
    [Arguments(-2.0, NegInf, 0.0)]
    [Arguments(-2.0, Inf, Inf)]
    // a finite negative x with a finite y that is not an integer.
    [Arguments(-8.0, 0.5, NaN)]
    [Arguments(-8.0, 3.0, -512.0)]
    public async Task Pow_FollowsTheSpecialValueTable(
        double x,
        double y,
        double expected,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(MathHarness.Same(MathHarness.CallDouble("pow", x, y), expected)).IsTrue();
    }

    [Test]
    // y is a zero.
    [Arguments(0.0, 0.0, 0.0)]
    [Arguments(NegZero, 0.0, NegZero)]
    [Arguments(0.0, NegZero, 3.141592653589793)]
    [Arguments(NegZero, NegZero, -3.141592653589793)]
    [Arguments(0.0, -1.0, 3.141592653589793)]
    [Arguments(NegZero, -1.0, -3.141592653589793)]
    [Arguments(0.0, 1.0, 0.0)]
    [Arguments(NegZero, 1.0, NegZero)]
    // x is a zero.
    [Arguments(1.0, 0.0, 1.5707963267948966)]
    [Arguments(1.0, NegZero, 1.5707963267948966)]
    [Arguments(-1.0, 0.0, -1.5707963267948966)]
    [Arguments(-1.0, NegZero, -1.5707963267948966)]
    // y is infinity.
    [Arguments(Inf, Inf, 0.7853981633974483)]
    [Arguments(NegInf, Inf, -0.7853981633974483)]
    [Arguments(Inf, NegInf, 2.356194490192345)]
    [Arguments(NegInf, NegInf, -2.356194490192345)]
    [Arguments(Inf, 1.0, 1.5707963267948966)]
    [Arguments(NegInf, 1.0, -1.5707963267948966)]
    // x is infinity and y is finite.
    [Arguments(1.0, Inf, 0.0)]
    [Arguments(-1.0, Inf, NegZero)]
    [Arguments(1.0, NegInf, 3.141592653589793)]
    [Arguments(-1.0, NegInf, -3.141592653589793)]
    // NaN.
    [Arguments(NaN, 1.0, NaN)]
    [Arguments(1.0, NaN, NaN)]
    public async Task Atan2_FollowsTheSpecialValueTable(
        double y,
        double x,
        double expected,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(MathHarness.Same(MathHarness.CallDouble("atan2", y, x), expected)).IsTrue();
    }

    [Test]
    // The results below are the correctly rounded values; they must be bit for bit the same on every target framework.
    [Arguments("exp", 1.0, 2.718281828459045)]
    [Arguments("exp", 0.0, 1.0)]
    [Arguments("exp", NegInf, 0.0)]
    [Arguments("exp", Inf, Inf)]
    [Arguments("exp10", 2.0, 100.0)]
    [Arguments("exp10", 0.0, 1.0)]
    [Arguments("exp10", -1.0, 0.1)]
    [Arguments("exp10", NegInf, 0.0)]
    [Arguments("log", 1.0, 0.0)]
    [Arguments("log", 10.0, 2.302585092994046)]
    [Arguments("log", 0.0, NegInf)]
    [Arguments("log", NegZero, NegInf)]
    [Arguments("log", -1.0, NaN)]
    [Arguments("log", Inf, Inf)]
    [Arguments("log10", 1000.0, 3.0)]
    [Arguments("log10", 0.0, NegInf)]
    [Arguments("log10", -1.0, NaN)]
    [Arguments("sqrt", 2.0, 1.4142135623730951)]
    [Arguments("sqrt", 0.0, 0.0)]
    [Arguments("sqrt", NegZero, NegZero)]
    [Arguments("sqrt", -1.0, NaN)]
    [Arguments("sqrt", Inf, Inf)]
    [Arguments("sin", 1.0, 0.8414709848078965)]
    [Arguments("sin", NegZero, NegZero)]
    [Arguments("sin", Inf, NaN)]
    [Arguments("cos", 1.0, 0.5403023058681398)]
    [Arguments("cos", 0.0, 1.0)]
    [Arguments("cos", NegInf, NaN)]
    [Arguments("tan", 1.0, 1.5574077246549023)]
    [Arguments("tan", NegZero, NegZero)]
    [Arguments("tan", Inf, NaN)]
    [Arguments("asin", 1.0, 1.5707963267948966)]
    [Arguments("asin", NegZero, NegZero)]
    [Arguments("asin", 2.0, NaN)]
    [Arguments("acos", 0.0, 1.5707963267948966)]
    [Arguments("acos", 1.0, 0.0)]
    [Arguments("acos", 2.0, NaN)]
    [Arguments("atan", 1.0, 0.7853981633974483)]
    [Arguments("atan", NegZero, NegZero)]
    [Arguments("atan", Inf, 1.5707963267948966)]
    [Arguments("atan", NegInf, -1.5707963267948966)]
    [Arguments("atan", NaN, NaN)]
    public async Task UnaryFunction_ForFixedInputs_IsBitIdentical(
        string local,
        double argument,
        double expected,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(MathHarness.Same(MathHarness.CallDouble(local, argument), expected)).IsTrue();
    }

    [Test]
    public async Task Pi_ReturnsTheDoubleClosestToPi(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(MathHarness.Same(MathHarness.CallDouble("pi"), 3.141592653589793)).IsTrue();
    }

    [Test]
    [Arguments(2.0, 10.0, 1024.0)]
    [Arguments(2.0, 0.5, 1.4142135623730951)]
    [Arguments(10.0, -2.0, 0.01)]
    public async Task Pow_ForFixedInputs_IsBitIdentical(
        double x,
        double y,
        double expected,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(MathHarness.Same(MathHarness.CallDouble("pow", x, y), expected)).IsTrue();
    }
}
