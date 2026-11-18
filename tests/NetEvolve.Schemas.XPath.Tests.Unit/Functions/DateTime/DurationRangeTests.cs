namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.DateTime;

using System.Numerics;
using System.Threading.Tasks;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.Xml.Datatypes.Duration;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The range of the integer components: the largest lexical durations fit, and a result of duration arithmetic beyond 64 bits is reported, not wrapped.</summary>
public class DurationRangeTests
{
    [Test]
    [Arguments("days-from-duration", "duration:P99999999999999999D", "99999999999999999")]
    [Arguments("years-from-duration", "duration:P99999999999999999Y", "99999999999999999")]
    [Arguments("hours-from-duration", "duration:PT99999999999999999H", "15")]
    [Arguments("days-from-duration", "duration:-PT99999999999999999H", "-4166666666666666")]
    public async Task Component_LargestLexicalDurations_Fit(string function, string argument, string expected) =>
        await Assert.That(DateTimeHarness.Run(function, argument)).IsEqualTo(expected);

    [Test]
    public async Task Days_BeyondTheIntegerRange_RaisesFOAR0002()
    {
        var huge = XdmItem.FromDuration(
            new XsdDuration(0, new XsdDecimal(BigInteger.Pow(10, 30), 0)),
            DurationKind.DayTime
        );
        using var context = new XPathEvaluationContext();
        DateTimeHarness.TryResolve("days-from-duration", 1, out var function);

        var failure = Assert.Throws<XPathException>(() => function!.Call(context, [XdmSequence.Create(huge)]));

        await Assert.That(failure.Diagnostic.Code).IsEqualTo("FOAR0002");
    }
}
