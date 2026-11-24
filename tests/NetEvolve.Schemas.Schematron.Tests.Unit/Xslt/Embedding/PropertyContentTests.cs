namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Embedding;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Schematron.Evaluation.Assertions;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Results.Mapping;
using NetEvolve.Schemas.Schematron.Scopes;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Assertions;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Construction;
using NetEvolve.Schemas.Schematron.Xslt.Embedding;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Assertions.AssertionModel;

/// <summary>
/// <c>xsl:copy-of</c> in a property: the message builder evaluates the <see langword="select"/> expression, keeps the string value of
/// the result in the text of the property, and hands the nodes and the text to the content of the finding, in order.
/// </summary>
public class PropertyContentTests
{
    private sealed class ContentSink : ISchematronReportSink
    {
        internal List<(string Id, string Text, IFindingContent? Content)> Properties { get; } = [];

        internal int Findings { get; private set; }

        public void Begin(in SchematronRunInfo info) { }

        public void ActivePattern(in SchematronPatternEvent pattern) { }

        public void FiredRule(in SchematronRuleEvent rule) { }

        public void ErrorOccurred(in SchematronErrorEvent errorEvent) { }

        public void Finish(in SchematronRunSummary summary) { }

        public void Finding(in SchematronFinding finding)
        {
            Findings++;
            for (var i = 0; i < finding.Properties.Count; i++)
            {
                Properties.Add(
                    (finding.Properties.IdAt(i), finding.Properties.TextAt(i), finding.Properties.ContentAt(i))
                );
            }
        }
    }

    private sealed class Harness : IDisposable
    {
        private readonly IQueryRun _run;

        internal Harness(
            SchematronProperty property,
            bool embedded = true,
            string[]? variables = null,
            int maxTextLength = 1000
        )
        {
            Binding = new AssertionTestBinding();
            Binder = new RecordingSlotBinder();
            Sink = new ContentSink();
            Problems = new FakeProblemSink();
            Schema = AssertionModel.Schema(null, [property]);
            Rule = new RuleScope(
                AssertionModel.Rule(),
                [],
                new QueryStaticScope([], variables ?? [], null, SchematronEdition.Iso2016)
            );
            var mapper = new IssueMapper(new ValidationIssueCollector(ValidationLimits.Default), Sink);
            _run = Binding.BeginRun(null!, default);
            Evaluator = new AssertionEvaluator(
                _run,
                Binder,
                mapper,
                Problems,
                "file:///instance.xml",
                maxTextLength,
                embedded ? new EmbeddedContent() : null
            );
        }

        internal AssertionTestBinding Binding { get; }

        internal RecordingSlotBinder Binder { get; }

        internal ContentSink Sink { get; }

        internal FakeProblemSink Problems { get; }

        internal SchematronSchema Schema { get; }

        internal RuleScope Rule { get; }

        internal AssertionEvaluator Evaluator { get; }

        internal void Fire()
        {
            var assertion = MakeAssert("false", Message("m"), properties: ["p"]);
            var at = new AssertionPosition(1, 1, 1);
            if (!AssertionCompiler.TryCompile(assertion, Rule, Schema, Binding, in at, Problems, out var compiled))
            {
                throw new InvalidOperationException("The assertion does not compile.");
            }

            var focus = new QueryFocus(AssertionTree.Node("order1"), 1, 1);
            _ = Evaluator.Evaluate(compiled, in focus);
        }

        public void Dispose()
        {
            Evaluator.Dispose();
            _run.Dispose();
        }
    }

    private static SchematronCopyOf CopyOf(string select) => new(Next(), select);

    private static SchematronProperty PropertyOf(params SchematronMessagePart[] parts) => Property("p", Message(parts));

    private static string Holder(IFindingContent content, string path)
    {
        using var tree = new XmlTreeBuilder();
        tree.StartDocument(default);
        var name = tree.Names.Intern(string.Empty, "holder", string.Empty);
        tree.StartElement(name, default);
        content.WriteAttributes(tree);
        tree.EndAttributes();
        content.WriteChildren(tree);
        tree.EndElement(name);
        tree.EndDocument();
        var item = XdmItem.FromNode(new XPathNode(tree.Complete().Document.FirstChild));
        var nodes = PathQuery.Select(in item, path);
        var found = new string[nodes.Count];
        for (var i = 0; i < found.Length; i++)
        {
            var node = nodes[i];
            found[i] = PathQuery.Describe(in node);
        }

        return string.Join("|", found);
    }

    [Test]
    public async Task CopyOf_OfAnElement_KeepsItsStringValueAsTheTextAndItsNodesAsTheContent()
    {
        using var h = new Harness(PropertyOf(CopyOf("sel")));
        h.Binding.Items["sel"] = [XdmItem.FromNode(AssertionTree.Node("order1"))];

        h.Fire();

        await Assert.That(h.Problems.Problems.Count).IsEqualTo(0);
        var property = h.Sink.Properties[0];
        await Assert.That(property.Id).IsEqualTo("p");
        await Assert.That(property.Text).IsEqualTo("txt12");
        await Assert.That(Holder(property.Content!, "order/@id")).IsEqualTo("Attribute:id=7");
        await Assert.That(Holder(property.Content!, "order/item")).IsEqualTo("Element:item=1|Element:item=2");
    }

    [Test]
    public async Task CopyOf_OfAnAtomicValue_WritesItsStringValue()
    {
        using var h = new Harness(PropertyOf(CopyOf("sel")));
        h.Binding.Items["sel"] = [XdmItem.FromString("seven")];

        h.Fire();

        var property = h.Sink.Properties[0];
        await Assert.That(property.Text).IsEqualTo("seven");
        await Assert.That(Holder(property.Content!, "node()")).IsEqualTo("Text:=seven");
    }

    [Test]
    public async Task CopyOf_OfAnAttribute_CopiesItOntoTheHolder()
    {
        using var h = new Harness(PropertyOf(CopyOf("sel")));
        h.Binding.Items["sel"] = [XdmItem.FromNode(AssertionTree.Node("id"))];

        h.Fire();

        var property = h.Sink.Properties[0];
        await Assert.That(property.Text).IsEqualTo("7");
        await Assert.That(Holder(property.Content!, "@*")).IsEqualTo("Attribute:id=7");
    }

    [Test]
    public async Task CopyOf_BetweenText_KeepsTheOrderInTheTextAndTheContent()
    {
        using var h = new Harness(PropertyOf(Text("a="), CopyOf("sel"), Text("!"), ValueOf("val")));
        h.Binding.Items["sel"] = [XdmItem.FromString("v")];
        h.Binding.Texts["val"] = "w";

        h.Fire();

        var property = h.Sink.Properties[0];
        await Assert.That(property.Text).IsEqualTo("a=v!w");
        await Assert.That(Holder(property.Content!, "node()")).IsEqualTo("Text:=a=v!w");
    }

    [Test]
    public async Task CopyOf_OfTwoAtomicValues_SeparatesThemByOneSpace()
    {
        using var h = new Harness(PropertyOf(CopyOf("sel")));
        h.Binding.Items["sel"] = [XdmItem.FromString("x"), XdmItem.FromInteger(2)];

        h.Fire();

        await Assert.That(h.Sink.Properties[0].Text).IsEqualTo("x 2");
    }

    [Test]
    public async Task CopyOf_OfTheEmptySequence_IsAnEmptyProperty()
    {
        using var h = new Harness(PropertyOf(CopyOf("sel")));
        h.Binding.Items["sel"] = [];

        h.Fire();

        var property = h.Sink.Properties[0];
        await Assert.That(property.Text).IsEqualTo(string.Empty);
        await Assert.That(Holder(property.Content!, "node()|@*")).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Property_WithoutCopyOf_HasNoContent()
    {
        using var h = new Harness(PropertyOf(Text("plain")));

        h.Fire();

        await Assert.That(h.Sink.Properties[0].Text).IsEqualTo("plain");
        await Assert.That(h.Sink.Properties[0].Content).IsNull();
    }

    [Test]
    public async Task CopyOf_AttributeAfterText_IsXtde0410_AndTheFindingKeepsOnlyTheOtherParts()
    {
        using var h = new Harness(PropertyOf(Text("text"), CopyOf("sel")));
        h.Binding.Items["sel"] = [XdmItem.FromNode(AssertionTree.Node("id"))];

        h.Fire();

        await Assert.That(h.Sink.Findings).IsEqualTo(1);
        await Assert.That(h.Sink.Properties.Count).IsEqualTo(0);
        await Assert.That(h.Problems.Problems.Count).IsEqualTo(1);
        await Assert.That(h.Problems.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.EvaluationFailed);
        await Assert.That(h.Problems.Problems[0].Constraint).IsEqualTo("XTDE0410");
        await Assert.That(h.Problems.Problems[0].Location.Line).IsGreaterThan(0);
    }

    [Test]
    public async Task CopyOf_OfAnAttribute_WithFormattingWhiteSpaceAround_StillCopiesIt()
    {
        using var h = new Harness(PropertyOf(Text("\n  "), CopyOf("sel"), Text("\n")));
        h.Binding.Items["sel"] = [XdmItem.FromNode(AssertionTree.Node("id"))];

        h.Fire();

        await Assert.That(h.Problems.Problems.Count).IsEqualTo(0);
        await Assert.That(h.Sink.Properties.Count).IsEqualTo(1);
        var property = h.Sink.Properties[0];
        await Assert.That(property.Text).IsEqualTo("7");
        await Assert.That(Holder(property.Content!, "@*")).IsEqualTo("Attribute:id=7");
        await Assert.That(Holder(property.Content!, "node()")).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task CopyOf_OfAnElement_WithFormattingWhiteSpaceAround_KeepsOnlyTheCopiedNodes()
    {
        using var h = new Harness(PropertyOf(Text("\n  "), CopyOf("sel"), Text("\n")));
        h.Binding.Items["sel"] = [XdmItem.FromNode(AssertionTree.Node("order1"))];

        h.Fire();

        await Assert.That(h.Problems.Problems.Count).IsEqualTo(0);
        var property = h.Sink.Properties[0];
        await Assert.That(property.Text).IsEqualTo("txt12");
        await Assert.That(Holder(property.Content!, "order/@id")).IsEqualTo("Attribute:id=7");
        await Assert.That(Holder(property.Content!, "node()")).IsEqualTo("Element:order=txt12");
    }

    [Test]
    public async Task CopyOf_WithFormattingWhiteSpaceBetweenMixedParts_KeepsTheTextThatHasContent()
    {
        using var h = new Harness(
            PropertyOf(Text("\n  "), CopyOf("sel"), Text("\n  "), ValueOf("val"), Text("\n  tail"))
        );
        h.Binding.Items["sel"] = [XdmItem.FromString("v")];
        h.Binding.Texts["val"] = "w";

        h.Fire();

        await Assert.That(h.Problems.Problems.Count).IsEqualTo(0);
        var property = h.Sink.Properties[0];
        await Assert.That(property.Text).IsEqualTo("vw\n  tail");
        await Assert.That(Holder(property.Content!, "node()")).IsEqualTo("Text:=vw\n  tail");
    }

    [Test]
    public async Task CopyOf_WhoseSelectFails_DropsThePropertyAndReportsOnce()
    {
        using var h = new Harness(PropertyOf(Text("x"), CopyOf("bad")));

        h.Fire();

        await Assert.That(h.Sink.Findings).IsEqualTo(1);
        await Assert.That(h.Sink.Properties.Count).IsEqualTo(0);
        await Assert.That(h.Problems.Problems.Count).IsEqualTo(1);
    }

    [Test]
    public async Task CopyOf_WithAValueThatHasNoTextForm_DropsThePropertyAndReportsOnce()
    {
        using var h = new Harness(PropertyOf(CopyOf("sel")));
        h.Binding.Items["sel"] = [default(XdmItem)];

        h.Fire();

        await Assert.That(h.Sink.Findings).IsEqualTo(1);
        await Assert.That(h.Sink.Properties.Count).IsEqualTo(0);
        await Assert.That(h.Problems.Problems.Count).IsEqualTo(1);
    }

    [Test]
    public async Task CopyOf_WithoutTheEmbeddedContent_IsTheUnsupportedConstructError()
    {
        using var h = new Harness(PropertyOf(CopyOf("sel")), embedded: false);
        h.Binding.Items["sel"] = [XdmItem.FromString("v")];

        h.Fire();

        await Assert.That(h.Sink.Findings).IsEqualTo(1);
        await Assert.That(h.Sink.Properties.Count).IsEqualTo(0);
        await Assert.That(h.Problems.Problems.Count).IsEqualTo(1);
        await Assert.That(h.Problems.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.XsltUnsupported);
    }

    [Test]
    public async Task CopyOf_IsCompiledAsAMessagePartWithItsExpression()
    {
        using var h = new Harness(PropertyOf(CopyOf("sel")));
        var assertion = MakeAssert("false", Message("m"), properties: ["p"]);
        var at = new AssertionPosition(1, 1, 1);

        _ = AssertionCompiler.TryCompile(assertion, h.Rule, h.Schema, h.Binding, in at, h.Problems, out var compiled);

        var part = compiled.Properties[0].Parts[0];
        await Assert.That(part.Kind).IsEqualTo(MessagePartKind.CopyOf);
        await Assert.That(part.Expression).IsNotNull();
        await Assert.That(compiled.Properties[0].HasCopyOf).IsTrue();
        await Assert.That(compiled.Message.HasCopyOf).IsFalse();
        await Assert
            .That(h.Binding.Compiled)
            .Contains(c =>
                string.Equals(c.Text, "sel", StringComparison.Ordinal) && c.Kind == QueryExpressionKind.Value
            );
    }

    [Test]
    public async Task CopyOf_WithAnExpressionThatDoesNotCompile_IsReportedAtCompile()
    {
        using var h = new Harness(PropertyOf(CopyOf("invalid")));
        var assertion = MakeAssert("false", Message("m"), properties: ["p"]);
        var at = new AssertionPosition(1, 1, 1);

        var ok = AssertionCompiler.TryCompile(assertion, h.Rule, h.Schema, h.Binding, in at, h.Problems, out _);

        await Assert.That(ok).IsFalse();
        await Assert.That(h.Problems.Problems.Count).IsEqualTo(1);
    }

    [Test]
    public async Task CopyOf_ReadingAVariableThatFailsToBind_DropsThePropertyWithoutAnotherProblem()
    {
        using var h = new Harness(PropertyOf(CopyOf("$v")), variables: ["v"]);
        _ = h.Binder.Failing.Add(0);

        h.Fire();

        await Assert.That(h.Sink.Findings).IsEqualTo(1);
        await Assert.That(h.Sink.Properties.Count).IsEqualTo(0);
        await Assert.That(h.Problems.Problems.Count).IsEqualTo(0);
    }

    [Test]
    public async Task CopyOf_LongerThanTheLimit_IsCutAndDropsTheProperty()
    {
        using var h = new Harness(PropertyOf(CopyOf("sel")), maxTextLength: 3);
        h.Binding.Items["sel"] = [XdmItem.FromString("seven")];

        h.Fire();

        await Assert.That(h.Sink.Findings).IsEqualTo(1);
        await Assert.That(h.Sink.Properties.Count).IsEqualTo(0);
        await Assert.That(h.Problems.Problems.Count).IsEqualTo(1);
        await Assert.That(h.Problems.Problems[0].Message).Contains("longer than the limit");
    }

    [Test]
    public async Task Property_WithTextLongerThanTheLimit_AndCopyOf_IsCutAndDropped()
    {
        using var h = new Harness(PropertyOf(Text("a long text"), CopyOf("sel")), maxTextLength: 3);
        h.Binding.Items["sel"] = [XdmItem.FromString("x")];

        h.Fire();

        await Assert.That(h.Sink.Properties.Count).IsEqualTo(0);
        await Assert.That(h.Problems.Problems.Count).IsEqualTo(1);
    }
}
