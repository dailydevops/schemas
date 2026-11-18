namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.DateTime;

using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Functions.Binding;

/// <summary>The date and time group stages itself into the default catalog through its registration file, with the arities of the specification.</summary>
public class RegistrationTests
{
    [Test]
    [Arguments("year-from-dateTime", 1, 1)]
    [Arguments("month-from-dateTime", 1, 1)]
    [Arguments("day-from-dateTime", 1, 1)]
    [Arguments("hours-from-dateTime", 1, 1)]
    [Arguments("minutes-from-dateTime", 1, 1)]
    [Arguments("seconds-from-dateTime", 1, 1)]
    [Arguments("timezone-from-dateTime", 1, 1)]
    [Arguments("year-from-date", 1, 1)]
    [Arguments("month-from-date", 1, 1)]
    [Arguments("day-from-date", 1, 1)]
    [Arguments("timezone-from-date", 1, 1)]
    [Arguments("hours-from-time", 1, 1)]
    [Arguments("minutes-from-time", 1, 1)]
    [Arguments("seconds-from-time", 1, 1)]
    [Arguments("timezone-from-time", 1, 1)]
    [Arguments("years-from-duration", 1, 1)]
    [Arguments("months-from-duration", 1, 1)]
    [Arguments("days-from-duration", 1, 1)]
    [Arguments("hours-from-duration", 1, 1)]
    [Arguments("minutes-from-duration", 1, 1)]
    [Arguments("seconds-from-duration", 1, 1)]
    [Arguments("adjust-dateTime-to-timezone", 1, 2)]
    [Arguments("adjust-date-to-timezone", 1, 2)]
    [Arguments("adjust-time-to-timezone", 1, 2)]
    [Arguments("current-dateTime", 0, 0)]
    [Arguments("current-date", 0, 0)]
    [Arguments("current-time", 0, 0)]
    [Arguments("implicit-timezone", 0, 0)]
    public async Task DefaultCatalog_ResolvesTheFunctionForItsArityRangeOnly(string local, int minArity, int maxArity)
    {
        for (var arity = 0; arity <= 3; arity++)
        {
            var found = DateTimeHarness.TryResolve(local, arity, out var function);

            await Assert.That(found).IsEqualTo(arity >= minArity && arity <= maxArity);
            if (found)
            {
                await Assert.That(function!.Name).IsEqualTo(new XPathQName(DateTimeHarness.FunctionNamespace, local));
                await Assert.That(function.MinArity).IsEqualTo(minArity);
                await Assert.That(function.MaxArity).IsEqualTo(maxArity);
                await Assert.That(function.HostRequirements).IsEqualTo(XPathHostRequirements.None);
            }
        }
    }

    [Test]
    [Arguments(XPathVersion.V20)]
    [Arguments(XPathVersion.V30)]
    [Arguments(XPathVersion.V31)]
    public async Task DefaultCatalog_TheFunctionsAreVisibleInEveryLanguageVersion(XPathVersion version)
    {
        var context = XPathStaticContext.Default.ToBuilder().WithVersion(version).Build();

        var found = FunctionCatalog.Default.Registry.TryResolve(
            context,
            new XPathQName(DateTimeHarness.FunctionNamespace, "adjust-date-to-timezone"),
            2,
            XPathHostRequirements.None,
            out _
        );

        await Assert.That(found).IsTrue();
    }

    [Test]
    public async Task DefaultCatalog_SameLocalNameInAnotherNamespace_IsNotFound()
    {
        var found = FunctionCatalog.Default.Registry.TryResolve(
            XPathStaticContext.Default,
            new XPathQName("urn:other", "year-from-dateTime"),
            1,
            XPathHostRequirements.None,
            out _
        );

        await Assert.That(found).IsFalse();
    }
}
