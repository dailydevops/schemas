namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions;

using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using NetEvolve.Schemas.Schematron.Tests.Unit.Parsing.Core;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions.FunctionFixture;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions.FunctionTree;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions.Fx;

/// <summary>
/// A local <c>xsl:variable</c> with a sequence constructor (XSLT 2.0 section 9.3, #629): a temporary tree with a document node
/// without <see langword="as"/>, the converted sequence with <see langword="as"/>, and the slots of the variable and of the locals in its content.
/// </summary>
public class FunctionVariableContentTests
{
    private static QueryStatus Call(FunctionFixture fixture, out XdmSequence result, out SchematronProblem problem)
    {
        var (_, caller) = fixture.CreateRun();
        _ = fixture.Table.TryGet(new XPathQName(Functions, "f"), 0, out var function);
        var status = caller.TryCall(function, [], out result);
        problem = caller.GetProblem();
        return status;
    }

    private static XmlElem Content(string? type, params object[] content) => Variable("v", null, type, 11, content);

    [Test]
    public async Task Call_ContentWithoutAs_BindsADocumentNodeWithTheConstructedChildren()
    {
        var fixture = CreateWithConstruction();
        _ = fixture.CompileAll(
            Function("f:f", null, 10, Content(null, Other("item", "urn:test:items", 12)), Sequence("$v", 13))
        );

        var status = Call(fixture, out var result, out _);

        await Assert.That(fixture.Problems.Problems.Count).IsEqualTo(0);
        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        await Assert.That(result.Count).IsEqualTo(1);
        _ = result[0].TryGetNode(out var node);
        await Assert.That(node.Node.Kind).IsEqualTo(XmlNodeKind.Document);
        await Assert.That(node.Node.FirstChild.Kind).IsEqualTo(XmlNodeKind.Element);
    }

    [Test]
    public async Task Call_ContentThatConstructsNothing_BindsAnEmptyDocumentNode()
    {
        var fixture = CreateWithConstruction();
        _ = fixture.CompileAll(
            Function("f:f", null, 10, Content(null, X("if", 12, A("test", "false()"))), Sequence("$v", 13))
        );

        var status = Call(fixture, out var result, out _);

        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        _ = result[0].TryGetNode(out var node);
        await Assert.That(node.Node.Kind).IsEqualTo(XmlNodeKind.Document);
        await Assert.That(node.Node.FirstChild.IsEmpty).IsTrue();
    }

    [Test]
    public async Task Call_ContentWithAs_BindsTheConvertedSequence()
    {
        var fixture = CreateWithConstruction();
        _ = fixture.CompileAll(
            Function("f:f", null, 10, Content("xs:double*", Sequence("1, 2", 12)), Sequence("$v", 13))
        );

        var status = Call(fixture, out var result, out _);

        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result[0].TryGetDouble(out var first)).IsTrue();
        await Assert.That(first).IsEqualTo(1d);
    }

    [Test]
    public async Task Call_ContentWithAsAndAnEmptyResult_BindsTheEmptySequence()
    {
        var fixture = CreateWithConstruction();
        _ = fixture.CompileAll(
            Function("f:f", null, 10, Content("xs:integer?", X("if", 12, A("test", "false()"))), Sequence("$v", 13))
        );

        var status = Call(fixture, out var result, out _);

        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        await Assert.That(result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Call_ContentThatDoesNotMatchAs_FailsWithXtte0570()
    {
        var fixture = CreateWithConstruction();
        _ = fixture.CompileAll(
            Function("f:f", null, 10, Content("xs:integer", Sequence("'x'", 12)), Sequence("$v", 13))
        );

        var status = Call(fixture, out _, out var problem);

        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(problem.Code).IsEqualTo(SchematronDiagnosticCodes.EvaluationFailed);
        await Assert.That(problem.Constraint).IsEqualTo("XPTY0004");
        await Assert.That(problem.Message).Contains("XTTE0570").And.Contains("'v'");
        await Assert.That(problem.Location.Line).IsEqualTo(11);
    }

    [Test]
    public async Task Call_VariablesAfterAndInsideTheContent_KeepTheirOwnSlots()
    {
        var fixture = CreateWithConstruction();
        _ = fixture.CompileAll(
            Function(
                "f:f",
                null,
                10,
                Content("xs:integer*", Variable("inner", "5", null, 12), Sequence("$inner, 6", 13)),
                Variable("w", "7", null, 14),
                Sequence("$v, $w", 15)
            )
        );

        var status = Call(fixture, out var result, out _);

        await Assert.That(fixture.Problems.Problems.Count).IsEqualTo(0);
        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        await Assert.That(result.Count).IsEqualTo(3);
        await Assert.That(MiniQuery.Integer(XdmSequence.Create(result[0]))).IsEqualTo(5);
        await Assert.That(MiniQuery.Integer(XdmSequence.Create(result[1]))).IsEqualTo(6);
        await Assert.That(MiniQuery.Integer(XdmSequence.Create(result[2]))).IsEqualTo(7);
    }

    [Test]
    public async Task Compile_ContentThatReadsItsOwnVariable_ReportsTheUndefinedVariable()
    {
        var fixture = CreateWithConstruction();

        var component = fixture.CompileOne(
            Function("f:f", null, 10, Content(null, Sequence("$v", 12)), Sequence("1", 13))
        );

        await Assert.That(component).IsNull();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo("NES3300/XPST0008");
    }

    [Test]
    public async Task Compile_ContentWithAnInvalidAs_ReportsTheType()
    {
        var fixture = CreateWithConstruction();

        var component = fixture.CompileOne(
            Function("f:f", null, 10, Content("xs:nosuchtype", Sequence("1", 12)), Sequence("1", 13))
        );

        await Assert.That(component).IsNull();
        await Assert.That(Codes(fixture.Problems)).StartsWith("NES3202");
        await Assert.That(fixture.Problems.Problems[0].Message).Contains("'v'");
    }

    [Test]
    public async Task Compile_ContentWithAFailingChild_ReportsTheChildAndTheFunctionFails()
    {
        var fixture = CreateWithConstruction();

        var component = fixture.CompileOne(
            Function("f:f", null, 10, Content(null, Sequence("1 +", 12)), Sequence("1", 13))
        );

        await Assert.That(component).IsNull();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo("NES3202/XPST0003");
    }

    [Test]
    [Arguments("xs:string", false)]
    [Arguments("xs:string?", true)]
    [Arguments("xs:string*", true)]
    public async Task Call_NeitherSelectNorContentWithAs_IsTheEmptySequenceConvertedToTheType(
        string type,
        bool accepted
    )
    {
        var fixture = CreateWithConstruction();
        _ = fixture.CompileAll(Function("f:f", null, 10, Content(type), Sequence("$v", 13)));

        var status = Call(fixture, out var result, out var problem);

        await Assert.That(status).IsEqualTo(accepted ? QueryStatus.Ok : QueryStatus.Problem);
        await Assert
            .That(accepted ? result.Count == 0 : problem.Message.Contains("XTTE0570", System.StringComparison.Ordinal))
            .IsTrue();
    }

    [Test]
    public async Task Call_NeitherSelectNorContentNorAs_IsTheEmptyString()
    {
        var fixture = CreateWithConstruction();
        _ = fixture.CompileAll(Function("f:f", null, 10, Content(null), Sequence("$v", 13)));

        var status = Call(fixture, out var result, out _);

        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        await Assert.That(MiniQuery.Text(result)).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Execute_OnARunWithoutValueHooks_IsTheUnsupportedConstructError()
    {
        var fixture = CreateWithConstruction();
        var node = ChildOfDeclaration(Function("f:f", null, 10, Content(null, Sequence("1", 12))));
        var executable = fixture.Compile(node, fixture.FunctionScope, XsltPosition.Instruction);
        using var run = new FakeQueryRun();
        var frame = new XsltFrame(run);

        var status = executable!.TryExecute(frame, new FakeSequenceSink());

        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(frame.TakeProblem().Code).IsEqualTo(SchematronDiagnosticCodes.XsltUnsupported);
    }
}
