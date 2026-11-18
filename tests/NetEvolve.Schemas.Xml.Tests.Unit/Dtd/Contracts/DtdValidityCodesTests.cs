namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Contracts;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;

public class DtdValidityCodesTests
{
    // The 26 validity constraints of XML 1.0 (Fifth Edition) by their official names, in specification order,
    // with the section and the owning step as approved in design #122, section 6.
    private static readonly (string Alias, string Name, string Section, string Owner)[] Constraints =
    [
        ("XML5001", "Root Element Type", "2.8", "XML-67"),
        ("XML5002", "Proper Declaration/PE Nesting", "2.8", "XML-67"),
        ("XML5003", "Standalone Document Declaration", "2.9", "XML-67"),
        ("XML5004", "Element Valid", "3", "XML-63"),
        ("XML5005", "Attribute Value Type", "3.1", "XML-64"),
        ("XML5006", "Unique Element Type Declaration", "3.2", "XML-67"),
        ("XML5007", "Proper Group/PE Nesting", "3.2.1", "XML-67"),
        ("XML5008", "No Duplicate Types", "3.2.2", "XML-67"),
        ("XML5009", "ID", "3.3.1", "XML-65"),
        ("XML5010", "One ID per Element Type", "3.3.1", "XML-65"),
        ("XML5011", "ID Attribute Default", "3.3.1", "XML-65"),
        ("XML5012", "IDREF", "3.3.1", "XML-65"),
        ("XML5013", "Entity Name", "3.3.1", "XML-66"),
        ("XML5014", "Name Token", "3.3.1", "XML-64"),
        ("XML5015", "Notation Attributes", "3.3.1", "XML-66"),
        ("XML5016", "One Notation Per Element Type", "3.3.1", "XML-66"),
        ("XML5017", "No Notation on Empty Element", "3.3.1", "XML-66"),
        ("XML5018", "No Duplicate Tokens", "3.3.1", "XML-64"),
        ("XML5019", "Enumeration", "3.3.1", "XML-64"),
        ("XML5020", "Required Attribute", "3.3.2", "XML-64"),
        ("XML5021", "Attribute Default Value Syntactically Correct", "3.3.2", "XML-64"),
        ("XML5022", "Fixed Attribute Default", "3.3.2", "XML-64"),
        ("XML5023", "Proper Conditional Section/PE Nesting", "3.4", "XML-67"),
        ("XML5024", "Entity Declared", "4.1", "XML-67"),
        ("XML5025", "Notation Declared", "4.2.2", "XML-66"),
        ("XML5026", "Unique Notation Name", "4.7", "XML-66"),
    ];

    // The optional reports that are not validity constraints (design #122, section 7).
    private static readonly (string Alias, string Section, string Owner, IssueSeverity Severity)[] Reports =
    [
        ("XML5901", "3.2", "XML-67", IssueSeverity.Warning),
        ("XML5902", "3.3", "XML-64", IssueSeverity.Warning),
        ("XML5903", "3.3", "XML-64", IssueSeverity.Warning),
        ("XML5904", "4.2", "XML-66", IssueSeverity.Warning),
        ("XML5910", "2.8", "XML-67", IssueSeverity.Error),
        ("XML5911", "3.2.1", "XML-63", IssueSeverity.Warning),
    ];

    // The limits added by design #122, section 9; exceeding one is a fatal status, never an issue.
    private static readonly (string Alias, string Name)[] Limits =
    [
        ("XML1117", "Limit: MaxExternalEntityRequests"),
        ("XML1118", "Limit: MaxContentModelStates"),
        ("XML1119", "Limit: MaxDeclarations"),
    ];

    private static int Number(string alias) => int.Parse(alias.AsSpan(3), CultureInfo.InvariantCulture);

    [Test]
    public async Task Codes_EveryVc_HasExactlyOneUniqueCode()
    {
        var actual = new List<(string, string, string, string)>();
        var aliases = new HashSet<string>(StringComparer.Ordinal);
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var code in DtdValidityCodes.Constraints)
        {
            actual.Add((code.Alias, code.Name, code.Section, code.Owner));
            aliases.Add(code.Alias);
            names.Add(code.Name);
        }

        await Assert.That(actual).IsEquivalentTo(Constraints);
        await Assert.That(actual).Count().IsEqualTo(26);
        await Assert.That(aliases).Count().IsEqualTo(26);
        await Assert.That(names).Count().IsEqualTo(26);
    }

    [Test]
    public async Task Codes_NoCodeWithoutConstraint()
    {
        var official = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (_, name, _, _) in Constraints)
        {
            official.Add(name);
        }

        var withoutConstraint = new List<string>();
        foreach (var code in DtdValidityCodes.All)
        {
            if (code.Kind == DtdValidityCodeKind.ValidityConstraint && !official.Contains(code.Name))
            {
                withoutConstraint.Add(code.Alias);
            }
        }

        await Assert.That(withoutConstraint).IsEmpty();
    }

    [Test]
    public async Task Codes_EveryConstraintRow_IsKindValidityConstraintWithErrorSeverity()
    {
        var failures = new List<string>();
        foreach (var code in DtdValidityCodes.Constraints)
        {
            if (code.Kind != DtdValidityCodeKind.ValidityConstraint || code.DefaultSeverity != IssueSeverity.Error)
            {
                failures.Add(code.Alias);
            }
        }

        await Assert.That(failures).IsEmpty();
    }

    [Test]
    public async Task Codes_OwningStepTotals_AreOneSevenFourSixEight()
    {
        var totals = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var code in DtdValidityCodes.Constraints)
        {
            totals[code.Owner] = totals.TryGetValue(code.Owner, out var count) ? count + 1 : 1;
        }

        await Assert.That(totals["XML-63"]).IsEqualTo(1);
        await Assert.That(totals["XML-64"]).IsEqualTo(7);
        await Assert.That(totals["XML-65"]).IsEqualTo(4);
        await Assert.That(totals["XML-66"]).IsEqualTo(6);
        await Assert.That(totals["XML-67"]).IsEqualTo(8);
        await Assert.That(totals).Count().IsEqualTo(5);
    }

    [Test]
    public async Task Codes_NonVcReports_AreInSeparateTable()
    {
        var actual = new List<(string, string, string, IssueSeverity)>();
        var misplaced = new List<string>();
        foreach (var code in DtdValidityCodes.Reports)
        {
            actual.Add((code.Alias, code.Section, code.Owner, code.DefaultSeverity));
            if (code.Kind != DtdValidityCodeKind.Report)
            {
                misplaced.Add(code.Alias);
            }
        }

        foreach (var code in DtdValidityCodes.Constraints)
        {
            if (code.Kind == DtdValidityCodeKind.Report)
            {
                misplaced.Add(code.Alias);
            }
        }

        await Assert.That(actual).IsEquivalentTo(Reports);
        await Assert.That(misplaced).IsEmpty();
    }

    [Test]
    public async Task Codes_Limits_AreReaderTableRowsAndNeverIssues()
    {
        var actual = new List<(string, string)>();
        foreach (var code in DtdLimitCodes.All)
        {
            actual.Add((code.Alias, code.Constraint));
            await Assert.That(code.Message).IsNotEmpty();
        }
        await Assert.That(actual).IsEquivalentTo(Limits);
        await Assert.That(typeof(DtdLimitCodes).GetFields().Length).IsEqualTo(3);
        await Assert.That(DtdLimitCodes.XML1117.Code).IsEqualTo(new DiagnosticCode(1117));
        await Assert.That(DtdLimitCodes.XML1119.Code).IsEqualTo(new DiagnosticCode(1119));
    }

    [Test]
    public async Task Codes_AreStableSnapshot()
    {
        var mismatches = new List<string>();
        foreach (var code in DtdValidityCodes.All)
        {
            var number = Number(code.Alias);
            var expected = code.Kind switch
            {
                DtdValidityCodeKind.ValidityConstraint => 1500 + (number - 5000),
                DtdValidityCodeKind.Report => 1600 + (number - 5900),
                _ => throw new InvalidOperationException(code.Alias),
            };
            if (code.Code.Number != expected)
            {
                mismatches.Add(code.Alias + " -> " + code.Code);
            }
        }

        await Assert.That(mismatches).IsEmpty();
        await Assert.That(DtdValidityCodes.XML5001.Code).IsEqualTo(new DiagnosticCode(1501));
        await Assert.That(DtdValidityCodes.XML5026.Code).IsEqualTo(new DiagnosticCode(1526));
        await Assert.That(DtdValidityCodes.XML5901.Code).IsEqualTo(new DiagnosticCode(1601));
        await Assert.That(DtdValidityCodes.XML5910.Code).IsEqualTo(new DiagnosticCode(1610));
        await Assert.That(DtdValidityCodes.XML5911.Code).IsEqualTo(new DiagnosticCode(1611));
    }

    [Test]
    public async Task Codes_NeverCollideWithTheXmlTable_AndLieInTheDtdBands()
    {
        var aliases = new HashSet<string>(StringComparer.Ordinal);
        var numbers = new HashSet<int>();
        foreach (var code in XmlErrorCodes.All)
        {
            aliases.Add(code.Alias);
            numbers.Add(code.Code.Number);
        }

        foreach (var code in DtdLimitCodes.All)
        {
            aliases.Add(code.Alias);
            numbers.Add(code.Code.Number);
        }
        var collisions = new List<string>();
        var outOfRange = new List<string>();
        foreach (var code in DtdValidityCodes.All)
        {
            if (!aliases.Add(code.Alias) || !numbers.Add(code.Code.Number))
            {
                collisions.Add(code.Alias);
            }

            var inBand = code.Code.Number is >= 1500 and <= 1799;
            if (!inBand || !DiagnosticRanges.Xml.Contains(code.Code))
            {
                outOfRange.Add(code.Alias);
            }
        }

        await Assert.That(collisions).IsEmpty();
        await Assert.That(outOfRange).IsEmpty();
        await Assert.That(DtdValidityCodes.All).Count().IsEqualTo(Constraints.Length + Reports.Length);
    }

    [Test]
    public async Task Codes_HaveAMessageAndANameAndToStringIsTheAlias()
    {
        var missing = new List<string>();
        foreach (var code in DtdValidityCodes.All)
        {
            if (string.IsNullOrWhiteSpace(code.Message) || string.IsNullOrWhiteSpace(code.Name))
            {
                missing.Add(code.Alias);
            }
        }

        await Assert.That(missing).IsEmpty();
        await Assert.That(DtdValidityCodes.XML5004.ToString()).IsEqualTo("XML5004");
    }

    [Test]
    public async Task All_MatchesTheDeclaredFields()
    {
        var fields = new List<DtdValidityCode>();
        foreach (var field in typeof(DtdValidityCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.FieldType == typeof(DtdValidityCode))
            {
                var code = (DtdValidityCode)field.GetValue(null)!;
                fields.Add(code);
                await Assert.That(code.Alias).IsEqualTo(field.Name);
            }
        }

        await Assert.That(fields).IsEquivalentTo(DtdValidityCodes.All);
    }
}
