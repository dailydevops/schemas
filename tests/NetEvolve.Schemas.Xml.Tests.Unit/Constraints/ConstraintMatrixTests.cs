namespace NetEvolve.Schemas.Xml.Tests.Unit.Constraints;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

/// <summary>
/// The coverage matrix of the well-formedness, validity, and namespace constraints, checked against the error tables and the
/// tests of this assembly. A constraint code of an error table without an entry, an entry without a positive or a negative
/// test, and a mapping to a test that does not exist each turn a test of this class red.
/// </summary>
public class ConstraintMatrixTests
{
    // XML 1.0 (Fifth Edition): the twelve well-formedness constraints by the name the specification labels them with.
    private static readonly string[] WellFormednessNames =
    [
        "External Subset",
        "PE Between Declarations",
        "PEs in Internal Subset",
        "Element Type Match",
        "Unique Att Spec",
        "No External Entity References",
        "No < in Attribute Values",
        "Legal Character",
        "Entity Declared",
        "Parsed Entity",
        "No Recursion",
        "In DTD",
    ];

    // XML 1.0 (Fifth Edition): the 26 validity constraints in specification order (design #122, section 6).
    private static readonly string[] ValidityNames =
    [
        "Root Element Type",
        "Proper Declaration/PE Nesting",
        "Standalone Document Declaration",
        "Element Valid",
        "Attribute Value Type",
        "Unique Element Type Declaration",
        "Proper Group/PE Nesting",
        "No Duplicate Types",
        "ID",
        "One ID per Element Type",
        "ID Attribute Default",
        "IDREF",
        "Entity Name",
        "Name Token",
        "Notation Attributes",
        "One Notation Per Element Type",
        "No Notation on Empty Element",
        "No Duplicate Tokens",
        "Enumeration",
        "Required Attribute",
        "Attribute Default Value Syntactically Correct",
        "Fixed Attribute Default",
        "Proper Conditional Section/PE Nesting",
        "Entity Declared",
        "Notation Declared",
        "Unique Notation Name",
    ];

    // Namespaces in XML 1.0 (Third Edition): the four namespace constraints.
    private static readonly string[] NamespaceNames =
    [
        "Reserved Prefixes and Namespace Names",
        "No Prefix Undeclaring",
        "Prefix Declared",
        "Attributes Unique",
    ];

    // The gaps that are recorded, each with its tracking issue; a gap that is not in this table is silent.
    private static readonly (string Key, string Issue)[] RecordedGaps =
    [
        ("Validity/Proper Declaration/PE Nesting", "#608"),
        ("Validity/Unique Element Type Declaration", "#608"),
        ("Validity/Proper Group/PE Nesting", "#608"),
        ("Validity/Proper Conditional Section/PE Nesting", "#608"),
        ("Validity/Entity Declared", "#647, #935"),
    ];

    private static readonly Lazy<IReadOnlyList<ConstraintViolation>> Violations = new(() =>
        ConstraintMatrixChecker.Check(
            ConstraintMatrix.Entries,
            ConstraintCodeTables.Load(),
            ConstraintTestIndex.Exists,
            NoSuiteCatalog
        )
    );

    /// <summary>Gets the aliases of the constraint codes of the error tables.</summary>
    /// <returns>The aliases.</returns>
    public static IEnumerable<string> ConstraintCodes() =>
        ConstraintCodeTables.Load().Where(code => code.Kind is not null).Select(code => code.Alias);

    /// <summary>Gets the keys of the entries of the matrix.</summary>
    /// <returns>The keys.</returns>
    public static IEnumerable<string> EntryKeys() => ConstraintMatrix.Entries.Select(entry => entry.Key);

    // The unit leg has no catalog of the conformance suite (the Integration project reads it), so a mapped xmlconf case cannot be verified here and is reported as unknown.
    private static bool NoSuiteCatalog(string caseId) => false;

    private static async Task AssertNone(string subject, params ConstraintViolationKind[] kinds)
    {
        var found = Violations
            .Value.Where(v => string.Equals(v.Subject, subject, StringComparison.Ordinal) && kinds.Contains(v.Kind))
            .ToArray();

        await Assert.That(string.Join(Environment.NewLine, found)).IsEqualTo(string.Empty);
    }

    [Test]
    [MethodDataSource(nameof(ConstraintCodes))]
    public async Task Code_HasAnEntryInTheMatrix(string alias) =>
        await AssertNone(alias, ConstraintViolationKind.MissingEntry);

    [Test]
    [MethodDataSource(nameof(EntryKeys))]
    public async Task Entry_HasAPositiveAndANegativeTest_AndAGapNamesItsIssuesAndProbes(string key) =>
        await AssertNone(
            key,
            ConstraintViolationKind.MissingPositive,
            ConstraintViolationKind.MissingNegative,
            ConstraintViolationKind.InvalidGap
        );

    [Test]
    [MethodDataSource(nameof(EntryKeys))]
    public async Task Entry_MapsOnlyTestsAndCasesThatExist(string key) =>
        await AssertNone(
            key,
            ConstraintViolationKind.UnknownTest,
            ConstraintViolationKind.UnknownXmlConfCase,
            ConstraintViolationKind.InvalidReference
        );

    [Test]
    [MethodDataSource(nameof(EntryKeys))]
    public async Task Entry_AgreesWithTheErrorTables(string key) =>
        await AssertNone(
            key,
            ConstraintViolationKind.UnknownCode,
            ConstraintViolationKind.Mismatch,
            ConstraintViolationKind.DuplicateEntry
        );

    [Test]
    public async Task Matrix_HasNoViolation() =>
        await Assert.That(string.Join(Environment.NewLine, Violations.Value)).IsEqualTo(string.Empty);

    [Test]
    public async Task Matrix_ListsEveryConstraintOfTheSpecificationsOnceByItsOfficialName()
    {
        var expected = WellFormednessNames
            .Select(name => ConstraintKind.WellFormedness + "/" + name)
            .Concat(ValidityNames.Select(name => ConstraintKind.Validity + "/" + name))
            .Concat(NamespaceNames.Select(name => ConstraintKind.Namespace + "/" + name))
            .ToArray();

        var actual = ConstraintMatrix.Entries.Select(entry => entry.Key).ToArray();

        await Assert.That(actual).IsEquivalentTo(expected);
        await Assert
            .That(actual.Length)
            .IsEqualTo(WellFormednessNames.Length + ValidityNames.Length + NamespaceNames.Length);
    }

    [Test]
    public async Task Matrix_GapsAreExactlyTheRecordedOnes_EachWithItsIssue()
    {
        var actual = ConstraintMatrix
            .Entries.Where(entry => entry.Gap is not null)
            .Select(entry => (entry.Key, string.Join(", ", entry.Gap!.Issues)))
            .ToArray();

        await Assert.That(actual).IsEquivalentTo(RecordedGaps);
    }

    [Test]
    public async Task Matrix_ReportsEveryEntryAsTestOutput(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var lines = ConstraintMatrixReport.Render(ConstraintMatrix.Entries);
        var report = string.Join(Environment.NewLine, lines);
        await TestContext.Current!.OutputWriter.WriteLineAsync(report.AsMemory(), cancellationToken);

        await Assert.That(lines.Count).IsEqualTo(ConstraintMatrix.Entries.Count + 1);
        await Assert.That(lines[^1]).Contains(ConstraintMatrix.Entries.Count + " constraints");
    }
}
