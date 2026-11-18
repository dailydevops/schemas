namespace NetEvolve.Schemas.Schematron.Tests.Unit.Scopes;

using NetEvolve.Schemas.Schematron;
using NetEvolve.Schemas.Schematron.Scopes;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Scopes.ScopeFixture;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Scopes.ScopeModel;

/// <summary>
/// The <see langword="as"/> attribute of <see langword="let"/> (design #150 EP-5, #106 Q2): only <c>xs:string</c>, <c>xs:boolean</c>,
/// <c>xs:double</c>, and <c>xs:integer</c> are supported. A constant is checked at Build for the schema and phase level, a
/// non-constant value by the binding at first use, and a constant at pattern or rule level is reported at evaluation.
/// </summary>
public class AsTypeTests
{
    [Test]
    [Arguments("'abc'", "xs:string")]
    [Arguments("5", "xs:integer")]
    [Arguments("1.5", "xs:decimal")]
    [Arguments("1e3", "xs:double")]
    [Arguments(" 'a' ", "xs:string")]
    [Arguments("xs:integer('5')", "xs:integer")]
    [Arguments("xs:date('2020-01-01')", "xs:date")]
    [Arguments("xs:double(5)", "xs:double")]
    public async Task Constant_Definition_IsLexicalLiteralOrTypedConstructor(string text, string type)
    {
        var constant = ConstantValue.TryGetType(text, [], out var actual);

        await Assert.That(constant).IsTrue();
        await Assert.That(actual).IsEqualTo(type);
    }

    [Test]
    [Arguments("$x")]
    [Arguments("1 + 2")]
    [Arguments("-5")]
    [Arguments("(5)")]
    [Arguments("'a' 'b'")]
    [Arguments("")]
    [Arguments("current-dateTime()")]
    [Arguments("string(5)")]
    [Arguments("xs:integer($x)")]
    [Arguments("xs:integer('5', 1)")]
    [Arguments("xs:integer(1 + 1)")]
    [Arguments("fn:string('a')")]
    [Arguments("'abc")]
    public async Task Constant_Definition_RejectsEverythingElse(string text)
    {
        var constant = ConstantValue.TryGetType(text, [], out var type);

        await Assert.That(constant).IsFalse();
        await Assert.That(type).IsNull();
    }

    [Test]
    [Arguments("xs:string", "xs:string", true)]
    [Arguments("xs:integer", "xs:integer", true)]
    [Arguments("xs:integer", "xs:double", true)]
    [Arguments("xs:decimal", "xs:double", true)]
    [Arguments("xs:double", "xs:double", true)]
    [Arguments("xs:anyURI", "xs:string", true)]
    [Arguments("xs:boolean", "xs:boolean", true)]
    [Arguments("xs:string", "xs:integer", false)]
    [Arguments("xs:string", "xs:boolean", false)]
    [Arguments("xs:double", "xs:integer", false)]
    [Arguments("xs:decimal", "xs:integer", false)]
    [Arguments("xs:integer", "xs:string", false)]
    [Arguments("xs:date", "xs:string", false)]
    public async Task Constant_SatisfiesDeclaredType_FollowsFunctionConversionPromotion(
        string constant,
        string declared,
        bool expected
    ) => await Assert.That(ConstantValue.Satisfies(constant, declared)).IsEqualTo(expected);

    [Test]
    [Arguments("xs:string")]
    [Arguments("xs:boolean")]
    [Arguments("xs:double")]
    [Arguments("xs:integer")]
    public async Task SupportedTypes_AreExactlyTheFourOfDecisionQ2(string type)
    {
        var supported = ConstantValue.TryGetSupportedType(type, [], out var canonical);

        await Assert.That(supported).IsTrue();
        await Assert.That(canonical).IsEqualTo(type);
    }

    [Test]
    [Arguments("xs:date")]
    [Arguments("xs:integer?")]
    [Arguments("xs:integer*")]
    [Arguments("item()")]
    [Arguments("integer")]
    [Arguments("")]
    public async Task UnsupportedTypes_AreRejected(string type)
    {
        var supported = ConstantValue.TryGetSupportedType(type, [], out var canonical);

        await Assert.That(supported).IsFalse();
        await Assert.That(canonical).IsNull();
    }

    [Test]
    public async Task UnsupportedAsType_ReportsUnsupported_NamingTheType()
    {
        var let = Let("d", "count(//a)", "xs:date");
        var f = Create(Schema(lets: [let]));

        await Assert.That(f.Ok).IsFalse();
        await Assert.That(f.Problems.Problems.Count).IsEqualTo(1);
        await Assert.That(f.Problems.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.Unsupported);
        await Assert.That(f.Problems.Problems[0].Location).IsEqualTo(let.Location);
        await Assert.That(f.Problems.Problems[0].Message).Contains("xs:date");
    }

    [Test]
    public async Task Scope_AsTypeMismatch_Constant_ThrowsAtBuild()
    {
        var let = Let("n", "'abc'", "xs:integer");
        var f = Create(Schema(lets: [let], patterns: Pattern("p", [], Rule("r", "a"))));

        await Assert.That(f.Ok).IsFalse();
        await Assert.That(f.Problems.Problems.Count).IsEqualTo(1);
        await Assert.That(f.Problems.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.LetType);
        await Assert.That(f.Problems.Problems[0].Location).IsEqualTo(let.Location);
    }

    [Test]
    public async Task Scope_AsTypeMismatch_Constant_PhaseLevel_ThrowsAtBuild()
    {
        var schema = Schema(
            phases: [Phase("ph", [Let("n", "'abc'", "xs:integer")], "p")],
            patterns: Pattern("p", [], Rule("r", "a"))
        );

        var f = Create(schema, "ph");

        await Assert.That(f.Ok).IsFalse();
        await Assert.That(Codes(f.Problems)).IsEqualTo("NES3201");
    }

    [Test]
    [Arguments("'abc'", "xs:string")]
    [Arguments("5", "xs:integer")]
    [Arguments("5", "xs:double")]
    [Arguments("1.5", "xs:double")]
    [Arguments("xs:integer('5')", "xs:integer")]
    [Arguments("xs:boolean('true')", "xs:boolean")]
    public async Task Scope_AsTypeMatch_Constant_ReportsNothing(string value, string type)
    {
        var f = Create(Schema(lets: [Let("n", value, type)]));

        await Assert.That(f.Ok).IsTrue();
        await Assert.That(f.Problems.Problems.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ConstantLet_Literal_IsNotEvaluatedAtBuild()
    {
        var f = Create(Schema(lets: [Let("n", "5", "xs:integer")]));

        await Assert.That(f.Binding.RunsBegun).IsEqualTo(0);
        await Assert.That(f.Plan.Globals[0].StaticProblem).IsNull();
    }

    [Test]
    public async Task NonConstantLet_IsNotEvaluatedAtBuild_AndGetsItsDeclaredTypeAtCompile()
    {
        var f = Create(Schema(lets: [Let("n", "count(//item)", "xs:integer"), Let("m", "count(//item)")]));

        await Assert.That(f.Ok).IsTrue();
        await Assert.That(f.Binding.RunsBegun).IsEqualTo(0);
        await Assert.That(f.Binding.Compiled[0].Scope.DeclaredType).IsEqualTo("xs:integer");
        await Assert.That(f.Binding.Compiled[1].Scope.DeclaredType).IsNull();
    }

    [Test]
    public async Task NonConstantLet_CurrentDateTime_IsNotAConstant()
    {
        var f = Create(Schema(lets: [Let("t", "current-dateTime()", "xs:string")]));

        await Assert.That(f.Ok).IsTrue();
        await Assert.That(f.Plan.Globals[0].StaticProblem).IsNull();
    }

    [Test]
    public async Task RuleLevelConstant_TypeMismatch_IsNotABuildProblem_ButAStaticEvaluateProblem()
    {
        var schema = Schema(
            patterns: Pattern("p", [Let("pn", "'abc'", "xs:integer")], Rule("r", "a", Let("n", "'abc'", "xs:integer")))
        );

        var f = Create(schema);

        await Assert.That(f.Ok).IsTrue();
        await Assert.That(f.Problems.Problems.Count).IsEqualTo(0);
        await Assert
            .That(f.Plan.Patterns[0].Lets[0].StaticProblem!.Value.Code)
            .IsEqualTo(SchematronDiagnosticCodes.LetType);
        await Assert
            .That(f.Plan.Patterns[0].Rules[0].Lets[0].StaticProblem!.Value.Code)
            .IsEqualTo(SchematronDiagnosticCodes.LetType);
    }

    private const string XmlSchema = "http://www.w3.org/2001/XMLSchema";

    [Test]
    public async Task SupportedType_ThroughAnotherPrefixBoundToTheSchemaNamespace_IsRecognizedByNamespace()
    {
        var f = Create(Schema(lets: [Let("n", "count(//a)", "xsd:integer")], namespaces: [Ns("xsd", XmlSchema)]));

        await Assert.That(f.Ok).IsTrue();
        await Assert.That(f.Binding.Compiled[0].Scope.DeclaredType).IsEqualTo("xs:integer");
    }

    [Test]
    public async Task SupportedType_PrefixXs_IsPredeclared_WithoutANsElement()
    {
        var f = Create(Schema(lets: [Let("n", "count(//a)", "xs:integer")]));

        await Assert.That(f.Ok).IsTrue();
        await Assert.That(f.Binding.Compiled[0].Scope.DeclaredType).IsEqualTo("xs:integer");
    }

    [Test]
    public async Task SupportedType_PrefixXsBoundToAnotherNamespace_IsUnsupported()
    {
        var f = Create(Schema(lets: [Let("n", "count(//a)", "xs:integer")], namespaces: [Ns("xs", "urn:other")]));

        await Assert.That(f.Ok).IsFalse();
        await Assert.That(Codes(f.Problems)).IsEqualTo("NES3007");
    }

    [Test]
    public async Task SupportedType_UnboundPrefix_IsUnsupported()
    {
        var f = Create(Schema(lets: [Let("n", "count(//a)", "xsd:integer")]));

        await Assert.That(f.Ok).IsFalse();
        await Assert.That(Codes(f.Problems)).IsEqualTo("NES3007");
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task DuplicatePrefix_LastDeclarationWins_AsInTheXPathStaticContext(bool schemaNamespaceLast)
    {
        var good = Ns("xs", XmlSchema);
        var other = Ns("xs", "urn:other");

        var found = ConstantValue.TryGetSupportedType(
            "xs:integer",
            schemaNamespaceLast ? [other, good] : [good, other],
            out _
        );

        await Assert.That(found).IsEqualTo(schemaNamespaceLast);
    }

    [Test]
    public async Task Constant_ConstructorThroughAnotherPrefix_IsRecognizedByNamespace()
    {
        var constant = ConstantValue.TryGetType("xsd:integer('5')", [Ns("xsd", XmlSchema)], out var type);

        await Assert.That(constant).IsTrue();
        await Assert.That(type).IsEqualTo("xs:integer");
    }

    [Test]
    public async Task Constant_ConstructorWithPrefixBoundToAnotherNamespace_IsNotAConstant()
    {
        var other = ConstantValue.TryGetType("xs:integer('5')", [Ns("xs", "urn:other")], out _);
        var unbound = ConstantValue.TryGetType("xsd:integer('5')", [], out _);

        await Assert.That(other).IsFalse();
        await Assert.That(unbound).IsFalse();
    }

    [Test]
    public async Task Scope_AsTypeMismatch_ConstantThroughAnotherPrefix_ThrowsAtBuild()
    {
        var f = Create(Schema(lets: [Let("n", "'abc'", "xsd:integer")], namespaces: [Ns("xsd", XmlSchema)]));

        await Assert.That(f.Ok).IsFalse();
        await Assert.That(Codes(f.Problems)).IsEqualTo("NES3201");
    }
}
