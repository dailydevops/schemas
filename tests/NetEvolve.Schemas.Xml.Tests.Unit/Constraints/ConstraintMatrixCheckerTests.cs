namespace NetEvolve.Schemas.Xml.Tests.Unit.Constraints;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Self-tests of the coverage matrix check over a small fixture: the check must fail when a mapping is missing, so a green
/// run of the real matrix means something. Each test breaks the complete fixture in exactly one way.
/// </summary>
public class ConstraintMatrixCheckerTests
{
    private static readonly ConstraintCode[] Codes =
    [
        new("XML0001", ConstraintKind.WellFormedness, "Alpha", string.Empty),
        new("XML5001", ConstraintKind.Validity, "Beta", "2.8"),
        new("XML3001", ConstraintKind.Namespace, "Gamma", string.Empty),
        new("XML0002", null, "Production: Char", string.Empty),
    ];

    private static readonly string[] ConstraintCodeAliases = ["XML0001", "XML5001", "XML3001"];

    private static readonly ConstraintViolationKind[] MissingBothSides =
    [
        ConstraintViolationKind.MissingPositive,
        ConstraintViolationKind.MissingNegative,
    ];

    private static readonly HashSet<string> Tests = new(StringComparer.Ordinal)
    {
        "Fixture.Alpha_Accepts",
        "Fixture.Alpha_Rejects",
        "Fixture.Beta_Accepts",
        "Fixture.Beta_Rejects",
        "Fixture.Gamma_Accepts",
        "Fixture.Gamma_Rejects",
        "Fixture.Gamma_Probe",
    };

    private static readonly HashSet<string> Cases = new(StringComparer.Ordinal)
    {
        "sun/valid-case",
        "sun/invalid-case",
    };

    private static ConstraintEntry Alpha() =>
        new(
            ConstraintKind.WellFormedness,
            "Alpha",
            "3",
            "XML0001",
            ["Fixture.Alpha_Accepts"],
            ["Fixture.Alpha_Rejects"]
        );

    private static ConstraintEntry Beta() =>
        new(ConstraintKind.Validity, "Beta", "2.8", "XML5001", ["Fixture.Beta_Accepts"], ["Fixture.Beta_Rejects"]);

    private static ConstraintEntry Gamma() =>
        new(ConstraintKind.Namespace, "Gamma", "6.3", "XML3001", ["Fixture.Gamma_Accepts"], ["Fixture.Gamma_Rejects"]);

    private static ConstraintGap Gap(string issue, string probe) => new([issue], [probe]);

    private static ConstraintEntry[] Complete() => [Alpha(), Beta(), Gamma()];

    private static IReadOnlyList<ConstraintViolation> Check(params ConstraintEntry[] entries) =>
        ConstraintMatrixChecker.Check(entries, Codes, Tests.Contains, Cases.Contains);

    private static string Describe(IReadOnlyList<ConstraintViolation> violations) =>
        string.Join(Environment.NewLine, violations);

    [Test]
    public async Task Check_CompleteFixture_HasNoViolation()
    {
        var violations = Check(Complete());

        await Assert.That(Describe(violations)).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Check_RemovingTheEntryOfAConstraintCode_FailsForThatCode()
    {
        var violations = Check(Alpha(), Gamma());

        await Assert.That(violations.Count).IsEqualTo(1);
        await Assert.That(violations[0].Kind).IsEqualTo(ConstraintViolationKind.MissingEntry);
        await Assert.That(violations[0].Subject).IsEqualTo("XML5001");
    }

    [Test]
    public async Task Check_NoEntryAtAll_FailsForEveryConstraintCodeAndNotForOtherCodes()
    {
        var violations = Check();

        await Assert.That(violations.Select(v => v.Subject).ToArray()).IsEquivalentTo(ConstraintCodeAliases);
    }

    [Test]
    public async Task Check_RemovingThePositiveMapping_FailsWithMissingPositive()
    {
        var violations = Check(Alpha() with { Positive = [] }, Beta(), Gamma());

        await Assert.That(violations.Count).IsEqualTo(1);
        await Assert.That(violations[0].Kind).IsEqualTo(ConstraintViolationKind.MissingPositive);
        await Assert.That(violations[0].Subject).IsEqualTo("WellFormedness/Alpha");
    }

    [Test]
    public async Task Check_RemovingTheNegativeMapping_FailsWithMissingNegative()
    {
        var violations = Check(Alpha(), Beta() with { Negative = [] }, Gamma());

        await Assert.That(violations.Count).IsEqualTo(1);
        await Assert.That(violations[0].Kind).IsEqualTo(ConstraintViolationKind.MissingNegative);
        await Assert.That(violations[0].Subject).IsEqualTo("Validity/Beta");
    }

    [Test]
    public async Task Check_MappedTestThatDoesNotExist_FailsWithUnknownTest()
    {
        var violations = Check(Alpha(), Beta(), Gamma() with { Negative = ["Fixture.Gamma_Renamed"] });

        await Assert.That(violations.Count).IsEqualTo(1);
        await Assert.That(violations[0].Kind).IsEqualTo(ConstraintViolationKind.UnknownTest);
        await Assert.That(violations[0].Message).Contains("Fixture.Gamma_Renamed");
    }

    [Test]
    public async Task Check_MappedXmlConfCaseThatDoesNotExist_FailsWithUnknownXmlConfCase()
    {
        var violations = Check(Alpha(), Beta() with { Negative = ["xmlconf:sun/missing-case"] }, Gamma());

        await Assert.That(violations.Count).IsEqualTo(1);
        await Assert.That(violations[0].Kind).IsEqualTo(ConstraintViolationKind.UnknownXmlConfCase);
        await Assert.That(violations[0].Message).Contains("sun/missing-case");
    }

    [Test]
    public async Task Check_XmlConfCasesThatExist_CountAsEvidence()
    {
        var beta = Beta() with { Positive = ["xmlconf:sun/valid-case"], Negative = ["xmlconf:sun/invalid-case"] };

        var violations = Check(Alpha(), beta, Gamma());

        await Assert.That(Describe(violations)).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Check_EmptyReference_FailsWithInvalidReference()
    {
        var violations = Check(Alpha() with { Positive = ["Fixture.Alpha_Accepts", " "] }, Beta(), Gamma());

        await Assert.That(violations.Count).IsEqualTo(1);
        await Assert.That(violations[0].Kind).IsEqualTo(ConstraintViolationKind.InvalidReference);
    }

    [Test]
    public async Task Check_EmptyXmlConfCaseId_FailsWithInvalidReference()
    {
        var violations = Check(Alpha() with { Negative = ["xmlconf:"] }, Beta(), Gamma());

        await Assert.That(violations.Select(v => v.Kind).ToArray()).Contains(ConstraintViolationKind.InvalidReference);
    }

    [Test]
    public async Task Check_TwoEntriesForTheSameConstraint_FailsWithDuplicateEntry()
    {
        var violations = Check(Alpha(), Alpha(), Beta(), Gamma());

        await Assert.That(violations.Count).IsEqualTo(1);
        await Assert.That(violations[0].Kind).IsEqualTo(ConstraintViolationKind.DuplicateEntry);
    }

    [Test]
    public async Task Check_SameNameInAnotherFamily_IsNotADuplicate()
    {
        var violations = Check(Alpha(), Beta(), Gamma(), Gamma() with { Name = "Alpha" });

        await Assert
            .That(violations.Select(v => v.Kind).ToArray())
            .DoesNotContain(ConstraintViolationKind.DuplicateEntry);
    }

    [Test]
    public async Task Check_EntryWithACodeOfNoTable_FailsWithUnknownCode()
    {
        var violations = Check(Alpha(), Beta(), Gamma() with { Code = "XML9999" });

        await Assert.That(violations.Select(v => v.Kind).ToArray()).Contains(ConstraintViolationKind.UnknownCode);
    }

    [Test]
    public async Task Check_EntryNamedAnotherThanItsCode_FailsWithMismatch()
    {
        var violations = Check(Alpha() with { Name = "Alpa" }, Beta(), Gamma());

        await Assert.That(violations.Select(v => v.Kind).ToArray()).Contains(ConstraintViolationKind.Mismatch);
    }

    [Test]
    public async Task Check_EntryInAnotherFamilyThanItsCode_FailsWithMismatch()
    {
        var violations = Check(Alpha() with { Kind = ConstraintKind.Namespace }, Beta(), Gamma());

        await Assert.That(violations.Select(v => v.Kind).ToArray()).Contains(ConstraintViolationKind.Mismatch);
    }

    [Test]
    public async Task Check_EntryInAnotherSectionThanItsCode_FailsWithMismatch()
    {
        var violations = Check(Alpha(), Beta() with { Section = "9.9" }, Gamma());

        await Assert.That(violations.Select(v => v.Kind).ToArray()).Contains(ConstraintViolationKind.Mismatch);
    }

    [Test]
    public async Task Check_EntryForACodeThatIsNoConstraint_NeedsNoNameFromTheTable()
    {
        var entry = new ConstraintEntry(
            ConstraintKind.WellFormedness,
            "Delta",
            "4.1",
            "XML0002",
            ["Fixture.Alpha_Accepts"],
            ["Fixture.Alpha_Rejects"]
        );

        var violations = Check(Alpha(), Beta(), Gamma(), entry);

        await Assert.That(Describe(violations)).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Check_GapWithTestsAnIssueAndAnExistingProbe_HasNoViolation()
    {
        var gamma = Gamma() with { Gap = Gap("#608", "Fixture.Gamma_Probe") };

        var violations = Check(Alpha(), Beta(), gamma);

        await Assert.That(Describe(violations)).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Check_GapWithoutPositiveAndNegativeTests_StillFailsWithMissingPositiveAndMissingNegative()
    {
        var gamma = Gamma() with { Positive = [], Negative = [], Gap = Gap("#608", "Fixture.Gamma_Probe") };

        var violations = Check(Alpha(), Beta(), gamma);

        await Assert.That(violations.Select(v => v.Kind).ToArray()).IsEquivalentTo(MissingBothSides);
    }

    [Test]
    public async Task Check_GapWithoutAnIssue_FailsWithInvalidGap()
    {
        var gamma = Gamma() with { Gap = Gap("later", "Fixture.Gamma_Probe") };

        var violations = Check(Alpha(), Beta(), gamma);

        await Assert.That(violations.Count).IsEqualTo(1);
        await Assert.That(violations[0].Kind).IsEqualTo(ConstraintViolationKind.InvalidGap);
    }

    [Test]
    public async Task Check_GapWithNoIssueAtAll_FailsWithInvalidGap()
    {
        var gamma = Gamma() with { Gap = new ConstraintGap([], ["Fixture.Gamma_Probe"]) };

        var violations = Check(Alpha(), Beta(), gamma);

        await Assert.That(violations.Count).IsEqualTo(1);
        await Assert.That(violations[0].Kind).IsEqualTo(ConstraintViolationKind.InvalidGap);
    }

    [Test]
    public async Task Check_GapWhoseProbeDoesNotExist_FailsWithInvalidGap()
    {
        var gamma = Gamma() with { Gap = Gap("#608", "Fixture.Gamma_Gone") };

        var violations = Check(Alpha(), Beta(), gamma);

        await Assert.That(violations.Count).IsEqualTo(1);
        await Assert.That(violations[0].Kind).IsEqualTo(ConstraintViolationKind.InvalidGap);
    }

    [Test]
    public async Task Check_GapWithNoProbeAtAll_FailsWithInvalidGap()
    {
        var gamma = Gamma() with { Gap = new ConstraintGap(["#608"], []) };

        var violations = Check(Alpha(), Beta(), gamma);

        await Assert.That(violations.Count).IsEqualTo(1);
        await Assert.That(violations[0].Kind).IsEqualTo(ConstraintViolationKind.InvalidGap);
    }

    [Test]
    public async Task Check_GapWithSeveralIssuesAndProbes_ChecksEachOfThem()
    {
        var valid = Gamma() with
        {
            Gap = new ConstraintGap(["#608", "#935"], ["Fixture.Gamma_Probe", "Fixture.Alpha_Accepts"]),
        };
        var broken = Gamma() with
        {
            Gap = new ConstraintGap(["#608", "935"], ["Fixture.Gamma_Probe", "Fixture.Gamma_Gone"]),
        };

        await Assert.That(Describe(Check(Alpha(), Beta(), valid))).IsEqualTo(string.Empty);
        await Assert.That(Check(Alpha(), Beta(), broken).Count).IsEqualTo(2);
    }

    [Test]
    public async Task Check_GapThatListsATestThatDoesNotExist_StillFailsWithUnknownTest()
    {
        var gamma = Gamma() with { Negative = ["Fixture.Gamma_Gone"], Gap = Gap("#608", "Fixture.Gamma_Probe") };

        var violations = Check(Alpha(), Beta(), gamma);

        await Assert.That(violations.Select(v => v.Kind).ToArray()).Contains(ConstraintViolationKind.UnknownTest);
    }

    [Test]
    public async Task Report_ListsEveryEntryWithItsStatus_AndSummarizesTheGaps()
    {
        var gamma = Gamma() with { Gap = Gap("#608", "Fixture.Gamma_Probe") };

        var lines = ConstraintMatrixReport.Render([Alpha(), Beta(), gamma]);

        await Assert.That(lines.Count).IsEqualTo(4);
        await Assert.That(lines[0]).Contains("WellFormedness").And.Contains("Alpha").And.Contains("XML0001");
        await Assert.That(lines[0]).Contains("covered");
        await Assert.That(lines[2]).Contains("Namespace").And.Contains("Gamma").And.Contains("gap #608");
        await Assert.That(lines[3]).Contains("3 constraints").And.Contains("2 covered").And.Contains("1 gap");
    }

    [Test]
    public async Task Report_NamesEveryIssueOfAGap()
    {
        var gamma = Gamma() with { Gap = new ConstraintGap(["#647", "#935"], ["Fixture.Gamma_Probe"]) };

        var lines = ConstraintMatrixReport.Render([gamma]);

        await Assert.That(lines[0]).Contains("gap #647, #935");
    }
}
