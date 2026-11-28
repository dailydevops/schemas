namespace NetEvolve.Schemas.XPath.Tests.Unit.Errors;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Errors;

public class XPathErrorCodesTests
{
    // XML Path Language (XPath) 2.0 (Second Edition), appendix G "Error Conditions" (the appendix the work package calls F).
    private static readonly string[] XPath20Codes =
    [
        "XPST0001",
        "XPDY0002",
        "XPST0003",
        "XPTY0004",
        "XPST0005",
        "XPTY0006",
        "XPTY0007",
        "XPST0008",
        "XPST0010",
        "XPST0017",
        "XPTY0018",
        "XPTY0019",
        "XPTY0020",
        "XPDY0021",
        "XPDY0050",
        "XPST0051",
        "XPST0080",
        "XPST0081",
        "XPST0083",
    ];

    // XQuery 1.0 and XPath 2.0 Functions and Operators (Second Edition), appendix B "Error Summary".
    private static readonly string[] FunctionsCodes =
    [
        "FOER0000",
        "FOAR0001",
        "FOAR0002",
        "FOCA0001",
        "FOCA0002",
        "FOCA0003",
        "FOCA0005",
        "FOCA0006",
        "FOCH0001",
        "FOCH0002",
        "FOCH0003",
        "FOCH0004",
        "FODC0001",
        "FODC0002",
        "FODC0003",
        "FODC0004",
        "FODC0005",
        "FODT0001",
        "FODT0002",
        "FODT0003",
        "FONS0004",
        "FONS0005",
        "FORG0001",
        "FORG0002",
        "FORG0003",
        "FORG0004",
        "FORG0005",
        "FORG0006",
        "FORG0008",
        "FORG0009",
        "FORX0001",
        "FORX0002",
        "FORX0003",
        "FORX0004",
        "FOTY0012",
    ];

    // XML Path Language (XPath) 3.0, appendix G, and XQuery and XPath Functions and Operators 3.0, appendix D: the conditions that function items add.
    private static readonly string[] XPath30Codes = ["XQST0039", "FOTY0013"];

    // XML Path Language (XPath) 3.1, appendix F, and XQuery and XPath Functions and Operators 3.1, appendix D: the conditions that maps and arrays add.
    private static readonly string[] XPath31Codes =
    [
        "XQDY0137",
        "FORG0010",
        "FOJS0003",
        "FOJS0005",
        "FOAY0001",
        "FOAY0002",
    ];

    public static IEnumerable<string> AllCodes() =>
        [.. XPath20Codes, .. FunctionsCodes, .. XPath30Codes, .. XPath31Codes];

    private static HashSet<string> TableCodes(string prefix)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var code in XPathErrorCodes.All)
        {
            if (code.LocalName.StartsWith(prefix, StringComparison.Ordinal))
            {
                _ = set.Add(code.LocalName);
            }
        }

        return set;
    }

    [Test]
    public async Task Table_XPath20Codes_EqualAppendixList()
    {
        var table = TableCodes("XP");

        await Assert.That(table.SetEquals(XPath20Codes)).IsTrue();
        await Assert.That(table.Count).IsEqualTo(XPath20Codes.Length);
    }

    [Test]
    public async Task Table_FunctionsCodes_EqualErrorSummaryList()
    {
        var table = TableCodes("FO");

        string[] foCodes = [.. XPath31Codes.Where(static c => c.StartsWith("FO", StringComparison.Ordinal))];

        await Assert.That(table.SetEquals([.. FunctionsCodes, "FOTY0013", .. foCodes])).IsTrue();
        await Assert.That(table.Count).IsEqualTo(FunctionsCodes.Length + 1 + foCodes.Length);
    }

    [Test]
    public async Task Table_HasNoDuplicatesAndNoOtherPrefixes() =>
        await Assert
            .That(XPathErrorCodes.All.Count)
            .IsEqualTo(XPath20Codes.Length + FunctionsCodes.Length + XPath30Codes.Length + XPath31Codes.Length);

    [Test]
    [MethodDataSource(nameof(AllCodes))]
    public async Task Diagnostic_EveryCode_CarriesQNameMessageAndLocation(string localName)
    {
        var code = new XPathErrorCode(localName);

        var diagnostic = XPathDiagnostic.Create(code, 7, 3);

        await Assert.That(diagnostic.QName).IsEqualTo(code);
        await Assert.That(diagnostic.Code).IsEqualTo(localName);
        await Assert.That(diagnostic.Message).IsNotEmpty();
        await Assert.That(diagnostic.Offset).IsEqualTo(7);
        await Assert.That(diagnostic.Length).IsEqualTo(3);
    }

    [Test]
    [Arguments("XPST0003", XPathErrorKind.Static)]
    [Arguments("XQST0039", XPathErrorKind.Static)]
    [Arguments("XQDY0137", XPathErrorKind.Dynamic)]
    [Arguments("FOJS0003", XPathErrorKind.Function)]
    [Arguments("XPTY0004", XPathErrorKind.Type)]
    [Arguments("XPDY0002", XPathErrorKind.Dynamic)]
    [Arguments("FOAR0001", XPathErrorKind.Function)]
    public async Task Kind_SamplePerPrefix_IsCorrect(string localName, XPathErrorKind expected)
    {
        var diagnostic = XPathDiagnostic.Create(new XPathErrorCode(localName), 0, 1);

        await Assert.That(diagnostic.Kind).IsEqualTo(expected);
    }

    [Test]
    public async Task Diagnostic_CodeOutsideTable_IsEngineErrorWithFallbackMessage()
    {
        var diagnostic = XPathDiagnostic.Create(new XPathErrorCode("urn:example:errors", "XP0001"));

        await Assert.That(diagnostic.Kind).IsEqualTo(XPathErrorKind.Engine);
        await Assert.That(diagnostic.Message).IsEqualTo("{urn:example:errors}XP0001");
        await Assert.That(diagnostic.Offset).IsEqualTo(-1);
        await Assert.That(diagnostic.Length).IsEqualTo(0);
    }

    [Test]
    public async Task Diagnostic_InvalidLocation_Throws()
    {
        var code = XPathErrorCodes.XPST0003;

        await Assert.That(() => XPathDiagnostic.Create(code, -2, 0)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => XPathDiagnostic.Create(code, 0, -1)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task ThrowHelper_BuildsExceptionCarryingDiagnostic()
    {
        var exception = XPathErrors.Exception(XPathErrorCodes.XPST0017, 4, 2);

        await Assert.That(exception.Diagnostic.QName).IsEqualTo(XPathErrorCodes.XPST0017);
        await Assert.That(exception.Diagnostic.Offset).IsEqualTo(4);
        await Assert.That(exception.Message).IsEqualTo(exception.Diagnostic.Message);
    }
}
