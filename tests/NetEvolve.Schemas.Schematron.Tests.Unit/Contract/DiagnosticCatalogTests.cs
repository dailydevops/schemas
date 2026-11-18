namespace NetEvolve.Schemas.Schematron.Tests.Unit.Contract;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

public class DiagnosticCatalogTests
{
    private static readonly Dictionary<string, string> ExplicitSymbolicNames = new(StringComparer.Ordinal)
    {
        ["AbstractTargetInvalid"] = "SCH-ABSTRACT-INVALID",
        ["DocumentDenied"] = "SCH-DOC-DENIED",
        ["ExpressionInvalid"] = "SCH-EXPR-INVALID",
        ["VariableUndefined"] = "SCH-VAR-UNDEFINED",
        ["EvaluationFailed"] = "SCH-EVAL-FAILED",
        ["AssertionFailed"] = "SCH-ASSERT-FAILED",
        ["EvaluationLimitStop"] = "SCH-EVAL-LIMIT-STOP",
    };

    private static readonly Dictionary<int, string> TopicOfBlock = new()
    {
        [30] = "schema structure and selection",
        [31] = "modules, documents, and expansion",
        [32] = "declarations and static expressions",
        [33] = "variables and dynamic evaluation",
        [34] = "assertion findings",
        [35] = "report output",
    };

    private static List<(string Field, DiagnosticCode Code)> Catalog() =>
        [
            .. typeof(SchematronDiagnosticCodes)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.FieldType == typeof(DiagnosticCode))
                .Select(field => (field.Name, (DiagnosticCode)field.GetValue(null)!))
                .OrderBy(entry => entry.Item2.Number),
        ];

    [Test]
    public async Task Catalog_AllCodes_LieInSchematronRange()
    {
        foreach (var (_, code) in Catalog())
        {
            await Assert.That(DiagnosticRanges.Schematron.Contains(code)).IsTrue();
        }
    }

    [Test]
    public async Task Catalog_NoOverlapWithCoreCodes()
    {
        foreach (var (_, code) in Catalog())
        {
            await Assert.That(code.Number is >= 1 and <= 7).IsFalse();
            await Assert.That(code.Number is >= 100 and <= 103).IsFalse();
            await Assert.That(DiagnosticRanges.Core.Contains(code)).IsFalse();
        }
    }

    [Test]
    public async Task Catalog_CodeBlock_MatchesDeclaredTopic()
    {
        foreach (var (field, code) in Catalog())
        {
            await Assert.That(TopicOfBlock.ContainsKey(code.Number / 100)).IsTrue();
            await Assert
                .That(RecordedCodes.Rows.Single(row => string.Equals(row.Field, field, StringComparison.Ordinal)).Topic)
                .IsEqualTo(TopicOfBlock[code.Number / 100]);
        }
    }

    [Test]
    public async Task Catalog_RecordedList_FailsOnRenameRemovalDuplicate()
    {
        var actual = Catalog().Select(entry => entry.Field + "=" + entry.Code.Number).ToArray();
        var recorded = RecordedCodes.Rows.Select(row => row.Field + "=" + row.Number).ToArray();

        await Assert.That(actual).IsEquivalentTo(recorded);
        await Assert.That(actual.Distinct(StringComparer.Ordinal).Count()).IsEqualTo(actual.Length);
        await Assert.That(Catalog().Select(entry => entry.Code.Number).Distinct().Count()).IsEqualTo(actual.Length);
    }

    [Test]
    public async Task Catalog_EveryEntry_HasSymbolicName()
    {
        foreach (var row in RecordedCodes.Rows)
        {
            await Assert.That(SymbolicNameOf(row.Field)).IsEqualTo(row.SymbolicName);
            await Assert.That(row.SymbolicName.StartsWith("SCH-", StringComparison.Ordinal)).IsTrue();
        }
    }

    [Test]
    public async Task Catalog_StopCodes_AreNes3502IssueLimitAndNes3503EvaluationLimit()
    {
        var numbers = Catalog().Select(entry => entry.Code.Number).ToArray();

        await Assert.That(SchematronDiagnosticCodes.IssueLimitStop.ToString()).IsEqualTo("NES3502");
        await Assert.That(SchematronDiagnosticCodes.EvaluationLimitStop.ToString()).IsEqualTo("NES3503");
        await Assert.That(numbers.Max()).IsEqualTo(3503);
        await Assert.That(numbers.Contains(3504)).IsFalse();
    }

    [Test]
    public async Task Catalog_NotImplemented_IsNes3001()
    {
        await Assert.That(SchematronDiagnosticCodes.NotImplemented.ToString()).IsEqualTo("NES3001");
        await Assert.That(RecordedCodes.Rows.Count).IsEqualTo(Catalog().Count);
    }

    private static string SymbolicNameOf(string field)
    {
        if (ExplicitSymbolicNames.TryGetValue(field, out var explicitName))
        {
            return explicitName;
        }

        var builder = new StringBuilder("SCH");
        foreach (var character in field)
        {
            _ = builder.Append(char.IsUpper(character) ? "-" : string.Empty).Append(char.ToUpperInvariant(character));
        }

        return builder.ToString();
    }
}
