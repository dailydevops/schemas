namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Keys;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Patterns;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Keys;
using NetEvolve.Schemas.Schematron.Xslt.Registry;
using NetEvolve.Schemas.XPath.Api;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions.FunctionTree;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Keys.KeyFixture;

/// <summary>The compile of <c>xsl:key</c> (XSLT 2.0 section 16.3): name, match through the shared matcher, use or content, collation.</summary>
public class KeyInstructionTests
{
    [Test]
    public async Task Compile_WithUseAttribute_KeepsNameMatchAndUse()
    {
        var fixture = new KeyFixture();

        var key = fixture.CompileOne(Kx.Key("k", "item[@group = 'g1']", "@code", 10));

        await Assert.That(key).IsNotNull();
        await Assert.That(key!.Name).IsEqualTo(new XPathQName(string.Empty, "k"));
        await Assert.That(key.DisplayName).IsEqualTo("k");
        await Assert.That(key.Use).IsNotNull();
        await Assert.That(key.Body).IsNull();
        await Assert.That(key.Location.Line).IsEqualTo(10);
        await Assert.That(fixture.Problems.Problems).IsEmpty();
    }

    [Test]
    public async Task Compile_Match_IsCompiledByTheSharedPatternMatcher()
    {
        var fixture = new KeyFixture();

        var key = fixture.CompileOne(Kx.Key("k", "item[@group = 'g1']", "@code", 10));

        // The predicate text reached the binding through the shared matcher, and the plan is the one of the shared matcher.
        await Assert.That(fixture.Binding.Compiled).Contains("@group = 'g1'");
        await Assert.That(key!.Match).IsTypeOf<PatternPlan>();
    }

    [Test]
    public async Task Compile_WithContent_KeepsTheBodyAndNoUse()
    {
        var fixture = new KeyFixture();

        var key = fixture.CompileOne(Kx.Key("k", "*", null, 10, Kx.Text("key", 11)));

        await Assert.That(key).IsNotNull();
        await Assert.That(key!.Body).IsNotNull();
        await Assert.That(key.Use).IsNull();
        await Assert.That(fixture.Problems.Problems).IsEmpty();
    }

    [Test]
    public async Task Compile_PrefixedName_ExpandsTheNamespace()
    {
        var fixture = new KeyFixture();

        var key = fixture.CompileOne(Kx.Key("f:k", "*", "'1'", 10));

        await Assert.That(key!.Name).IsEqualTo(new XPathQName(Functions, "k"));
        await Assert.That(key.DisplayName).IsEqualTo("f:k");
    }

    [Test]
    public async Task Compile_UnboundPrefix_IsXtse0280()
    {
        var fixture = new KeyFixture();

        var key = fixture.CompileOne(Kx.Key("zz:k", "*", "'1'", 10));

        await Assert.That(key).IsNull();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo(SchematronDiagnosticCodes.AttributeInvalid + "/XTSE0280");
        await Assert.That(fixture.Problems.Problems[0].Location.Line).IsEqualTo(10);
    }

    [Test]
    public async Task Compile_NameThatIsNotAQName_IsXtse0020()
    {
        var fixture = new KeyFixture();

        var key = fixture.CompileOne(Kx.Key("1k", "*", "'1'", 10));

        await Assert.That(key).IsNull();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo(SchematronDiagnosticCodes.AttributeInvalid + "/XTSE0020");
    }

    [Test]
    public async Task Compile_WithoutName_IsAMissingAttribute()
    {
        var fixture = new KeyFixture();

        var key = fixture.CompileOne(Kx.Key(null, "*", "'1'", 10));

        await Assert.That(key).IsNull();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo(SchematronDiagnosticCodes.AttributeMissing.ToString());
    }

    [Test]
    public async Task Compile_WithoutMatch_IsAMissingAttribute()
    {
        var fixture = new KeyFixture();

        var key = fixture.CompileOne(Kx.Key("k", null, "'1'", 10));

        await Assert.That(key).IsNull();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo(SchematronDiagnosticCodes.AttributeMissing.ToString());
        await Assert.That(fixture.Problems.Problems[0].Message).Contains("match");
    }

    [Test]
    public async Task Compile_MatchStartingWithKey_IsCompiledThroughTheSharedMatcher()
    {
        var fixture = new KeyFixture();

        // key() may start the pattern of xsl:key as it may start a rule context (#780): the call is compiled as an expression.
        var key = fixture.CompileOne(Kx.Key("k", "key('k', 'v')/c", "'1'", 10));

        await Assert.That(key).IsNotNull();
        await Assert.That(fixture.Problems.Problems).IsEmpty();
        await Assert.That(fixture.Binding.Compiled).Contains("key('k', 'v')");
    }

    [Test]
    public async Task Compile_MatchOutsideThePatternSubset_IsTheUnsupportedPatternError()
    {
        var fixture = new KeyFixture();

        // id() as a pattern start stays outside the subset (#67), as does a key() call with a computed argument.
        var key = fixture.CompileOne(Kx.Key("k", "key('k', @id)", "'1'", 10));

        await Assert.That(key).IsNull();
        await Assert.That(Codes(fixture.Problems)).StartsWith(SchematronDiagnosticCodes.PatternUnsupported.ToString());
    }

    [Test]
    public async Task Compile_UseAndContent_IsXtse1205()
    {
        var fixture = new KeyFixture();

        var key = fixture.CompileOne(Kx.Key("k", "*", "'1'", 10, Kx.Text("key", 11)));

        await Assert.That(key).IsNull();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo(SchematronDiagnosticCodes.ElementMisplaced + "/XTSE1205");
    }

    [Test]
    public async Task Compile_NeitherUseNorContent_IsXtse1205()
    {
        var fixture = new KeyFixture();

        var key = fixture.CompileOne(Kx.Key("k", "*", null, 10));

        await Assert.That(key).IsNull();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo(SchematronDiagnosticCodes.AttributeMissing + "/XTSE1205");
    }

    [Test]
    public async Task Compile_UseThatDoesNotCompile_IsReportedAsTheBindingGaveIt()
    {
        var fixture = new KeyFixture();

        var key = fixture.CompileOne(Kx.Key("k", "*", "invalid", 10));

        await Assert.That(key).IsNull();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo(SchematronDiagnosticCodes.ExpressionInvalid + "/XPST0003");
    }

    [Test]
    public async Task Compile_ContentThatDoesNotCompile_IsReported()
    {
        var fixture = new KeyFixture();

        var key = fixture.CompileOne(
            Kx.Key("k", "*", null, 10, FunctionTree.X("if", 11, FunctionTree.A("test", "invalid")))
        );

        await Assert.That(key).IsNull();
        await Assert.That(Codes(fixture.Problems)).Contains("XPST0003");
    }

    [Test]
    public async Task Compile_EveryProblemIsReported_NotOnlyTheFirst()
    {
        var fixture = new KeyFixture();

        var key = fixture.CompileOne(Kx.Key("zz:k", null, null, 10));

        await Assert.That(key).IsNull();
        await Assert.That(fixture.Problems.Problems.Count).IsEqualTo(3);
    }

    [Test]
    public async Task Compile_CodepointCollation_IsAccepted()
    {
        var fixture = new KeyFixture();

        var key = fixture.CompileOne(
            Kx.Key("k", "*", "'1'", 10, FunctionTree.A("collation", XsltKeyInstruction.CodepointCollation))
        );

        await Assert.That(key).IsNotNull();
        await Assert.That(fixture.Problems.Problems).IsEmpty();
    }

    [Test]
    public async Task Compile_OtherCollation_IsAnExplicitError()
    {
        var fixture = new KeyFixture();

        var key = fixture.CompileOne(
            Kx.Key("k", "*", "'1'", 10, FunctionTree.A("collation", "http://example.com/collation"))
        );

        await Assert.That(key).IsNull();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo(SchematronDiagnosticCodes.XsltUnsupported + "/XTSE1210");
        await Assert.That(fixture.Problems.Problems[0].Message).Contains("http://example.com/collation");
    }

    [Test]
    public async Task Compile_CollationWithATemplateExpression_IsReportedAsNotSupported()
    {
        var fixture = new KeyFixture();

        var key = fixture.CompileOne(Kx.Key("k", "*", "'1'", 10, FunctionTree.A("collation", "{$c}")));

        await Assert.That(key).IsNull();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo(SchematronDiagnosticCodes.XsltUnsupported.ToString());
    }

    [Test]
    public async Task Compile_CompositeAttribute_IsReportedAsNotSupported()
    {
        var fixture = new KeyFixture();

        var key = fixture.CompileOne(Kx.Key("k", "*", "'1'", 10, FunctionTree.A("composite", "yes")));

        await Assert.That(key).IsNull();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo(SchematronDiagnosticCodes.XsltUnsupported.ToString());
        await Assert.That(fixture.Problems.Problems[0].Message).Contains("composite");
    }

    [Test]
    public async Task Compile_TheSlotsOfMatchAndUse_AreTheSlotsOfTheComponent()
    {
        var fixture = new KeyFixture("one", "two");

        var key = fixture.CompileOne(Kx.Key("k", "item[@code = $two]", "$one", 10));

        await Assert.That(key!.ReferencedSlots.ToArray()).IsEquivalentTo([0, 1]);
    }

    [Test]
    public async Task Compile_WithoutABinding_IsAnInvalidOperation()
    {
        var fixture = new KeyFixture();
        var node = FunctionTree.Declaration(Kx.Key("k", "*", "'1'", 10));
        var context = new XsltCompileContext(node, fixture.Scope, _ => null, fixture.Problems);

        await Assert
            .That(() => new XsltKeyInstruction().TryCompile(context, out _, out _))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Compile_WithANullContext_Throws() =>
        await Assert
            .That(() => new XsltKeyInstruction().TryCompile(null!, out _, out _))
            .Throws<ArgumentNullException>();

    [Test]
    public async Task Declaration_Executes_ToNothing()
    {
        var fixture = new KeyFixture();
        var declaration = (XsltKeyDeclaration)fixture.Compile(Declaration(Kx.Key("k", "*", "'1'", 10)))!;
        var frame = new XsltFrame(fixture.Binding.BeginRun(null!, default));

        var status = declaration.TryExecute(frame, new FakeSequenceSink());

        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
    }

    [Test]
    public async Task Registry_ResolvesXslKey_AsADeclarationOnly()
    {
        var fixture = new KeyFixture();
        var name = new XPathQName(XsltInstructionRegistry.Namespace, "key");

        await Assert.That(fixture.Registry.TryGet(in name, out var instruction)).IsTrue();
        await Assert.That(instruction.Position).IsEqualTo(XsltPosition.Declaration);
        await Assert.That(fixture.Registry.IsAvailable(name)).IsFalse();
    }

    [Test]
    public async Task Registry_XslKeyInASequenceConstructor_IsMisplaced()
    {
        var fixture = new KeyFixture();

        var executable = fixture.Compile(Declaration(Kx.Key("k", "*", "'1'", 10)), XsltPosition.Instruction);

        await Assert.That(executable).IsNull();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo(SchematronDiagnosticCodes.ElementMisplaced + "/XTSE0010");
    }

    [Test]
    public async Task Registration_RegistersExactlyOneElement_AndRejectsASecondRegistration()
    {
        var builder = new XsltRegistryBuilder();
        Registration.Register(builder);

        await Assert.That(() => Registration.Register(builder)).Throws<InvalidOperationException>();
        await Assert.That(() => Registration.Register(null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Registration_ModuleInitializer_StagesIntoTheSharedCatalog() =>
        await Assert
            .That(
                XsltRegistryCatalog.Shared.Default.TryGet(
                    new XPathQName(XsltInstructionRegistry.Namespace, "key"),
                    out _
                )
            )
            .IsTrue();
}
