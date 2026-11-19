namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Ids;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Reader.Pipeline;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;
using NetEvolve.Schemas.Xsd.Validation.Core;

public class IdrefTests
{
    private static async Task AssertDangling(IReadOnlyList<ValidationIssue> issues, int count, int line, string token)
    {
        await Assert.That(issues.Count).IsEqualTo(count);
        await Assert.That(issues[0].Code).IsEqualTo(ValidationHarness.Code("cvc-id"));
        await Assert.That(issues[0].Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(issues[0].Constraint).IsEqualTo("cvc-id.1");
        await Assert.That(issues[0].Location.Line).IsEqualTo(line);
        await Assert.That(issues[0].Location.Column).IsGreaterThan(0);
        await Assert.That(issues[0].Message).Contains("'" + token + "'");
    }

    [Test]
    public async Task Idref_Attribute_ThatMatchesNoId_IsReportedAtTheReferringNode()
    {
        var issues = IdFixture.Run(IdFixture.Schema(), "<r>\n<e id=\"a\"/>\n<e ref=\"b\"/>\n</r>");

        await AssertDangling(issues, 1, 3, "b");
    }

    [Test]
    public async Task Idref_Element_ThatMatchesNoId_IsReportedAtTheReferringElement()
    {
        var issues = IdFixture.Run(IdFixture.Schema(), "<r>\n<i>a</i>\n<f>b</f>\n</r>");

        await AssertDangling(issues, 1, 3, "b");
    }

    [Test]
    public async Task Idrefs_OneDanglingTokenInAList_IsReportedAtTheReferringNode_AndOnlyThatToken()
    {
        var issues = IdFixture.Run(IdFixture.Schema(), "<r>\n<e id=\"a\"/>\n<e id=\"c\"/>\n<e refs=\"a b c\"/>\n</r>");

        await AssertDangling(issues, 1, 4, "b");
    }

    [Test]
    public async Task Idrefs_Element_WithOneDanglingToken_IsReportedAtTheReferringElement()
    {
        var issues = IdFixture.Run(IdFixture.Schema(), "<r>\n<i>a</i>\n<fs>a\n b</fs>\n</r>");

        await AssertDangling(issues, 1, 3, "b");
    }

    [Test]
    public async Task Idref_ToAnIdDeclaredLaterInTheDocument_IsValid()
    {
        var issues = IdFixture.Run(
            IdFixture.Schema(),
            "<r>\n<e ref=\"a\"/>\n<e refs=\"a b\"/>\n<f>b</f>\n<e id=\"a\"/>\n<i>b</i>\n</r>"
        );

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Idref_ToAnEarlierId_AndToTheIdOfTheSameElement_IsValid()
    {
        var issues = IdFixture.Run(IdFixture.Schema(), "<r><e id=\"a\"/><e ref=\"a\"/><e id=\"b\" ref=\"b\"/></r>");

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Idref_Dangling_IsReportedOnlyAfterTheDocumentEnded()
    {
        var collector = new ValidationIssueCollector(ValidationLimits.Default);
        var probe = new CountProbe(collector);
        var validator = new StreamingValidator(
            IdFixture.Schema().Model(),
            new ScriptedAssessor(),
            new XsdIssueReporter(collector),
            ValidationLimits.Default,
            ValidationHarness.Source,
            [IdFixture.Listener(new ScriptedAssessor()), probe]
        );

        using var reader = XmlPipelineReader.Create("<r><e ref=\"x\"/><e id=\"a\"/></r>".AsSpan());
        var status = reader.Pump(ref validator);

        // The probe runs after the id listener at the end tag of the root, the last element event; nothing is reported yet.
        await Assert.That(status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(probe.CountAtRootEnd).IsEqualTo(0);
        await Assert.That(collector.ToResult().Issues.Count).IsEqualTo(1);
    }

    private sealed class CountProbe(ValidationIssueCollector collector) : ValidationListener
    {
        public int CountAtRootEnd { get; private set; } = -1;

        public override ListenerInterest Interest => ListenerInterest.EndElement;

        public override void EndElement(in ElementEndInfo info)
        {
            if (info.Ordinal == 1)
            {
                CountAtRootEnd = collector.ToResult().Issues.Count;
            }
        }
    }

    [Test]
    public async Task Idref_Dangling_EveryOccurrenceIsReportedAtItsOwnLocationInDocumentOrder()
    {
        var issues = IdFixture.Run(IdFixture.Schema(), "<r>\n<e ref=\"x\"/>\n<e refs=\"x y\"/>\n</r>");

        await Assert.That(issues.Count).IsEqualTo(3);
        await Assert.That(issues[0].Location.Line).IsEqualTo(2);
        await Assert.That(issues[1].Location.Line).IsEqualTo(3);
        await Assert.That(issues[2].Location.Line).IsEqualTo(3);
    }

    [Test]
    public async Task Idref_SurroundingWhiteSpace_IsCollapsed()
    {
        var issues = IdFixture.Run(IdFixture.Schema(), "<r><e id=\" a \"/><e ref=\"a\"/><f> a </f></r>");

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Idref_OnADerivedType_AndOnAListOfIdref_IsChecked()
    {
        var schema = new SchemaFixture();
        var derived = IdFixture.Restriction(SchemaFixture.Simple("IDREF"), schema);
        var list = IdFixture.ListOf(SchemaFixture.Simple("IDREF"), schema);

        var issues = IdFixture.Run(
            IdFixture.Schema(idref: derived, idrefs: list),
            "<r>\n<e id=\"a\"/>\n<e ref=\"x\"/>\n<e refs=\"a y\"/>\n</r>"
        );

        await Assert.That(issues.Count).IsEqualTo(2);
        await Assert.That(issues[0].Location.Line).IsEqualTo(3);
        await Assert.That(issues[1].Location.Line).IsEqualTo(4);
    }

    [Test]
    public async Task Idref_ThatTheDatatypeRejects_IsNotChecked()
    {
        var listener = IdFixture.Listener(new RejectingAssessor());

        var issues = ValidationHarness.Run(
            IdFixture.Schema(),
            "<r><e ref=\"1\"/></r>",
            new RejectingAssessor(),
            listener
        );

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsNotEqualTo(ValidationHarness.Code("cvc-id"));
    }
}
