namespace NetEvolve.Schemas.Xml.Tests.Unit.Contracts;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using NetEvolve.Schemas.Xml.Contracts;

public class XmlErrorCodesTests
{
    // The catalog approved in the XML core design (XML-01): well-formedness, limits, security, namespaces, encoding.
    private static readonly string[] ApprovedAliases =
    [
        .. Range("XML00", 1, 19),
        .. Range("XML11", 1, 16),
        "XML2001",
        "XML2002",
        .. Range("XML300", 1, 5),
        .. Range("XML400", 1, 3),
    ];

    private static IEnumerable<string> Range(string prefix, int first, int last)
    {
        for (var i = first; i <= last; i++)
        {
            yield return prefix
                + (
                    prefix.Length == 5
                        ? i.ToString("D2", CultureInfo.InvariantCulture)
                        : i.ToString(CultureInfo.InvariantCulture)
                );
        }
    }

    private static int Formula(string alias) =>
        1000 + (100 * (alias[3] - '0')) + int.Parse(alias.AsSpan(5), CultureInfo.InvariantCulture);

    [Test]
    public async Task All_ContainsExactlyTheApprovedCatalog()
    {
        var aliases = new List<string>();
        foreach (var code in XmlErrorCodes.All)
        {
            aliases.Add(code.Alias);
        }

        await Assert.That(aliases).IsEquivalentTo(ApprovedAliases);
    }

    [Test]
    public async Task All_AliasesAndCodesAreUnique()
    {
        var aliases = new HashSet<string>(StringComparer.Ordinal);
        var numbers = new HashSet<int>();
        foreach (var code in XmlErrorCodes.All)
        {
            aliases.Add(code.Alias);
            numbers.Add(code.Code.Number);
        }

        await Assert.That(aliases).Count().IsEqualTo(XmlErrorCodes.All.Count);
        await Assert.That(numbers).Count().IsEqualTo(XmlErrorCodes.All.Count);
    }

    [Test]
    public async Task All_HaveAMessageAndExactlyOneNamedConstraint()
    {
        var missing = new List<string>();
        foreach (var code in XmlErrorCodes.All)
        {
            if (string.IsNullOrWhiteSpace(code.Message) || string.IsNullOrWhiteSpace(code.Constraint))
            {
                missing.Add(code.Alias);
            }
        }

        await Assert.That(missing).IsEmpty();
    }

    [Test]
    public async Task All_NesConstantFollowsTheAliasFormulaAndLiesInTheXmlRange()
    {
        var failures = new List<string>();
        foreach (var code in XmlErrorCodes.All)
        {
            if (code.Code.Number != Formula(code.Alias) || !DiagnosticRanges.Xml.Contains(code.Code))
            {
                failures.Add(code.Alias + " -> " + code.Code);
            }
        }

        await Assert.That(failures).IsEmpty();
        await Assert.That(XmlErrorCodes.XML0001.Code).IsEqualTo(new DiagnosticCode(1001));
        await Assert.That(XmlErrorCodes.XML1101.Code).IsEqualTo(new DiagnosticCode(1101));
        await Assert.That(XmlErrorCodes.XML2001.Code).IsEqualTo(new DiagnosticCode(1201));
        await Assert.That(XmlErrorCodes.XML4003.Code).IsEqualTo(new DiagnosticCode(1403));
    }

    [Test]
    public async Task CoreCodeColumn_MapsOnlyTheFiveCoreLimits()
    {
        var mapped = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var code in XmlErrorCodes.All)
        {
            if (code.CoreCode != default)
            {
                mapped[code.Alias] = code.CoreCode.Number;
            }
        }

        await Assert.That(mapped).Count().IsEqualTo(5);
        await Assert.That(mapped["XML1101"]).IsEqualTo(1);
        await Assert.That(mapped["XML1102"]).IsEqualTo(2);
        await Assert.That(mapped["XML1103"]).IsEqualTo(3);
        await Assert.That(mapped["XML1109"]).IsEqualTo(4);
        await Assert.That(mapped["XML1110"]).IsEqualTo(5);
        await Assert.That(XmlErrorCodes.XML1101.CoreCode).IsEqualTo(CoreDiagnosticCodes.InputSizeLimitExceeded);
        await Assert.That(XmlErrorCodes.XML1102.CoreCode).IsEqualTo(CoreDiagnosticCodes.DepthLimitExceeded);
        await Assert.That(XmlErrorCodes.XML1103.CoreCode).IsEqualTo(CoreDiagnosticCodes.AttributeLimitExceeded);
        await Assert.That(XmlErrorCodes.XML1109.CoreCode).IsEqualTo(CoreDiagnosticCodes.EntityExpansionLimitExceeded);
        await Assert.That(XmlErrorCodes.XML1110.CoreCode).IsEqualTo(CoreDiagnosticCodes.EntityDepthLimitExceeded);
    }

    [Test]
    public async Task All_MatchesTheDeclaredFieldsAndToStringIsTheAlias()
    {
        var fields = new List<XmlErrorCode>();
        foreach (var field in typeof(XmlErrorCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.FieldType == typeof(XmlErrorCode))
            {
                var code = (XmlErrorCode)field.GetValue(null)!;
                fields.Add(code);
                await Assert.That(code.Alias).IsEqualTo(field.Name);
                await Assert.That(code.ToString()).IsEqualTo(field.Name);
            }
        }

        await Assert.That(fields).IsEquivalentTo(XmlErrorCodes.All);
    }
}
