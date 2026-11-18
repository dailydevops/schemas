namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.XPath.Api;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions.FunctionFixture;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions.FunctionTree;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions.Fx;

/// <summary>
/// Compiling <c>xsl:function</c> (design #150 EP-9, EP-13; XSLT 2.0 section 10.3): the name, the <see langword="as"/> types, the parameters,
/// the local variables and <c>xsl:sequence</c> of the body, and every static error with its code and location.
/// </summary>
public class FunctionCompileTests
{
    [Test]
    public async Task Compile_FunctionWithParamsAndBody_ProducesComponentWithNameArityAndSlots()
    {
        var fixture = Create("g1", "g2");

        var component = fixture.CompileOne(
            Function(
                "f:add",
                "xs:integer",
                10,
                Param("a", "xs:integer", 11),
                Param("b", "xs:integer", 12),
                Variable("sum", "$a + $b", null, 13),
                Sequence("$sum", 14)
            )
        );

        await Assert.That(fixture.Problems.Problems.Count).IsEqualTo(0);
        await Assert.That(component).IsNotNull();
        await Assert.That(component!.Name).IsEqualTo(new XPathQName(Functions, "add"));
        await Assert.That(component.Arity).IsEqualTo(2);
        await Assert.That(component.SlotBase).IsEqualTo(2);
        await Assert.That(component.Parameters[0].Name).IsEqualTo("a");
        await Assert.That(component.Parameters[0].Slot).IsEqualTo(2);
        await Assert.That(component.Parameters[1].Slot).IsEqualTo(3);
        await Assert.That(component.SlotCount).IsEqualTo(3);
        await Assert.That(component.Location.Line).IsEqualTo(10);
    }

    [Test]
    public async Task Compile_NameIsMatchedByNamespaceNotPrefix()
    {
        var fixture = Create();

        var component = fixture.CompileOne(Function("f:one", null, 10, Sequence("1", 11)));

        await Assert.That(component!.Name.NamespaceName).IsEqualTo(Functions);
        await Assert.That(component.Name.LocalName).IsEqualTo("one");
    }

    [Test]
    public async Task Compile_ZeroParameters_HasArityZero()
    {
        var component = Create().CompileOne(Function("f:zero", null, 10, Sequence("42", 11)));

        await Assert.That(component!.Arity).IsEqualTo(0);
        await Assert.That(component.SlotCount).IsEqualTo(0);
    }

    [Test]
    public async Task Compile_MissingName_ReportsAttributeMissingAtTheElement()
    {
        var fixture = Create();

        var component = fixture.CompileOne(Function(null, null, 10, Sequence("1", 11)));

        await Assert.That(component).IsNull();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo("NES3005");
        await Assert.That(fixture.Problems.Problems[0].Location.Line).IsEqualTo(10);
    }

    [Test]
    [Arguments("add", "NES3006/XTSE0740")]
    [Arguments("g:add", "NES3006/XTSE0280")]
    [Arguments("xs:add", "NES3006/XTSE0080")]
    [Arguments("xsl:add", "NES3006/XTSE0080")]
    [Arguments("1:add", "NES3006/XTSE0020")]
    [Arguments("f:", "NES3006/XTSE0020")]
    public async Task Compile_InvalidName_ReportsAttributeInvalidWithTheXsltCode(string name, string expected)
    {
        var fixture = Create();

        var component = fixture.CompileOne(Function(name, null, 10, Sequence("1", 11)));

        await Assert.That(component).IsNull();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo(expected);
        await Assert.That(fixture.Problems.Problems[0].Location.Line).IsEqualTo(10);
    }

    [Test]
    [Arguments("xs:integer*")]
    [Arguments("xs:string?")]
    [Arguments("item()*")]
    [Arguments("node()+")]
    [Arguments("empty-sequence()")]
    public async Task Compile_SupportedResultTypes_Compile(string type)
    {
        var fixture = Create();

        var component = fixture.CompileOne(Function("f:t", type, 10, Sequence("()", 11)));

        await Assert.That(fixture.Problems.Problems.Count).IsEqualTo(0);
        await Assert.That(component).IsNotNull();
    }

    [Test]
    [Arguments("xs:integer xs:string", "NES3202/XPST0003")]
    [Arguments("g:integer", "NES3202/XPST0081")]
    [Arguments("xs:nothing", "NES3202/XPST0051")]
    public async Task Compile_InvalidAsType_ReportsExpressionInvalidWithTheXPathCode(string type, string expected)
    {
        var fixture = Create();

        var component = fixture.CompileOne(Function("f:t", type, 10, Sequence("1", 11)));

        await Assert.That(component).IsNull();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo(expected);
        await Assert.That(fixture.Problems.Problems[0].Location.Line).IsEqualTo(10);
    }

    [Test]
    [Arguments("yes", true)]
    [Arguments("no", true)]
    [Arguments("maybe", false)]
    public async Task Compile_OverrideAttribute_AcceptsYesAndNoOnly(string value, bool accepted)
    {
        var fixture = Create();
        var declaration = Function("f:o", null, 10, A("override", value), Sequence("1", 11));

        var component = fixture.CompileOne(declaration);

        await Assert.That(component is not null).IsEqualTo(accepted);
        await Assert.That(Codes(fixture.Problems)).IsEqualTo(accepted ? string.Empty : "NES3006/XTSE0020");
    }

    [Test]
    public async Task Compile_ParamWithSelect_ReportsXtse0760()
    {
        var fixture = Create();

        var component = fixture.CompileOne(
            Function("f:p", null, 10, Param("a", null, 11, A("select", "1")), Sequence("$a", 12))
        );

        await Assert.That(component).IsNull();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo("NES3006/XTSE0760");
        await Assert.That(fixture.Problems.Problems[0].Location.Line).IsEqualTo(11);
    }

    [Test]
    public async Task Compile_ParamWithContent_ReportsXtse0760()
    {
        var fixture = Create();

        var component = fixture.CompileOne(
            Function("f:p", null, 10, Param("a", null, 11, Sequence("1", 12)), Sequence("$a", 13))
        );

        await Assert.That(component).IsNull();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo("NES3003/XTSE0760");
        await Assert.That(fixture.Problems.Problems[0].Location.Line).IsEqualTo(12);
    }

    [Test]
    public async Task Compile_DuplicateParamName_ReportsXtse0580()
    {
        var fixture = Create();

        var component = fixture.CompileOne(
            Function("f:p", null, 10, Param("a", null, 11), Param("a", null, 12), Sequence("$a", 13))
        );

        await Assert.That(component).IsNull();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo("NES3200/XTSE0580");
        await Assert.That(fixture.Problems.Problems[0].Location.Line).IsEqualTo(12);
    }

    [Test]
    public async Task Compile_ParamWithoutName_ReportsAttributeMissing()
    {
        var fixture = Create();

        var component = fixture.CompileOne(Function("f:p", null, 10, Param(null, null, 11), Sequence("1", 12)));

        await Assert.That(component).IsNull();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo("NES3005");
        await Assert.That(fixture.Problems.Problems[0].Location.Line).IsEqualTo(11);
    }

    [Test]
    public async Task Compile_ParamAfterBodyInstruction_IsMisplaced()
    {
        var fixture = Create();

        var component = fixture.CompileOne(Function("f:p", null, 10, Sequence("1", 11), Param("a", null, 12)));

        await Assert.That(component).IsNull();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo("NES3003/XTSE0010");
        await Assert.That(fixture.Problems.Problems[0].Location.Line).IsEqualTo(12);
    }

    [Test]
    public async Task Compile_BodyReadsParam_ResolvesItToTheParamSlot()
    {
        var fixture = Create("g");

        _ = fixture.CompileOne(Function("f:p", null, 10, Param("a", null, 11), Sequence("$a", 12)));

        await Assert.That(fixture.Problems.Problems.Count).IsEqualTo(0);
        var (text, scope) = fixture.Binding.Compiled[0];
        await Assert.That(text).IsEqualTo("$a");
        await Assert.That(MiniBinding.Resolve(scope, "a")).IsEqualTo(1);
        await Assert.That(MiniBinding.Resolve(scope, "g")).IsEqualTo(0);
    }

    [Test]
    public async Task Compile_LocalVariable_IsVisibleToLaterSiblingsOnly()
    {
        var fixture = Create();

        var component = fixture.CompileOne(
            Function("f:v", null, 10, Sequence("$late", 11), Variable("late", "1", null, 12), Sequence("$late", 13))
        );

        await Assert.That(component).IsNull();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo("NES3300/XPST0008");
        await Assert.That(fixture.Binding.Compiled.Count).IsEqualTo(3);
    }

    [Test]
    public async Task Compile_LocalVariable_ShadowsAParamOfTheSameName()
    {
        var fixture = Create();

        var component = fixture.CompileOne(
            Function("f:v", null, 10, Param("a", null, 11), Variable("a", "$a", null, 12), Sequence("$a", 13))
        );

        await Assert.That(fixture.Problems.Problems.Count).IsEqualTo(0);
        await Assert.That(component!.SlotCount).IsEqualTo(2);
        var (_, last) = fixture.Binding.Compiled[1];
        await Assert.That(MiniBinding.Resolve(last, "a")).IsEqualTo(1);
    }

    [Test]
    public async Task Compile_VariableWithAsType_PassesTheCanonicalTypeToTheBinding()
    {
        var fixture = Create();

        _ = fixture.CompileOne(Function("f:v", null, 10, Variable("n", "1", "xs:integer", 11), Sequence("$n", 12)));

        await Assert.That(fixture.Binding.Compiled[0].Scope.DeclaredType).IsEqualTo("xs:integer");
        await Assert.That(fixture.Binding.Compiled[1].Scope.DeclaredType).IsNull();
    }

    [Test]
    public async Task Compile_VariableWithUnsupportedAsType_ReportsUnsupported()
    {
        var fixture = Create();

        var component = fixture.CompileOne(
            Function("f:v", null, 10, Variable("n", "1", "xs:integer*", 11), Sequence("$n", 12))
        );

        await Assert.That(component).IsNull();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo("NES3007");
        await Assert.That(fixture.Problems.Problems[0].Location.Line).IsEqualTo(11);
    }

    [Test]
    public async Task Compile_VariableWithContent_ReportsXsltUnsupported()
    {
        var fixture = Create();

        var component = fixture.CompileOne(
            Function("f:v", null, 10, Variable("n", null, null, 11, "text"), Sequence("1", 12))
        );

        await Assert.That(component).IsNull();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo("NES3204");
        await Assert.That(fixture.Problems.Problems[0].Location.Line).IsEqualTo(11);
    }

    [Test]
    public async Task Compile_SequenceWithoutSelect_ReportsAttributeMissing()
    {
        var fixture = Create();

        var component = fixture.CompileOne(Function("f:s", null, 10, Sequence(null, 11)));

        await Assert.That(component).IsNull();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo("NES3005");
        await Assert.That(fixture.Problems.Problems[0].Location.Line).IsEqualTo(11);
    }

    [Test]
    public async Task Compile_ExpressionError_IsReportedWithItsCodeAndTheFunctionStillFails()
    {
        var fixture = Create();

        var component = fixture.CompileOne(Function("f:s", null, 10, Sequence("1 +", 11)));

        await Assert.That(component).IsNull();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo("NES3202/XPST0003");
    }

    [Test]
    public async Task Compile_UnregisteredInstructionInBody_ReportsXsltUnsupportedAtItsPosition()
    {
        var fixture = Create();

        var component = fixture.CompileOne(
            Function("f:s", null, 10, X("value-of", 11, A("select", "1")), Sequence("1", 12))
        );

        await Assert.That(component).IsNull();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo("NES3204");
        await Assert.That(fixture.Problems.Problems[0].Message).Contains("xsl:value-of");
        await Assert.That(fixture.Problems.Problems[0].Location.Line).IsEqualTo(11);
    }

    [Test]
    public async Task Compile_LiteralResultElementInBody_ReportsXsltUnsupported()
    {
        var fixture = Create();

        var component = fixture.CompileOne(Function("f:s", null, 10, Other("p", "urn:other", 11), Sequence("1", 12)));

        await Assert.That(component).IsNull();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo("NES3204");
    }

    [Test]
    public async Task Compile_TextInBody_WhitespaceIsIgnoredAndOtherTextIsUnsupported()
    {
        var okay = Create();
        var bad = Create();

        var withSpace = okay.CompileOne(Function("f:s", null, 10, "  \n ", Sequence("1", 11), " "));
        var withText = bad.CompileOne(Function("f:s", null, 10, "text", Sequence("1", 11)));

        await Assert.That(withSpace).IsNotNull();
        await Assert.That(withText).IsNull();
        await Assert.That(Codes(bad.Problems)).IsEqualTo("NES3204");
    }

    [Test]
    public async Task Compile_TwoProblemsInOneFunction_AreBothReported()
    {
        var fixture = Create();

        var component = fixture.CompileOne(
            Function("f:s", "xs:nothing", 10, Param("a", null, 11), Param("a", null, 12), Sequence("$b", 13))
        );

        await Assert.That(component).IsNull();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo("NES3202/XPST0051,NES3200/XTSE0580,NES3300/XPST0008");
    }

    [Test]
    public async Task Compile_FunctionInInstructionPosition_IsMisplaced()
    {
        var fixture = Create();
        var node = Declaration(Function("f:s", null, 10, Sequence("1", 11)));

        var executable = fixture.Compile(node, fixture.FunctionScope, XsltPosition.Instruction);

        await Assert.That(executable).IsNull();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo("NES3003/XTSE0010");
        await Assert.That(fixture.Problems.Problems[0].Location.Line).IsEqualTo(10);
    }

    [Test]
    public async Task Compile_ParamAndVariableAsDeclaration_AreMisplacedUntilTheGlobalsPackageExtendsThem()
    {
        var fixture = Create();
        var nodes = Declarations(Param("p", null, 10), Variable("v", "1", null, 11));

        _ = fixture.Compile(nodes[0], fixture.FunctionScope, XsltPosition.Declaration);
        _ = fixture.Compile(nodes[1], fixture.FunctionScope, XsltPosition.Declaration);

        await Assert.That(Codes(fixture.Problems)).IsEqualTo("NES3003/XTSE0010,NES3003/XTSE0010");
    }

    [Test]
    public async Task Compile_FunctionDeclaration_ExecutesAsNoOp()
    {
        var fixture = Create();
        var node = Declaration(Function("f:s", null, 10, Sequence("1", 11)));
        var executable = fixture.Compile(node, fixture.FunctionScope, XsltPosition.Declaration);
        var (run, _) = fixture.CreateRun();

        var status = executable!.TryExecute(new XsltFrame(run), new FakeSequenceSink());

        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        await Assert.That(run.Focuses.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Compile_Failure_ReturnsFalseWithADefaultProblemBecauseEveryProblemIsAlreadyReported()
    {
        var fixture = Create();
        var node = Declaration(Function("f:s", "xs:nothing", 10, Sequence("$b", 11)));
        var context = new XsltCompileContext(
            node,
            fixture.FunctionScope,
            static _ => null,
            fixture.Problems,
            fixture.Binding,
            (child, scope) => fixture.Compile(child, scope, XsltPosition.Instruction)
        );

        var compiled = new XsltFunctionInstruction().TryCompile(context, out _, out var problem);

        await Assert.That(compiled).IsFalse();
        await Assert.That(problem.Code).IsEqualTo(default(DiagnosticCode));
        await Assert.That(Codes(fixture.Problems)).IsEqualTo("NES3202/XPST0051,NES3300/XPST0008");
    }

    [Test]
    public async Task Compile_WithoutABinding_IsAProgrammingError()
    {
        var fixture = Create();
        var node = Declaration(Function("f:s", null, 10, Sequence("1", 11)));
        var context = new XsltCompileContext(
            node,
            fixture.FunctionScope,
            static _ => null,
            fixture.Problems,
            null,
            (child, scope) => fixture.Compile(child, scope, XsltPosition.Instruction)
        );

        await Assert
            .That(() => new XsltFunctionInstruction().TryCompile(context, out _, out _))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Component_Guards_RejectNullArguments()
    {
        await Assert.That(() => new XsltFunctionTableBuilder().TryAdd(null!, 0, out _)).Throws<ArgumentNullException>();
        await Assert.That(() => new XsltFunctionDeclaration(null!)).Throws<ArgumentNullException>();
        await Assert
            .That(() => new XsltFunctionInstruction().TryCompile(null!, out _, out _))
            .Throws<ArgumentNullException>();
    }
}
