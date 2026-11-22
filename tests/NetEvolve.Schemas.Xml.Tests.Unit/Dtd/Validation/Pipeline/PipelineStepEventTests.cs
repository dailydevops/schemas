namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Pipeline;

using NetEvolve.Schemas;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Validation.Pipeline;
using NetEvolve.Schemas.Xml.Reader.Pipeline;
using NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Contracts.Fakes;
using static NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Pipeline.PipelineHarness;

/// <summary>What the validating reader mode hands the steps and in which order (design #122, D1 and D2).</summary>
public class PipelineStepEventTests
{
    private const string Subset = "<!ELEMENT r ANY><!ELEMENT a (#PCDATA)>";

    [Test]
    public async Task Step_ReceivesEventsInReaderOrder()
    {
        var step = new ScriptedDtdValidationStep();

        var (result, _) = Run(Doc(Subset, "<r><a>x</a><!--c--><?p d?>y&#65;<![CDATA[z]]></r>"), step);

        await Assert.That(result.IsWellFormed).IsTrue();
        await Assert
            .That(step.Events)
            .IsEquivalentTo([
                "Begin",
                "StartElement:r",
                "StartElement:a",
                "Text:Text:x",
                "EndElement:a",
                "Comment",
                "ProcessingInstruction",
                "Text:Text:y",
                "Text:Reference:A",
                "Text:CData:z",
                "EndElement:r",
                "End",
            ]);
    }

    [Test]
    public async Task Step_BeginFiresOnceBeforeTheFirstStartElement()
    {
        var step = new ScriptedDtdValidationStep();

        _ = Run(Doc(Subset, "<r><a/><a/></r>"), step);

        await Assert
            .That(step.Events.FindAll(e => string.Equals(e, "Begin", StringComparison.Ordinal)).Count)
            .IsEqualTo(1);
        await Assert.That(step.Events[0]).IsEqualTo("Begin");
        await Assert.That(step.Context!.Declarations.Name).IsEqualTo("r");
        await Assert.That(step.Context.Declarations.Elements.ContainsKey("a")).IsTrue();
    }

    [Test]
    public async Task Step_BeginFiresAlsoWithoutDoctype_WithAnEmptyModel()
    {
        var step = new ScriptedDtdValidationStep();

        var (_, sink) = Run("<r/>", step);

        await Assert.That(step.Events).IsEquivalentTo(["Begin", "StartElement:r", "EndElement:r", "End"]);
        await Assert.That(step.Context!.Declarations.Name).IsEmpty();
        await Assert.That(step.Context.Declarations.Elements).IsEmpty();
        await Assert.That(sink.Events.Contains("doctype:r")).IsFalse();
    }

    [Test]
    public async Task Step_PrologAndEpilogCommentsAndInstructions_AreNotForwarded()
    {
        var step = new ScriptedDtdValidationStep();

        var (_, sink) = Run("<!--pre--><?pre x?>" + Doc(Subset, "<r/><!--post--><?post y?>"), step);

        await Assert.That(step.Events).IsEquivalentTo(["Begin", "StartElement:r", "EndElement:r", "End"]);
        await Assert
            .That(sink.Events.FindAll(e => e.StartsWith("comment:", StringComparison.Ordinal)).Count)
            .IsEqualTo(2);
        await Assert.That(sink.Events.FindAll(e => e.StartsWith("pi:", StringComparison.Ordinal)).Count).IsEqualTo(2);
    }

    [Test]
    public async Task Step_CommentAndInstructionInsideTheRoot_AreForwarded()
    {
        var step = new ScriptedDtdValidationStep();

        _ = Run(Doc(Subset, "<r><!--c--><?p d?></r>"), step);

        await Assert.That(step.Events.Contains("Comment")).IsTrue();
        await Assert.That(step.Events.Contains("ProcessingInstruction")).IsTrue();
    }

    [Test]
    public async Task Pipeline_StepsRunInRegistrationOrder_ForEveryEvent()
    {
        var log = new List<string>();
        var first = new ScriptedDtdValidationStep();
        var second = new ScriptedDtdValidationStep();
        foreach (var name in new[] { "Begin", "StartElement:r", "Text:Text:x", "EndElement:r", "End" })
        {
            var captured = name;
            _ = first.On(captured, _ => log.Add("1:" + captured));
            _ = second.On(captured, _ => log.Add("2:" + captured));
        }

        _ = Run(Doc(Subset, "<r>x</r>"), first, second);

        await Assert
            .That(log)
            .IsEquivalentTo([
                "1:Begin",
                "2:Begin",
                "1:StartElement:r",
                "2:StartElement:r",
                "1:Text:Text:x",
                "2:Text:Text:x",
                "1:EndElement:r",
                "2:EndElement:r",
                "1:End",
                "2:End",
            ]);
    }

    [Test]
    public async Task Pipeline_ForwardsEveryEventUnchangedToTheInnerSink()
    {
        const string Xml =
            "<?xml version=\"1.0\"?><!DOCTYPE r [<!ELEMENT r ANY><!ATTLIST r k CDATA \"d\">]>"
            + "<r xmlns:p=\"urn:p\" j=\"1\"><!--c-->t<?i d?><p:e/></r>";
        var options = DtdValidatingPipeline.ReaderOptions(DtdValidationOptions.Default, null, DocumentUri);
        using var reader = XmlPipelineReader.Create(Xml.AsSpan(), options);
        var direct = new RecordingSink();
        _ = reader.Pump(ref direct);

        var (_, through) = Run(Xml, new ScriptedDtdValidationStep());

        await Assert.That(through.Events).IsEquivalentTo(direct.Events);
        await Assert.That(through.Events.Contains("info:k:Cdata:False")).IsTrue();
    }

    [Test]
    public async Task Step_StartElement_CarriesTheNameAsWrittenAndTheAttributesAfterDefaults()
    {
        const string Declared =
            "<!ELEMENT r ANY><!ATTLIST r xmlns:p CDATA #FIXED \"urn:p\" k NMTOKEN #IMPLIED d CDATA \"dv\">";
        var step = new CapturingStep();

        _ = Run(Doc(Declared, "<r k=\" t  u \">\n  <p:e/></r>"), step);

        await Assert
            .That(step.Elements)
            .IsEquivalentTo([
                "r@2:1 xmlns:p=urn:p:Cdata! k=t u:NmToken~ d=dv:Cdata!",
                "p:e@3:3",
                "/p:e@3:9",
                "/r@3:13",
            ]);
    }

    [Test]
    public async Task Step_Attributes_StayIntactWhenTheCopyGrows()
    {
        var attributes = new System.Text.StringBuilder();
        for (var i = 0; i < 40; i++)
        {
            _ = attributes.Append(" attribute").Append(i).Append("=\"value number ").Append(i).Append('"');
        }

        var step = new CapturingStep();

        _ = Run(Doc("<!ELEMENT r ANY>", "<r" + attributes + "/>"), step);

        await Assert.That(step.Elements[0]).Contains(" attribute0=value number 0:-");
        await Assert.That(step.Elements[0]).Contains(" attribute39=value number 39:-");
    }

    [Test]
    public async Task Step_TextAndEndTagPositions_AreThePositionOfTheReaderAfterTheToken()
    {
        // Limitation (#927): the sink events carry no start position for text and end tags.
        var text = new ScriptedDtdValidationStep();
        var ends = new CapturingStep();

        _ = Run(Doc(Subset, "<r>x</r>"), text, ends);

        await Assert.That(text.LastPosition.Line).IsEqualTo(2);
        await Assert.That(text.LastPosition.Column).IsEqualTo(5);
        await Assert.That(ends.Elements).Contains("/r@2:9");
    }

    [Test]
    [Arguments("<?xml version=\"1.0\" standalone=\"yes\"?>", true)]
    [Arguments("<?xml version=\"1.0\" standalone=\"no\"?>", false)]
    [Arguments("<?xml version=\"1.0\"?>", false)]
    [Arguments("", false)]
    public async Task Context_IsStandalone_FollowsTheXmlDeclaration(string declaration, bool expected)
    {
        var step = new ScriptedDtdValidationStep();

        _ = Run(declaration + Doc(Subset, "<r/>"), step);

        await Assert.That(step.Context!.IsStandalone).IsEqualTo(expected);
    }

    [Test]
    public async Task Context_CarriesTheDocumentUriAndTheLocationOfTheDoctype()
    {
        var step = new ScriptedDtdValidationStep();

        _ = Run("\n" + Doc(Subset, "<r/>"), step);

        await Assert.That(step.Context!.SystemId).IsEqualTo(DocumentUri);
        await Assert.That(step.Context.DocumentTypeLocation.Source).IsEqualTo(DocumentUri);
        await Assert.That(step.Context.DocumentTypeLocation.Line).IsEqualTo(2);
        await Assert.That(step.Context.Options).IsSameReferenceAs(DtdValidationOptions.Default);
    }

    [Test]
    public async Task Context_WithoutDoctype_LocatesTheDocumentAtItsStart()
    {
        var step = new ScriptedDtdValidationStep();

        _ = Run("<r/>", step);

        await Assert.That(step.Context!.DocumentTypeLocation).IsEqualTo(new DocumentLocation(DocumentUri, 1, 1));
    }

    [Test]
    public async Task Step_EndDoesNotFire_AfterAFatalStatusOfTheReader()
    {
        var step = new ScriptedDtdValidationStep();

        var (result, _) = Run(Doc(Subset, "<r><a></r>"), step);

        await Assert.That(result.IsWellFormed).IsFalse();
        await Assert.That(step.Events.Contains("End")).IsFalse();
        await Assert.That(step.Events[^1]).IsEqualTo("StartElement:a");
    }

    [Test]
    public async Task Step_Stop_EndsTheDeliveryToEveryStepAndKeepsTheInnerSinkRunning()
    {
        var stopping = new ScriptedDtdValidationStep().On("Begin", c => c.Stop(DtdLimitCodes.XML1118, default));
        var other = new ScriptedDtdValidationStep();

        var (result, sink) = Run(Doc(Subset, "<r><a>x</a><!--c--><?p d?></r>"), stopping, other);

        await Assert.That(stopping.Events).IsEquivalentTo(["Begin"]);
        await Assert.That(other.Events).IsEquivalentTo(["Begin"]);
        await Assert.That(result.IsWellFormed).IsTrue();
        await Assert.That(result.Issues.Issues.Count).IsEqualTo(1);
        await Assert.That(result.Issues.Issues[0].Code).IsEqualTo(DtdLimitCodes.XML1118.Code);
        await Assert.That(sink.Events[^1]).IsEqualTo("EndDocument");
        await Assert.That(sink.Events.Contains("text:Text:x")).IsTrue();
    }

    [Test]
    public async Task Step_Stop_AfterTheFirstStartTag_SkipsTheRestOfTheDocument()
    {
        var stopping = new ScriptedDtdValidationStep().On("StartElement:r", c =>
            c.Stop(XmlErrorCodes.XML1108, default)
        );

        _ = Run(Doc(Subset, "<r><a>x</a><!--c--></r>"), stopping);

        await Assert.That(stopping.Events).IsEquivalentTo(["Begin", "StartElement:r"]);
    }

    [Test]
    public async Task Step_Stop_InsideTheRootStopsComments_Instructions_AndEndTags()
    {
        var stopping = new ScriptedDtdValidationStep().On("StartElement:a", c =>
            c.Stop(XmlErrorCodes.XML1108, default)
        );

        _ = Run(Doc(Subset, "<r><a/><!--c--><?p d?></r>"), stopping);

        await Assert.That(stopping.Events).IsEquivalentTo(["Begin", "StartElement:r", "StartElement:a"]);
    }
}
