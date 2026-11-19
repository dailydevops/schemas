namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Document;

using NetEvolve.Schemas;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Model;
using static NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Document.DocumentHarness;

/// <summary>
/// The codes that the approved table of design #122 gives to this step besides the constraints of the work package:
/// Entity Declared (<c>XML5024</c>), the missing document type declaration (<c>XML5910</c>), and the undeclared element
/// type that a declaration mentions (<c>XML5901</c>).
/// </summary>
public class DocumentReportTests
{
    // Entity Declared (section 4.1, the validity constraint form).

    [Test]
    public async Task EntityDeclared_ReferenceToAnUndeclaredEntity_ReportsXml5024AtTheReference_AndContinues()
    {
        var session = new Session(Declare("doc", [Element("doc")]))
            .Start("doc", At(3, 1))
            .Reference("missing", DtdEntityOutcome.Undeclared, At(3, 6))
            .Reference("missing", DtdEntityOutcome.Undeclared, At(3, 15));

        await Assert.That(session.Result.Count).IsEqualTo(2);
        var issue = session.Result[0];
        await Assert.That(issue.Code).IsEqualTo(DtdValidityCodes.XML5024.Code);
        await Assert.That(issue.Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(issue.Constraint).IsEqualTo("Entity Declared");
        await Assert.That(issue.Location).IsEqualTo(new DocumentLocation(SystemId, 3, 6));
        await Assert.That(issue.Message).Contains("'missing'");
        await Assert.That(session.Issues.IsStopped).IsFalse();
    }

    [Test]
    [Arguments(DtdEntityOutcome.DeclaredInternal)]
    [Arguments(DtdEntityOutcome.DeclaredExternal)]
    public async Task EntityDeclared_ReferenceToADeclaredEntity_ReportsNothing(DtdEntityOutcome outcome)
    {
        var session = new Session(Declare("doc", [Element("doc")], entities: [Entity("e", external: false)]))
            .Start("doc", At(3, 1))
            .Reference("e", outcome, At(3, 6));

        await Assert.That(session.Result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task EntityDeclared_IsReportedAlsoForAStandaloneDocument_NotAsAStandaloneClause()
    {
        var session = new Session(Declare("doc", [Element("doc")]), standalone: true)
            .Start("doc", At(3, 1))
            .Reference("missing", DtdEntityOutcome.Undeclared, At(3, 6));

        await Assert.That(session.Result.Count).IsEqualTo(1);
        await Assert.That(session.Result[0].Code).IsEqualTo(DtdValidityCodes.XML5024.Code);
    }

    // A valid document has a document type declaration (section 2.8).

    [Test]
    public async Task MissingDocumentType_EmptyModel_ReportsXml5910Once_AndNoRootElementTypeError()
    {
        var session = new Session(Declare(string.Empty))
            .Start("doc", At(1, 1))
            .Start("child", At(1, 6))
            .End("child")
            .End("doc")
            .Finish();

        await Assert.That(session.Result.Count).IsEqualTo(1);
        var issue = session.Result[0];
        await Assert.That(issue.Code).IsEqualTo(DtdValidityCodes.XML5910.Code);
        await Assert.That(issue.Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(issue.Location).IsEqualTo(DoctypeLocation);
    }

    [Test]
    public async Task MissingDocumentType_ModelWithAName_ReportsNothing()
    {
        var session = new Session(Declare("doc", [Element("doc")])).Start("doc", At(1, 1)).End("doc").Finish();

        await Assert.That(session.Result.Count).IsEqualTo(0);
    }

    // An element type that a declaration mentions has no declaration (section 3.2, optional warning).

    [Test]
    public async Task UndeclaredMention_NameInMixedAndElementContent_ReportsXml5901AsAWarning_OncePerName()
    {
        var model = Declare(
            "doc",
            [Mixed("doc", "known", "gone"), Children("known", external: false, "gone", "also"), Element("also")]
        );
        var session = new Session(model);

        await Assert.That(session.Result.Count).IsEqualTo(1);
        var issue = session.Result[0];
        await Assert.That(issue.Code).IsEqualTo(DtdValidityCodes.XML5901.Code);
        await Assert.That(issue.Severity).IsEqualTo(IssueSeverity.Warning);
        await Assert.That(issue.Location).IsEqualTo(DoctypeLocation);
        await Assert.That(issue.Message).Contains("'gone'");
        await Assert.That(issue.Message).Contains("'doc'");
    }

    [Test]
    public async Task UndeclaredMention_EveryMentionedNameDeclared_ReportsNothing()
    {
        var model = Declare("doc", [Mixed("doc", "a"), Children("a", external: false, "b"), Element("b")]);

        await Assert.That(new Session(model).Result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task UndeclaredMention_IsAWarning_ThatLeavesTheResultValid()
    {
        var session = new Session(Declare("doc", [Mixed("doc", "gone")]));

        await Assert.That(session.Issues.ToResult().IsValid).IsTrue();
    }

    [Test]
    public async Task UndeclaredMention_InANestedGroup_IsFound()
    {
        var inner = new DtdContentParticle(DtdParticleKind.Name, "deep", DtdOccurrence.Once, null);
        var group = new DtdContentParticle(DtdParticleKind.Choice, null, DtdOccurrence.Once, [inner]);
        var outer = new DtdContentParticle(DtdParticleKind.Sequence, null, DtdOccurrence.Once, [group]);
        var model = Declare("doc", [Children("doc", outer)]);

        var session = new Session(model);

        await Assert.That(session.Result.Count).IsEqualTo(1);
        await Assert.That(session.Result[0].Message).Contains("'deep'");
    }
}
