namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.DateTime;

using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Abstractions;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>fn:parse-ietf-date</c> (Functions and Operators 3.1, 9.7.1): the examples of the specification, each grammar alternative, the
/// time zone forms, and the invalid input (FORG0010).
/// </summary>
public class ParseIetfDateTests
{
    private const string Namespace = "http://www.w3.org/2005/xpath-functions";

    // The examples of the specification.
    [Test]
    [Arguments("Wed, 06 Jun 1994 07:29:35 GMT", "1994-06-06T07:29:35Z")]
    [Arguments("Wed, 6 Jun 94 07:29:35 +0500", "1994-06-06T07:29:35+05:00")]
    [Arguments("Wed Jun 06 11:54:45 EST 2013", "2013-06-06T11:54:45-05:00")]
    [Arguments("Sunday, 06-Nov-94 08:49:37 GMT", "1994-11-06T08:49:37Z")]
    [Arguments("Wed Nov  6 08:49:37 1994", "1994-11-06T08:49:37Z")]
    [Arguments("Sun Nov  6 08:49:37 GMT 1994", "1994-11-06T08:49:37Z")]
    public async Task Examples_OfTheSpecification(string input, string expected) =>
        await Assert.That(Run(input)).IsEqualTo(expected);

    // The alternatives of the grammar.
    [Test]
    [Arguments("06 Jun 1994 07:29:35 GMT", "1994-06-06T07:29:35Z")]
    [Arguments("Wed 06 Jun 1994 07:29:35 GMT", "1994-06-06T07:29:35Z")]
    [Arguments("Wednesday, 6 JUN 1994 7:29 gmt", "1994-06-06T07:29:00Z")]
    [Arguments("  \t Wed,\r\n 06 \n Jun   1994  07:29:35.25  UTC  ", "1994-06-06T07:29:35.25Z")]
    [Arguments("6 - Jun - 1994 07:29 GMT", "1994-06-06T07:29:00Z")]
    [Arguments("6-Jun-1994 07:29", "1994-06-06T07:29:00Z")]
    [Arguments("6 Jun 49 07:29:35 UT", "1949-06-06T07:29:35Z")]
    [Arguments("6 Jun 1994 24:00:00 GMT", "1994-06-07T00:00:00Z")]
    [Arguments("31 Dec 1994 24:00 GMT", "1995-01-01T00:00:00Z")]
    [Arguments("Jun 06 11:54:45 1994", "1994-06-06T11:54:45Z")]
    [Arguments("Jun 06 11:54:45 +0100 1994", "1994-06-06T11:54:45+01:00")]
    [Arguments("Jun 6 11:54 94", "1994-06-06T11:54:00Z")]
    [Arguments("Mon Jun 06 11:54:45 1994 ", "1994-06-06T11:54:45Z")]
    public async Task Grammar_Alternatives(string input, string expected) =>
        await Assert.That(Run(input)).IsEqualTo(expected);

    [Test]
    [Arguments("EST", "-05:00")]
    [Arguments("EDT", "-04:00")]
    [Arguments("CST", "-06:00")]
    [Arguments("CDT", "-05:00")]
    [Arguments("MST", "-07:00")]
    [Arguments("MDT", "-06:00")]
    [Arguments("PST", "-08:00")]
    [Arguments("PDT", "-07:00")]
    [Arguments("pst", "-08:00")]
    [Arguments("+0100", "+01:00")]
    [Arguments("-0530", "-05:30")]
    [Arguments("+01:00", "+01:00")]
    [Arguments("+1:30", "+01:30")]
    [Arguments("+1", "+01:00")]
    [Arguments("+01", "+01:00")]
    [Arguments("-0000", "Z")]
    [Arguments("+530", "+05:30")]
    [Arguments("+0100 (PST)", "+01:00")]
    [Arguments("+0100(PST)", "+01:00")]
    [Arguments("+0100 ( pdt )", "+01:00")]
    [Arguments("+14:00", "+14:00")]
    [Arguments("-1400", "-14:00")]
    public async Task TimeZones(string zone, string expectedOffset)
    {
        var expected = "1994-06-06T07:29:35" + expectedOffset;
        await Assert.That(Run("Wed, 06 Jun 1994 07:29:35 " + zone)).IsEqualTo(expected);
        await Assert.That(Run("Wed, 06 Jun 1994 07:29:35" + zone)).IsEqualTo(expected);
    }

    [Test]
    public async Task EmptyArgument_GivesTheEmptySequence() => await Assert.That(Run(null)).IsEqualTo("()");

    [Test]
    [Arguments("")]
    [Arguments("   ")]
    [Arguments("Wed, 06 Jun")]
    [Arguments("Wed, 06 Jun 1994")]
    [Arguments("Wed, 06 Jun 1994 07")]
    [Arguments("Wed, 06 Jun 1994 07:2 GMT")]
    [Arguments("Wed, 06 Jun 1994 07:29:3 GMT")]
    [Arguments("Wed, 06 Jun 1994 07:29:35. GMT")]
    [Arguments("Wed, 06 Jun 1994 07:29:35 XYZ")]
    [Arguments("Wed, 06 Jun 1994 07:29:35 GMT extra")]
    [Arguments("Wed, 06 Jun 1994 07:29:35 +0100 (XYZ)")]
    [Arguments("Wed, 06 Jun 1994 07:29:35 +0100 (PST")]
    [Arguments("Wed, 06 Jun 1994 07:29:35 +01000")]
    [Arguments("Wed, 06 Jun 1994 07:29:35 +")]
    [Arguments("Wed, 06 Jun 1994 07:29:35 +1:3")]
    [Arguments("Wed, 06 Jun 1994 07:29:35 +001:30")]
    [Arguments("Wed, 06 Jun 1994 07:29:35 +1500")]
    [Arguments("Wed, 06 Jun 1994 07:29:35 +0160")]
    [Arguments("Wed, 06 Jun 1994 07:29:35 +14:30")]
    [Arguments("Wed, 06 Jun 994 07:29:35 GMT")]
    [Arguments("Wed, 06 Jun 19944 07:29:35 GMT")]
    [Arguments("Wed, 06 Jun 4 07:29:35 GMT")]
    [Arguments("Wed, 06 Foo 1994 07:29:35 GMT")]
    [Arguments("Wed, 006 Jun 1994 07:29:35 GMT")]
    [Arguments("Wed, 32 Jun 1994 07:29:35 GMT")]
    [Arguments("Wed, 31 Jun 1994 07:29:35 GMT")]
    [Arguments("Wed, 29 Feb 1995 07:29:35 GMT")]
    [Arguments("Wed, 0 Jun 1994 07:29:35 GMT")]
    [Arguments("Wed, 06 Jun 1994 25:29:35 GMT")]
    [Arguments("Wed, 06 Jun 1994 24:01:00 GMT")]
    [Arguments("Wed, 06 Jun 1994 24:00:01 GMT")]
    [Arguments("Wed, 06 Jun 1994 07:60:35 GMT")]
    [Arguments("Wed, 06 Jun 1994 07:29:60 GMT")]
    [Arguments("Wed, 06 Jun 1994 007:29:35 GMT")]
    [Arguments("Wed, 06 Jun 1994 07-29-35 GMT")]
    [Arguments("Wed,, 06 Jun 1994 07:29:35 GMT")]
    [Arguments("Wednes, 06 Jun 1994 07:29:35 GMT")]
    [Arguments("Wed,06 Jun 1994 07:29:35 GMT")]
    [Arguments("Wed 06Jun 1994 07:29:35 GMT")]
    [Arguments("Wed, 06 Jun1994 07:29:35 GMT")]
    [Arguments("Wed, 06 Jun 1994T07:29:35 GMT")]
    [Arguments("Jun 06 11:54:45 GMT")]
    [Arguments("Jun 06 11:54:45 1994 1994")]
    [Arguments("Jun 06 11:54:45 994")]
    [Arguments("Jun 06 11:54:45")]
    [Arguments("Jun 06 1994")]
    [Arguments("1994-06-06T07:29:35Z")]
    public async Task InvalidInput_RaisesFORG0010(string input) =>
        await Assert.That(Run(input)).IsEqualTo("error:FORG0010");

    [Test]
    [Arguments(XPathVersion.V30, true)]
    [Arguments(XPathVersion.V31, true)]
    [Arguments(XPathVersion.V20, false)]
    public async Task Visibility_IsXPath30And31(XPathVersion version, bool expected)
    {
        var found = FunctionCatalog.Default.Registry.TryResolve(
            XPathStaticContext.Default.ToBuilder().WithVersion(version).Build(),
            new XPathQName(Namespace, "parse-ietf-date"),
            1,
            XPathHostRequirements.None,
            out _
        );
        await Assert.That(found).IsEqualTo(expected);
    }

    private static string Run(string? input)
    {
        _ = FunctionCatalog.Default.Registry.TryResolve(
            XPathStaticContext.Default.ToBuilder().WithVersion(XPathVersion.V31).Build(),
            new XPathQName(Namespace, "parse-ietf-date"),
            1,
            XPathHostRequirements.None,
            out var function
        );
        using var context = new XPathEvaluationContext();
        try
        {
            var result = function!.Call(
                context,
                [input is null ? XdmSequence.Empty : XdmSequence.Create(XdmItem.FromString(input))]
            );
            if (result.Count == 0)
            {
                return "()";
            }

            return result.Count == 1 && result[0].TryGetDateTime(out var value)
                ? value.ToString()
                : "error:not-a-dateTime";
        }
        catch (XPathException failure)
        {
            return "error:" + failure.Diagnostic.Code;
        }
    }
}
