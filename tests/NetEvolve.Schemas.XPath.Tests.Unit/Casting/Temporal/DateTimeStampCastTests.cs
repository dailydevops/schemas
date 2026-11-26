namespace NetEvolve.Schemas.XPath.Tests.Unit.Casting.Temporal;

using System;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Syntax.Parser;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static NetEvolve.Schemas.XPath.Tests.Unit.Casting.Core.CastFixture;

/// <summary>
/// Casting to <c>xs:dateTimeStamp</c> (XPath 3.1): the value is cast to <c>xs:dateTime</c> and must have an explicit timezone, and the
/// type and its constructor function exist in XPath 3.1 only.
/// </summary>
public class DateTimeStampCastTests
{
    private static string Run(string expression, XPathVersion version = XPathVersion.V31)
    {
        _ = XPathParser.TryParse(expression.AsSpan(), out var syntax, out _);
        var statics = Statics.ToBuilder().WithVersion(version).Build();
        if (
            !ExpressionCompiler.TryCompile(
                syntax!,
                expression,
                statics,
                Registry(),
                out var compiled,
                out var diagnostic
            )
        )
        {
            return diagnostic.Code;
        }

        try
        {
            using var context = new XPathEvaluationContext();
            var result = compiled!.Evaluate(context);
            return result.Count == 1 ? Render(result[0]) : "count:" + result.Count;
        }
        catch (XPathException failure)
        {
            return failure.Diagnostic.Code;
        }
    }

    [Test]
    [Arguments("'2020-01-02T03:04:05Z' cast as xs:dateTimeStamp cast as xs:string", "string:2020-01-02T03:04:05Z")]
    [Arguments("xs:dateTimeStamp('2020-01-02T03:04:05+02:00') cast as xs:string", "string:2020-01-02T03:04:05+02:00")]
    [Arguments(
        "xs:dateTimeStamp(xs:untypedAtomic('2020-01-02T03:04:05-05:00')) cast as xs:string",
        "string:2020-01-02T03:04:05-05:00"
    )]
    [Arguments(
        "xs:dateTimeStamp(xs:dateTime('2020-01-02T03:04:05Z')) cast as xs:string",
        "string:2020-01-02T03:04:05Z"
    )]
    [Arguments("xs:dateTimeStamp(xs:date('2020-01-02Z')) cast as xs:string", "string:2020-01-02T00:00:00Z")]
    [Arguments(
        "xs:dateTimeStamp(xs:dateTimeStamp('2020-01-02T03:04:05Z')) cast as xs:string",
        "string:2020-01-02T03:04:05Z"
    )]
    [Arguments(
        "xs:dateTimeStamp(xs:dateTimeStamp('2020-01-02T03:04:05Z')) cast as xs:date cast as xs:string",
        "string:2020-01-02Z"
    )]
    [Arguments("xs:dateTimeStamp(()) cast as xs:string", "count:0")]
    public async Task Cast_WithAnExplicitTimezone_Succeeds(string expression, string expected) =>
        await Assert.That(Run(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("'2020-01-02T03:04:05' cast as xs:dateTimeStamp")]
    [Arguments("xs:dateTimeStamp('2020-01-02T03:04:05')")]
    [Arguments("xs:dateTimeStamp(xs:dateTime('2020-01-02T03:04:05'))")]
    [Arguments("xs:dateTimeStamp(xs:date('2020-01-02'))")]
    [Arguments("xs:dateTimeStamp('not a date')")]
    [Arguments("xs:dateTimeStamp('2020-01-02')")]
    public async Task Cast_WithoutATimezoneOrAnInvalidForm_IsForg0001(string expression) =>
        await Assert.That(Run(expression)).IsEqualTo("FORG0001");

    [Test]
    [Arguments("'2020-01-02T03:04:05' castable as xs:dateTimeStamp", "boolean:false")]
    [Arguments("'2020-01-02T03:04:05Z' castable as xs:dateTimeStamp", "boolean:true")]
    [Arguments("xs:date('2020-01-02Z') castable as xs:dateTimeStamp", "boolean:true")]
    [Arguments("xs:time('03:04:05Z') castable as xs:dateTimeStamp", "boolean:false")]
    [Arguments("5 castable as xs:dateTimeStamp", "boolean:false")]
    public async Task Castable_FollowsTheCast(string expression, string expected) =>
        await Assert.That(Run(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("5 cast as xs:dateTimeStamp")]
    [Arguments("xs:time('03:04:05Z') cast as xs:dateTimeStamp")]
    [Arguments("xs:dateTimeStamp(5)")]
    public async Task Cast_FromATypeWithoutARow_IsXpty0004(string expression) =>
        await Assert.That(Run(expression)).IsEqualTo("XPTY0004");

    [Test]
    [Arguments("xs:dateTimeStamp('2020-01-02T03:04:05Z') instance of xs:dateTimeStamp", "boolean:true")]
    [Arguments("xs:dateTimeStamp('2020-01-02T03:04:05Z') instance of xs:dateTime", "boolean:true")]
    [Arguments("xs:dateTime('2020-01-02T03:04:05Z') instance of xs:dateTimeStamp", "boolean:false")]
    [Arguments("xs:dateTimeStamp('2020-01-02T03:04:05Z') eq xs:dateTime('2020-01-02T03:04:05Z')", "boolean:true")]
    [Arguments("xs:dateTimeStamp('2020-01-02T03:04:05Z') lt xs:dateTimeStamp('2020-01-02T03:04:06Z')", "boolean:true")]
    public async Task Value_IsADateTimeWithTheTypeOfItsOwn(string expression, string expected) =>
        await Assert.That(Run(expression)).IsEqualTo(expected);

    [Test]
    [Arguments(XPathVersion.V20)]
    [Arguments(XPathVersion.V30)]
    public async Task Type_BeforeXPath31_IsRejected(XPathVersion version)
    {
        await Assert.That(Run("'2020-01-02T03:04:05Z' cast as xs:dateTimeStamp", version)).IsEqualTo("XPST0051");
        await Assert.That(Run("'2020-01-02T03:04:05Z' castable as xs:dateTimeStamp", version)).IsEqualTo("XPST0051");
        await Assert.That(Run("xs:dateTimeStamp('2020-01-02T03:04:05Z')", version)).IsEqualTo("XPST0017");
    }
}
