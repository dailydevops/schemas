namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Globals;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Globals;
using NetEvolve.Schemas.XPath.Api;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions.FunctionTree;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Globals.GlobalFixture;

/// <summary>
/// Reading a top-level <c>xsl:variable</c> or <c>xsl:param</c> (XSLT 2.0 section 9.5): the name, the <see langword="as"/> type, the
/// <see langword="select"/> default, the required parameter, and every static error with its code and location. The global is the
/// same registry entry as the local variable and the parameter of a function; the parent of the element tells them apart.
/// </summary>
public class GlobalCompileTests
{
    [Test]
    public async Task Variable_AtTopLevel_ReadsNameTypeSelectAndLocation()
    {
        var fixture = new GlobalFixture();

        var global = fixture.CompileOne(X("variable", 10, A("name", "g"), A("select", "1 + 1"), A("as", "xs:integer")));

        await Assert.That(fixture.Problems.Problems.Count).IsEqualTo(0);
        await Assert.That(global).IsNotNull();
        await Assert.That(global!.Name).IsEqualTo(new XPathQName(string.Empty, "g"));
        var declaration = global.Declaration;
        await Assert.That(declaration.Name).IsEqualTo("g");
        await Assert.That(declaration.Value).IsEqualTo("1 + 1");
        await Assert.That(declaration.DeclaredType).IsEqualTo("xs:integer");
        await Assert.That(declaration.IsParameter).IsFalse();
        await Assert.That(declaration.IsRequired).IsFalse();
        await Assert.That(declaration.Location.Line).IsEqualTo(10);
    }

    [Test]
    public async Task Variable_WithoutSelect_IsTheEmptyString()
    {
        var fixture = new GlobalFixture();

        var global = fixture.CompileOne(X("variable", 10, A("name", "g")));

        await Assert.That(global!.Declaration.Value).IsEqualTo("''");
        await Assert.That(global.Declaration.DeclaredType).IsNull();
    }

    [Test]
    public async Task Variable_WithAPrefixedName_ExpandsTheNamespaceOfTheElement()
    {
        var fixture = new GlobalFixture();

        var global = fixture.CompileOne(X("variable", 10, A("name", "f:g"), A("select", "1")));

        await Assert.That(global!.Name).IsEqualTo(new XPathQName(Functions, "g"));
        await Assert.That(global.Declaration.Name).IsEqualTo("f:g");
    }

    [Test]
    public async Task Variable_WithAPrefixThatIsNotBound_IsXtse0280()
    {
        var fixture = new GlobalFixture();

        var global = fixture.CompileOne(X("variable", 10, A("name", "nope:g"), A("select", "1")));

        await Assert.That(global).IsNull();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo("NES3006/XTSE0280");
        await Assert.That(fixture.Problems.Problems[0].Location.Line).IsEqualTo(10);
    }

    [Test]
    public async Task Variable_WithoutAName_IsAMissingAttribute()
    {
        var fixture = new GlobalFixture();

        var global = fixture.CompileOne(X("variable", 10, A("select", "1")));

        await Assert.That(global).IsNull();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo("NES3005");
    }

    [Test]
    public async Task Variable_WithANameThatIsNotAQName_IsXtse0020()
    {
        var fixture = new GlobalFixture();

        var global = fixture.CompileOne(X("variable", 10, A("name", "1x"), A("select", "1")));

        await Assert.That(global).IsNull();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo("NES3006/XTSE0020");
    }

    [Test]
    public async Task Variable_WithATypeThatIsNotSupported_IsReportedAndNotCompiled()
    {
        var fixture = new GlobalFixture();

        var global = fixture.CompileOne(X("variable", 10, A("name", "g"), A("select", "1"), A("as", "xs:date")));

        await Assert.That(global).IsNull();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo("NES3007");
    }

    [Test]
    public async Task Variable_WithSelectAndContent_IsXtse0620()
    {
        var fixture = new GlobalFixture();

        var global = fixture.CompileOne(X("variable", 10, A("name", "g"), A("select", "1"), "text"));

        await Assert.That(global).IsNull();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo("NES3003/XTSE0620");
    }

    [Test]
    public async Task Variable_WithContent_IsRecordedForTheBodyCompileAndHasNoSelectValue()
    {
        var fixture = new GlobalFixture();

        var global = fixture.CompileOne(X("variable", 10, A("name", "g"), "text"));

        await Assert.That(fixture.Problems.Problems.Count).IsEqualTo(0);
        await Assert.That(global!.Declaration.Value).IsNull();
        await Assert.That(global.Declaration.ContentSource).IsNotNull();
        await Assert.That(global.Declaration.IsRequired).IsFalse();
    }

    [Test]
    public async Task Variable_WithTheStaticAttribute_IsNotSupportedBecauseItBelongsToDef28()
    {
        var fixture = new GlobalFixture();

        var global = fixture.CompileOne(X("variable", 10, A("name", "g"), A("select", "1"), A("static", "yes")));

        await Assert.That(global).IsNull();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo("NES3204");
        await Assert.That(fixture.Problems.Problems[0].Message).Contains("static");
    }

    [Test]
    public async Task Param_WithSelect_IsOptionalWithTheDefault()
    {
        var fixture = new GlobalFixture();

        var global = fixture.CompileOne(X("param", 10, A("name", "p"), A("select", "'d'")));

        await Assert.That(fixture.Problems.Problems.Count).IsEqualTo(0);
        var declaration = global!.Declaration;
        await Assert.That(declaration.IsParameter).IsTrue();
        await Assert.That(declaration.IsRequired).IsFalse();
        await Assert.That(declaration.Value).IsEqualTo("'d'");
    }

    [Test]
    public async Task Param_WithoutSelectAndWithoutAType_IsOptionalWithTheEmptyString()
    {
        var fixture = new GlobalFixture();

        var global = fixture.CompileOne(X("param", 10, A("name", "p")));

        await Assert.That(global!.Declaration.IsRequired).IsFalse();
        await Assert.That(global.Declaration.Value).IsEqualTo("''");
    }

    [Test]
    public async Task Param_RequiredYes_HasNoDefaultAndIsRequired()
    {
        var fixture = new GlobalFixture();

        var global = fixture.CompileOne(X("param", 10, A("name", "p"), A("required", "yes")));

        await Assert.That(global!.Declaration.IsRequired).IsTrue();
        await Assert.That(global.Declaration.Value).IsNull();
    }

    [Test]
    public async Task Param_RequiredNo_IsOptional()
    {
        var fixture = new GlobalFixture();

        var global = fixture.CompileOne(X("param", 10, A("name", "p"), A("required", "no")));

        await Assert.That(global!.Declaration.IsRequired).IsFalse();
    }

    [Test]
    public async Task Param_WithATypeAndWithoutSelect_IsRequiredBecauseTheEmptySequenceDoesNotMatchTheType()
    {
        var fixture = new GlobalFixture();

        var global = fixture.CompileOne(X("param", 10, A("name", "p"), A("as", "xs:string")));

        await Assert.That(global!.Declaration.IsRequired).IsTrue();
        await Assert.That(global.Declaration.Value).IsNull();
        await Assert.That(global.Declaration.DeclaredType).IsEqualTo("xs:string");
    }

    [Test]
    public async Task Param_WithATypeAndASelect_IsOptional()
    {
        var fixture = new GlobalFixture();

        var global = fixture.CompileOne(X("param", 10, A("name", "p"), A("as", "xs:string"), A("select", "'d'")));

        await Assert.That(global!.Declaration.IsRequired).IsFalse();
        await Assert.That(global.Declaration.Value).IsEqualTo("'d'");
    }

    [Test]
    public async Task Param_RequiredWithSelect_IsAGeneralStaticError()
    {
        var fixture = new GlobalFixture();

        var global = fixture.CompileOne(X("param", 10, A("name", "p"), A("required", "yes"), A("select", "1")));

        await Assert.That(global).IsNull();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo("NES3006/XTSE0010");
        await Assert.That(fixture.Problems.Problems[0].Location.Line).IsEqualTo(10);
    }

    [Test]
    public async Task Param_RequiredWithAWordThatIsNotYesOrNo_IsXtse0020()
    {
        var fixture = new GlobalFixture();

        var global = fixture.CompileOne(X("param", 10, A("name", "p"), A("required", "maybe")));

        await Assert.That(global).IsNull();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo("NES3006/XTSE0020");
    }

    [Test]
    public async Task Param_WithContent_IsAnOptionalParameterWithoutSelectValue()
    {
        var fixture = new GlobalFixture();

        var global = fixture.CompileOne(X("param", 10, A("name", "p"), "text"));

        await Assert.That(fixture.Problems.Problems.Count).IsEqualTo(0);
        await Assert.That(global!.Declaration.IsParameter).IsTrue();
        await Assert.That(global.Declaration.IsRequired).IsFalse();
        await Assert.That(global.Declaration.Value).IsNull();
        await Assert.That(global.Declaration.ContentSource).IsNotNull();
    }

    [Test]
    public async Task Param_WithSelectAndContent_IsXtse0620()
    {
        var fixture = new GlobalFixture();

        var global = fixture.CompileOne(X("param", 10, A("name", "p"), A("select", "1"), "text"));

        await Assert.That(global).IsNull();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo("NES3003/XTSE0620");
    }

    [Test]
    public async Task Param_WithoutAName_IsAMissingAttribute()
    {
        var fixture = new GlobalFixture();

        var global = fixture.CompileOne(X("param", 10, A("select", "1")));

        await Assert.That(global).IsNull();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo("NES3005");
    }

    [Test]
    [Arguments("stylesheet")]
    [Arguments("transform")]
    public async Task IsTopLevel_AChildOfAStylesheetModuleRoot_IsTrue(string root)
    {
        var node = DeclarationsUnder(root, Xslt, X("variable", 10, A("name", "g")))[0];

        await Assert.That(XsltGlobalCompile.IsTopLevel(node)).IsTrue();
    }

    [Test]
    [Arguments("schema")]
    [Arguments("library")]
    public async Task IsTopLevel_AChildOfASchematronRoot_IsTrue(string root)
    {
        var node = DeclarationsUnder(root, Schematron, X("variable", 10, A("name", "g")))[0];

        await Assert.That(XsltGlobalCompile.IsTopLevel(node)).IsTrue();
    }

    [Test]
    public async Task IsTopLevel_AChildOfAFunction_IsFalse()
    {
        var node = DeclarationsUnder(
            "schema",
            Schematron,
            X("function", 10, A("name", "f:x"), X("variable", 11, A("name", "v")))
        )[0];
        var variable = new NetEvolve.Schemas.XPath.Xdm.Nodes.XPathNode(node.Node.FirstChild);

        await Assert.That(XsltGlobalCompile.IsTopLevel(variable)).IsFalse();
    }

    [Test]
    public async Task IsTopLevel_ASchemaElementOfAnotherNamespaceOrAnotherName_IsFalse()
    {
        var other = DeclarationsUnder("schema", "urn:other", X("variable", 10, A("name", "g")))[0];
        var misnamed = DeclarationsUnder("pattern", Schematron, X("variable", 10, A("name", "g")))[0];
        var inXsltOther = DeclarationsUnder("template", Xslt, X("variable", 10, A("name", "g")))[0];

        await Assert.That(XsltGlobalCompile.IsTopLevel(other)).IsFalse();
        await Assert.That(XsltGlobalCompile.IsTopLevel(misnamed)).IsFalse();
        await Assert.That(XsltGlobalCompile.IsTopLevel(inXsltOther)).IsFalse();
    }

    [Test]
    public async Task IsTopLevel_AParentWithoutANamespace_AndTheRootElementItself_AreFalse()
    {
        var noNamespace = DeclarationsUnder("schema", string.Empty, X("variable", 10, A("name", "g")))[0];
        var root = new NetEvolve.Schemas.XPath.Xdm.Nodes.XPathNode(noNamespace.Node.Parent);

        await Assert.That(XsltGlobalCompile.IsTopLevel(noNamespace)).IsFalse();
        await Assert.That(XsltGlobalCompile.IsTopLevel(root)).IsFalse();
    }

    [Test]
    public async Task Registry_VariableIsADeclarationAndAnInstruction_ParamIsADeclarationAndAChild()
    {
        var fixture = new GlobalFixture();
        var variable = new XPathQName(
            NetEvolve.Schemas.Schematron.Xslt.Registry.XsltInstructionRegistry.Namespace,
            "variable"
        );
        var param = new XPathQName(
            NetEvolve.Schemas.Schematron.Xslt.Registry.XsltInstructionRegistry.Namespace,
            "param"
        );

        await Assert
            .That(fixture.Registry.Classify(in variable, XsltPosition.Declaration, out _))
            .IsEqualTo(XsltElementClass.Registered);
        await Assert
            .That(fixture.Registry.Classify(in variable, XsltPosition.Instruction, out _))
            .IsEqualTo(XsltElementClass.Registered);
        await Assert
            .That(fixture.Registry.Classify(in param, XsltPosition.Declaration, out _))
            .IsEqualTo(XsltElementClass.Registered);
        await Assert
            .That(fixture.Registry.Classify(in param, XsltPosition.Child, out _))
            .IsEqualTo(XsltElementClass.Registered);
        await Assert
            .That(fixture.Registry.Classify(in param, XsltPosition.Instruction, out _))
            .IsEqualTo(XsltElementClass.Misplaced);
        await Assert.That(fixture.Registry.IsAvailable(in variable)).IsTrue();
        await Assert.That(fixture.Registry.IsAvailable(in param)).IsFalse();
    }

    [Test]
    public async Task LocalVariableInAFunction_StaysALocalVariable()
    {
        var fixture = new GlobalFixture();
        var node = Declaration(
            X("function", 10, A("name", "f:x"), Fx.Variable("v", "1", null, 11), Fx.Sequence("$v", 12))
        );

        var executable = fixture.Compile(node);

        await Assert.That(fixture.Problems.Problems.Count).IsEqualTo(0);
        await Assert.That(executable).IsTypeOf<XsltFunctionDeclaration>();
    }

    [Test]
    public async Task Declaration_ExecutesAsANoOp_AndRejectsNull()
    {
        var fixture = new GlobalFixture();
        var global = fixture.CompileOne(X("variable", 10, A("name", "g"), A("select", "1")))!;
        using var run = new MiniRun();

        var status = global.TryExecute(new XsltFrame(run), new FakeSequenceSink());

        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        await Assert.That(run.Focuses.Count).IsEqualTo(0);
        await Assert.That(() => global.TryExecute(null!, new FakeSequenceSink())).Throws<ArgumentNullException>();
        await Assert.That(() => global.TryExecute(new XsltFrame(run), null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Guards_RejectNullArguments()
    {
        var fixture = new GlobalFixture();

        await Assert.That(() => XsltGlobalCompile.TryCompile(null!, false, out _)).Throws<ArgumentNullException>();
        await Assert.That(() => new XsltGlobalDeclaration(default, null!)).Throws<ArgumentNullException>();
        await Assert
            .That(() =>
                new NetEvolve.Schemas.Schematron.Xslt.Functions.XsltVariableInstruction().TryCompile(
                    null!,
                    out _,
                    out _
                )
            )
            .Throws<ArgumentNullException>();
        await Assert
            .That(() =>
                new NetEvolve.Schemas.Schematron.Xslt.Functions.XsltParamInstruction().TryCompile(null!, out _, out _)
            )
            .Throws<ArgumentNullException>();
        await Assert.That(() => XsltGlobalCompile.IsTopLevel(default)).Throws<ArgumentNullException>();
        await Assert
            .That(() => XsltVariableInstruction.TryReadDeclaredType(null!, "g", out _))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => XsltVariableInstruction.TryReadDeclaredType(CompileContext(fixture), null!, out _))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => XsltVariableInstruction.CheckContent(null!, false, "xsl:variable"))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => XsltVariableInstruction.CheckContent(CompileContext(fixture), false, null!))
            .Throws<ArgumentNullException>();
        await Assert.That(fixture.Problems.Problems.Count).IsEqualTo(0);
    }

    private static XsltCompileContext CompileContext(GlobalFixture fixture) =>
        new(
            Declaration(X("variable", 10, A("name", "g"))),
            fixture.Scope,
            static _ => null,
            fixture.Problems,
            fixture.Binding
        );
}
