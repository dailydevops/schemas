namespace NetEvolve.Schemas.XPath.Tests.Unit.Formatting.Numbers;

using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Api;

/// <summary>
/// <c>format-number</c> is not a function of XPath 2.0: it is visible in a 2.0 static context only under the XSLT 2.0 host flag, and in
/// XPath 3.0 and 3.1 without a flag. Where it is not visible, the call is XPST0017 (no function matches the name and number of arguments).
/// </summary>
public class RegistrationTests
{
    [Test]
    [Arguments(2, XPathVersion.V20, XPathHostRequirements.None, false)]
    [Arguments(3, XPathVersion.V20, XPathHostRequirements.None, false)]
    [Arguments(2, XPathVersion.V20, XPathHostRequirements.CurrentItem, false)]
    [Arguments(2, XPathVersion.V20, XPathHostRequirements.DecimalFormats, false)]
    [Arguments(2, XPathVersion.V20, XPathHostRequirements.Xslt20Host, true)]
    [Arguments(3, XPathVersion.V20, XPathHostRequirements.Xslt20Host, true)]
    [Arguments(2, XPathVersion.V20, XPathHostRequirements.Xslt20Host | XPathHostRequirements.DecimalFormats, true)]
    [Arguments(1, XPathVersion.V20, XPathHostRequirements.Xslt20Host, false)]
    [Arguments(4, XPathVersion.V20, XPathHostRequirements.Xslt20Host, false)]
    [Arguments(2, XPathVersion.V30, XPathHostRequirements.None, true)]
    [Arguments(3, XPathVersion.V30, XPathHostRequirements.None, true)]
    [Arguments(2, XPathVersion.V31, XPathHostRequirements.None, true)]
    [Arguments(3, XPathVersion.V31, XPathHostRequirements.None, true)]
    [Arguments(2, XPathVersion.V30, XPathHostRequirements.Xslt20Host, true)]
    [Arguments(1, XPathVersion.V31, XPathHostRequirements.None, false)]
    [Arguments(4, XPathVersion.V31, XPathHostRequirements.None, false)]
    public async Task FormatNumber_IsFoundOnlyForTheXslt20HostAndForXPath30AndLater(
        int arity,
        XPathVersion version,
        XPathHostRequirements host,
        bool expected
    )
    {
        var found = FormatNumberHarness.TryResolve(arity, out var function, version, host);

        await Assert.That(found).IsEqualTo(expected);
        if (found)
        {
            await Assert
                .That(function!.Name)
                .IsEqualTo(new XPathQName(FormatNumberHarness.FunctionNamespace, "format-number"));
        }
    }

    // Plain XPath 2.0 without the host flag: the call is not bound, which the compiler reports as XPST0017.
    [Test]
    public async Task FormatNumber_InPlainXPath20_IsNotBound_SoTheCallIsXPST0017()
    {
        await Assert
            .That(FormatNumberHarness.Run(XPathVersion.V20, XPathHostRequirements.None, "integer:5", "0", null, null))
            .IsEqualTo("error:not-registered");

        // The same call binds the moment the host sets the flag, so the check above is about the flag and not about the harness.
        await Assert.That(FormatNumberHarness.Xslt20("integer:5", "0")).IsEqualTo("5");
    }

    [Test]
    public async Task FormatNumber_RegistersOneFunctionPerRuleSet_WithDisjointVersions()
    {
        _ = FormatNumberHarness.TryResolve(2, out var xslt, XPathVersion.V20, XPathHostRequirements.Xslt20Host);
        _ = FormatNumberHarness.TryResolve(2, out var xpath, XPathVersion.V31);

        await Assert.That(xslt!.Versions).IsEqualTo(XPathVersionMask.V20);
        await Assert.That(xslt.HostRequirements).IsEqualTo(XPathHostRequirements.Xslt20Host);
        await Assert.That(xpath!.Versions).IsEqualTo(XPathVersionMask.V30 | XPathVersionMask.V31);
        await Assert.That(xpath.HostRequirements).IsEqualTo(XPathHostRequirements.None);
        await Assert.That(xslt.MinArity).IsEqualTo(2);
        await Assert.That(xslt.MaxArity).IsEqualTo(3);
        await Assert.That(xpath.MinArity).IsEqualTo(2);
        await Assert.That(xpath.MaxArity).IsEqualTo(3);
    }
}
