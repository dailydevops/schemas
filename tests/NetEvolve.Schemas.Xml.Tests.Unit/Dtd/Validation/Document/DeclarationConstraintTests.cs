namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Document;

using System;
using NetEvolve.Schemas;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Model;
using NetEvolve.Schemas.Xml.Dtd.Validation.Document;
using static NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Document.DocumentHarness;

/// <summary>
/// The constraints that are decided when the declarations are complete: No Duplicate Types, and the four that depend on the
/// parse-time facts of design #122, D4 (Unique Element Type Declaration, Proper Declaration/PE Nesting, Proper Group/PE
/// Nesting, Proper Conditional Section/PE Nesting). Each has a valid counterpart that reports nothing.
/// </summary>
public class DeclarationConstraintTests
{
    private static readonly DocumentLocation Written = new(SystemId, 7, 9);

    private static DtdDeclarations Plain() => Declare("doc", [Element("doc")]);

    private static Session WithFacts(params DtdAnomaly[] facts) => new(new AnomalousDeclarations(Plain(), facts));

    // Unique Element Type Declaration (section 3.2).

    [Test]
    public async Task UniqueElementType_SecondDeclarationOfAnElementType_ReportsXml5006AtItsLocation()
    {
        var session = WithFacts(new DtdAnomaly(DtdAnomalyKind.ElementRedeclared, Written, "doc"));

        await Assert.That(session.Result.Count).IsEqualTo(1);
        var issue = session.Result[0];
        await Assert.That(issue.Code).IsEqualTo(DtdValidityCodes.XML5006.Code);
        await Assert.That(issue.Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(issue.Constraint).IsEqualTo("Unique Element Type Declaration");
        await Assert.That(issue.Location).IsEqualTo(Written);
        await Assert.That(issue.Message).Contains("'doc'");
    }

    [Test]
    public async Task UniqueElementType_EachElementTypeDeclaredOnce_ReportsNothing()
    {
        var session = WithFacts();

        await Assert.That(session.Result.Count).IsEqualTo(0);
    }

    // Proper Declaration/PE Nesting (section 2.8).

    [Test]
    public async Task ProperDeclarationNesting_DeclarationAcrossReplacementTexts_ReportsXml5002AtItsLocation()
    {
        var session = WithFacts(new DtdAnomaly(DtdAnomalyKind.DeclarationNesting, Written));

        await Assert.That(session.Result.Count).IsEqualTo(1);
        var issue = session.Result[0];
        await Assert.That(issue.Code).IsEqualTo(DtdValidityCodes.XML5002.Code);
        await Assert.That(issue.Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(issue.Constraint).IsEqualTo("Proper Declaration/PE Nesting");
        await Assert.That(issue.Location).IsEqualTo(Written);
    }

    [Test]
    public async Task ProperDeclarationNesting_ModelWithoutFacts_ReportsNothing()
    {
        var session = new Session(Plain());

        await Assert.That(session.Result.Count).IsEqualTo(0);
    }

    // Proper Group/PE Nesting (section 3.2.1).

    [Test]
    public async Task ProperGroupNesting_GroupAcrossReplacementTexts_ReportsXml5007AtItsLocation()
    {
        var session = WithFacts(new DtdAnomaly(DtdAnomalyKind.GroupNesting, Written));

        await Assert.That(session.Result.Count).IsEqualTo(1);
        var issue = session.Result[0];
        await Assert.That(issue.Code).IsEqualTo(DtdValidityCodes.XML5007.Code);
        await Assert.That(issue.Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(issue.Constraint).IsEqualTo("Proper Group/PE Nesting");
        await Assert.That(issue.Location).IsEqualTo(Written);
    }

    [Test]
    public async Task ProperGroupNesting_OnlyOtherFacts_ReportsNothingForGroups()
    {
        var session = WithFacts(new DtdAnomaly(DtdAnomalyKind.DeclarationNesting, Written));

        await Assert.That(session.Result.Count).IsEqualTo(1);
        await Assert.That(session.Result[0].Code).IsNotEqualTo(DtdValidityCodes.XML5007.Code);
    }

    // Proper Conditional Section/PE Nesting (section 3.4).

    [Test]
    public async Task ProperConditionalSectionNesting_SectionAcrossReplacementTexts_ReportsXml5023AtItsLocation()
    {
        var session = WithFacts(new DtdAnomaly(DtdAnomalyKind.ConditionalSectionNesting, Written));

        await Assert.That(session.Result.Count).IsEqualTo(1);
        var issue = session.Result[0];
        await Assert.That(issue.Code).IsEqualTo(DtdValidityCodes.XML5023.Code);
        await Assert.That(issue.Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(issue.Constraint).IsEqualTo("Proper Conditional Section/PE Nesting");
        await Assert.That(issue.Location).IsEqualTo(Written);
    }

    [Test]
    public async Task ProperConditionalSectionNesting_ModelWithoutFacts_ReportsNothing()
    {
        var session = WithFacts();

        await Assert.That(session.Result.Count).IsEqualTo(0);
    }

    // The seam.

    [Test]
    public async Task Facts_WithoutALocation_FallBackToTheDocumentTypeLocation()
    {
        var session = WithFacts(new DtdAnomaly(DtdAnomalyKind.GroupNesting, default));

        await Assert.That(session.Result[0].Location).IsEqualTo(DoctypeLocation);
    }

    [Test]
    public async Task Facts_AreReportedInTheOrderTheyWereFound_AndBeforeAnyEvent()
    {
        var session = WithFacts(
            new DtdAnomaly(DtdAnomalyKind.ConditionalSectionNesting, Written),
            new DtdAnomaly(DtdAnomalyKind.ElementRedeclared, Written, "doc"),
            new DtdAnomaly(DtdAnomalyKind.DeclarationNesting, Written)
        );

        await Assert.That(session.Result.Count).IsEqualTo(3);
        await Assert.That(session.Result[0].Code).IsEqualTo(DtdValidityCodes.XML5023.Code);
        await Assert.That(session.Result[1].Code).IsEqualTo(DtdValidityCodes.XML5006.Code);
        await Assert.That(session.Result[2].Code).IsEqualTo(DtdValidityCodes.XML5002.Code);
    }

    [Test]
    public async Task Fact_WithAnUndefinedKind_IsRejected()
    {
        await Assert.That(() => new DtdAnomaly((DtdAnomalyKind)9, Written)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => new DtdAnomaly((DtdAnomalyKind)(-1), Written)).Throws<ArgumentOutOfRangeException>();
    }

    // No Duplicate Types (section 3.2.2).

    [Test]
    public async Task NoDuplicateTypes_RepeatedNameInMixedContent_ReportsXml5008AtTheDocumentType()
    {
        var model = Declare("doc", [Mixed("doc", "a", "b", "a"), Element("a"), Element("b")]);
        var session = new Session(model);

        await Assert.That(session.Result.Count).IsEqualTo(1);
        var issue = session.Result[0];
        await Assert.That(issue.Code).IsEqualTo(DtdValidityCodes.XML5008.Code);
        await Assert.That(issue.Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(issue.Constraint).IsEqualTo("No Duplicate Types");
        await Assert.That(issue.Location).IsEqualTo(DoctypeLocation);
        await Assert.That(issue.Message).Contains("'a'");
        await Assert.That(issue.Message).Contains("'doc'");
    }

    [Test]
    public async Task NoDuplicateTypes_DistinctNamesInMixedContent_ReportsNothing()
    {
        var model = Declare("doc", [Mixed("doc", "a", "b"), Element("a"), Element("b")]);

        await Assert.That(new Session(model).Result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task NoDuplicateTypes_EachRepeatedOccurrence_IsReported()
    {
        var model = Declare("doc", [Mixed("doc", "a", "a", "a"), Element("a")]);

        await Assert.That(new Session(model).Result.Count).IsEqualTo(2);
    }

    [Test]
    public async Task NoDuplicateTypes_RepeatedNameInElementContent_IsNotThisConstraint()
    {
        var model = Declare("doc", [Children("doc", external: false, "a", "a"), Element("a")]);

        await Assert.That(new Session(model).Result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task NoDuplicateTypes_TheSameNameInTwoDeclarations_IsNotADuplicate()
    {
        var model = Declare("doc", [Mixed("doc", "a"), Mixed("p", "a"), Element("a"), Element("p")]);

        await Assert.That(new Session(model).Result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task NoDuplicateTypes_MixedContentWithoutNames_ReportsNothing()
    {
        var model = Declare("doc", [Mixed("doc")]);

        await Assert.That(new Session(model).Result.Count).IsEqualTo(0);
    }
}
