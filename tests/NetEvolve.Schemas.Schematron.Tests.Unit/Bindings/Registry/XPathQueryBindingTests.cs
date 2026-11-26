namespace NetEvolve.Schemas.Schematron.Tests.Unit.Bindings.Registry;

using System;
using System.Linq;
using System.Threading;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Schematron.Bindings.Registry;
using NetEvolve.Schemas.Schematron.Bindings.Xslt1;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Xml.Model.Loading;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>
/// The XPath bindings on the real XPath engine (SCH-55, #311): compiling with the variables of a scope, the slots an expression
/// reads, binding values by slot, the focus, and the problems of both phases.
/// </summary>
public class XPathQueryBindingTests
{
    private const string BaseUri = "file:///schema.sch";

    private static readonly SchematronNamespace Xs = new(
        new DocumentLocation(BaseUri, 1, 1),
        "xs",
        "http://www.w3.org/2001/XMLSchema"
    );

    private static readonly XPathNode Document = Load("<r><a>1</a><a>2</a><b/></r>");

    private static XPathNode Load(string xml) => new(XmlDocumentLoader.Load(xml.AsSpan()).Document);

    private static QueryStaticScope Scope(string[]? names = null, string? declaredType = null) =>
        new([Xs], names ?? [], BaseUri, SchematronEdition.Iso2016, declaredType);

    private static XPathQueryBinding Xpath2() => new XPathQueryBinding("xpath2", allowsXsltInstructions: false);

    private static XPath10QueryBinding Xslt() => new XPath10QueryBinding("xslt", allowsXsltInstructions: true);

    private static IQueryRun Begin() => BeginWith(default);

    private static IQueryRun BeginWith(CancellationToken token) =>
        Xpath2().BeginRun(new QueryResources(DenyAllResourceResolver.Instance), token);

    private static QueryExpression Compile(
        IQueryBinding binding,
        string text,
        string[]? names = null,
        string? declaredType = null,
        QueryExpressionKind kind = QueryExpressionKind.Boolean
    )
    {
        var ok = binding.TryCompile(
            text.AsSpan(),
            kind,
            Scope(names, declaredType),
            out var expression,
            out var problem
        );

        return ok ? expression : throw new InvalidOperationException(problem.Message);
    }

    private static SchematronProblem CompileProblem(string text, string[]? names = null)
    {
        var ok = Xpath2().TryCompile(text.AsSpan(), QueryExpressionKind.Boolean, Scope(names), out _, out var problem);

        return ok ? throw new InvalidOperationException("The expression compiles: " + text) : problem;
    }

    private static bool Evaluate(IQueryRun run, QueryExpression expression, int position = 1, int size = 1)
    {
        var status = run.TryEvaluateBoolean(expression, new QueryFocus(Document, position, size), out var value);

        return status == QueryStatus.Ok ? value : throw new InvalidOperationException(run.GetProblem().Message);
    }

    private static QueryStaticScope ScopeWith(params SchematronNamespace[] namespaces) =>
        new(namespaces, [], BaseUri, SchematronEdition.Iso2016);

    private static bool TryCompileIn(IQueryBinding binding, string text, QueryStaticScope scope) =>
        binding.TryCompile(text.AsSpan(), QueryExpressionKind.Value, scope, out _, out _);

    [Test]
    public async Task Compile_UndeclaredXsPrefix_ResolvesToTheXmlSchemaNamespace()
    {
        var compiled = TryCompileIn(Xpath2(), "xs:decimal('1.5') gt 1", ScopeWith());

        await Assert.That(compiled).IsTrue();
    }

    [Test]
    public async Task Compile_SchemaDeclaredXsPrefix_KeepsItsOwnBinding()
    {
        var other = new SchematronNamespace(new DocumentLocation(BaseUri, 1, 1), "xs", "urn:other");

        var compiled = TryCompileIn(Xpath2(), "xs:decimal('1.5')", ScopeWith(other));

        await Assert.That(compiled).IsFalse();
    }

    [Test]
    public async Task Compile_UndeclaredXsPrefix_StaysUnboundInXPath10Mode()
    {
        var compiled = TryCompileIn(Xslt(), "xs:decimal('1.5')", ScopeWith());

        await Assert.That(compiled).IsFalse();
    }

    [Test]
    public async Task Compile_ExpressionWithoutXsPrefix_IsUnchanged()
    {
        var compiled = TryCompileIn(Xpath2(), "1 + 1 = 2", ScopeWith());

        await Assert.That(compiled).IsTrue();
    }

    [Test]
    public async Task Compile_ValidExpression_EvaluatesAgainstTheFocusNode()
    {
        var binding = Xpath2();
        using var run = Begin();

        await Assert.That(Evaluate(run, Compile(binding, "count(r/a) = 2"))).IsTrue();
        await Assert.That(Evaluate(run, Compile(binding, "count(r/a) = 3"))).IsFalse();
        await Assert.That(Evaluate(run, Compile(binding, "r/c"))).IsFalse();
    }

    [Test]
    public async Task Compile_SyntaxError_IsExpressionInvalidWithTheXPathCode()
    {
        var problem = CompileProblem("r/a[");

        await Assert.That(problem.Code).IsEqualTo(SchematronDiagnosticCodes.ExpressionInvalid);
        await Assert.That(problem.Constraint).IsEqualTo("XPST0003");
        await Assert.That(problem.Location.Source).IsEqualTo(BaseUri);
    }

    [Test]
    public async Task Compile_UnknownFunction_IsExpressionInvalid()
    {
        var problem = CompileProblem("nope()");

        await Assert.That(problem.Code).IsEqualTo(SchematronDiagnosticCodes.ExpressionInvalid);
        await Assert.That(problem.Constraint).IsEqualTo("XPST0017");
    }

    [Test]
    public async Task Compile_UndeclaredVariable_IsVariableUndefinedNeverExpressionInvalid()
    {
        var problem = CompileProblem("$nope", ["a"]);

        await Assert.That(problem.Code).IsEqualTo(SchematronDiagnosticCodes.VariableUndefined);
        await Assert.That(problem.Constraint).IsEqualTo("XPST0008");
    }

    [Test]
    public async Task Compile_ProblemOnASecondLine_CarriesLineAndColumnWithinTheText()
    {
        var problem = CompileProblem("1 +\n  nope()");

        await Assert.That(problem.Location.Line).IsEqualTo(2);
        await Assert.That(problem.Location.Column).IsGreaterThan(1);
    }

    [Test]
    public async Task Compile_NullScope_Throws() =>
        await Assert
            .That(() => Xpath2().TryCompile("1".AsSpan(), QueryExpressionKind.Boolean, null!, out _, out _))
            .Throws<ArgumentNullException>();

    [Test]
    [Arguments("$a + $b", new[] { "a", "b" }, new[] { 0, 1 })]
    [Arguments("$b", new[] { "a", "b" }, new[] { 1 })]
    [Arguments("$a and $a", new[] { "a" }, new[] { 0 })]
    [Arguments("for $a in (1, 2) return $a", new[] { "a" }, new int[0])]
    [Arguments("some $a in (1, 2) satisfies $a = $b", new[] { "a", "b" }, new[] { 1 })]
    [Arguments("$a", new[] { "x", "a", "a" }, new[] { 2 })]
    [Arguments("$a", new[] { "a", "" }, new[] { 0 })]
    [Arguments("1 = 1", new[] { "a" }, new int[0])]
    [Arguments("1 = 1", new[] { "", "" }, new int[0])]
    [Arguments("$a + 1", new[] { "", "a" }, new[] { 1 })]
    public async Task Compile_ReferencedSlots_AreTheSlotsOfTheVariablesTheExpressionReads(
        string text,
        string[] names,
        int[] expected
    )
    {
        var expression = Compile(Xpath2(), text, names);

        await Assert.That(expression.ReferencedSlots.ToArray()).IsEquivalentTo(expected);
    }

    [Test]
    public async Task Evaluate_VariableBoundByExpression_IsReadBySlot()
    {
        var binding = Xpath2();
        using var run = Begin();
        var value = Compile(binding, "count(r/a)", [], kind: QueryExpressionKind.Value);

        var status = run.TryBindVariable(0, value, new QueryFocus(Document, 1, 1));

        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        await Assert.That(Evaluate(run, Compile(binding, "$n = 2", ["n"]))).IsTrue();
    }

    [Test]
    public async Task Evaluate_VariableBoundByLiteral_IsAString()
    {
        var binding = Xpath2();
        using var run = Begin();

        run.BindVariable(0, "DE");

        await Assert.That(Evaluate(run, Compile(binding, "$country = 'DE'", ["country"]))).IsTrue();
        await Assert.That(Evaluate(run, Compile(binding, "$country instance of xs:string", ["country"]))).IsTrue();
    }

    [Test]
    public async Task Evaluate_TwoSlotsWithOneName_EachExpressionReadsItsOwnSlot()
    {
        var binding = Xpath2();
        using var run = Begin();
        run.BindVariable(0, "outer");
        run.BindVariable(1, "inner");

        // The inner declaration shadows the outer one only for the expression compiled where both are visible.
        await Assert.That(Evaluate(run, Compile(binding, "$x = 'inner'", ["x", "x"]))).IsTrue();
        await Assert.That(Evaluate(run, Compile(binding, "$x = 'outer'", ["x"]))).IsTrue();
        await Assert.That(Evaluate(run, Compile(binding, "$x = 'outer'", ["x", "x"]))).IsFalse();
    }

    [Test]
    public async Task Evaluate_SlotsFarApart_GrowTheValueTable()
    {
        var binding = Xpath2();
        using var run = Begin();
        var names = Enumerable.Repeat(string.Empty, 101).ToArray();
        names[40] = "near";
        names[100] = "far";
        run.BindVariable(100, "last");
        run.BindVariable(40, "middle");

        await Assert.That(Evaluate(run, Compile(binding, "$far = 'last' and $near = 'middle'", names))).IsTrue();
    }

    [Test]
    public async Task Evaluate_SlotRebound_ReadsTheNewValue()
    {
        var binding = Xpath2();
        using var run = Begin();
        var expression = Compile(binding, "$v = 'second'", ["v"]);

        run.BindVariable(0, "first");
        await Assert.That(Evaluate(run, expression)).IsFalse();
        run.BindVariable(0, "second");
        await Assert.That(Evaluate(run, expression)).IsTrue();
    }

    [Test]
    public async Task Evaluate_DeclaredTypeMismatch_IsLetType()
    {
        var binding = Xpath2();
        using var run = Begin();
        var value = Compile(binding, "'abc'", [], "xs:integer", QueryExpressionKind.Value);

        var status = run.TryBindVariable(0, value, new QueryFocus(Document, 1, 1));
        var problem = run.GetProblem();

        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(problem.Code).IsEqualTo(SchematronDiagnosticCodes.LetType);
        await Assert.That(problem.Constraint).IsEqualTo("XPTY0004");
    }

    [Test]
    [Arguments("5", "xs:integer", true)]
    [Arguments("5", "xs:double", true)]
    [Arguments("1.5", "xs:double", true)]
    [Arguments("'a'", "xs:string", true)]
    [Arguments("true()", "xs:boolean", true)]
    [Arguments("xs:anyURI('u')", "xs:string", true)]
    [Arguments("1.5", "xs:integer", false)]
    [Arguments("'a'", "xs:boolean", false)]
    [Arguments("(1, 2)", "xs:integer", false)]
    [Arguments("()", "xs:integer", false)]
    [Arguments("r/a", "xs:string", false)]
    [Arguments("xs:date('2020-01-01')", "xs:string", false)]
    [Arguments("xs:date('2020-01-01')", "xs:date", false)]
    [Arguments("5", "xs:boolean", false)]
    [Arguments("xs:float(1)", "xs:double", true)]
    [Arguments("1.5", "xs:decimal", false)]
    public async Task Evaluate_DeclaredType_FollowsTheFunctionConversionRules(string text, string type, bool accepted)
    {
        using var run = Begin();
        var value = Compile(Xpath2(), text, [], type, QueryExpressionKind.Value);

        var status = run.TryBindVariable(0, value, new QueryFocus(Document, 1, 1));

        await Assert.That(status == QueryStatus.Ok).IsEqualTo(accepted);
    }

    [Test]
    public async Task Evaluate_DynamicError_IsEvaluationFailedWithTheXPathCode()
    {
        using var run = Begin();
        var expression = Compile(Xpath2(), "xs:integer('abc') = 1");

        var status = run.TryEvaluateBoolean(expression, new QueryFocus(Document, 1, 1), out _);
        var problem = run.GetProblem();

        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(problem.Code).IsEqualTo(SchematronDiagnosticCodes.EvaluationFailed);
        await Assert.That(problem.Constraint).IsEqualTo("FORG0001");
    }

    [Test]
    public async Task Evaluate_FailingValueOfAVariable_IsAProblemAndBindsNothing()
    {
        var binding = Xpath2();
        using var run = Begin();
        var value = Compile(binding, "xs:integer('abc')", [], null, QueryExpressionKind.Value);

        var status = run.TryBindVariable(0, value, new QueryFocus(Document, 1, 1));
        var read = run.TryEvaluateBoolean(Compile(binding, "$v = 1", ["v"]), new QueryFocus(Document, 1, 1), out _);

        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(read).IsEqualTo(QueryStatus.Problem);
        await Assert.That(run.GetProblem().Constraint).IsEqualTo("XPDY0002");
    }

    [Test]
    public async Task Evaluate_UnboundVariable_IsVariableUndefined()
    {
        using var run = Begin();
        var expression = Compile(Xpath2(), "$v = 1", ["v"]);

        var status = run.TryEvaluateBoolean(expression, new QueryFocus(Document, 1, 1), out _);

        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(run.GetProblem().Code).IsEqualTo(SchematronDiagnosticCodes.VariableUndefined);
        await Assert.That(run.GetProblem().Constraint).IsEqualTo("XPDY0002");
    }

    [Test]
    public async Task Evaluate_NoEffectiveBooleanValue_IsEvaluationFailed()
    {
        using var run = Begin();
        var expression = Compile(Xpath2(), "(1, 2)");

        var status = run.TryEvaluateBoolean(expression, new QueryFocus(Document, 1, 1), out _);

        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(run.GetProblem().Constraint).IsEqualTo("FORG0006");
    }

    [Test]
    public async Task Evaluate_Focus_PositionAndSizeReachTheExpression()
    {
        var binding = Xpath2();
        using var run = Begin();
        var expression = Compile(binding, "position() = 2 and last() = 5");

        await Assert.That(Evaluate(run, expression, 2, 5)).IsTrue();
        await Assert.That(Evaluate(run, expression, 3, 5)).IsFalse();
    }

    [Test]
    public async Task Evaluate_UnknownSize_IsTakenAsThePosition()
    {
        using var run = Begin();

        await Assert.That(Evaluate(run, Compile(Xpath2(), "last() = position()"), 1, -1)).IsTrue();
    }

    [Test]
    public async Task Evaluate_FocusWithoutItem_HasNoContextItem()
    {
        using var run = Begin();
        var expression = Compile(Xpath2(), "1 = 1");
        var literal = run.TryEvaluateBoolean(expression, default, out var value);
        var contextual = run.TryEvaluateBoolean(Compile(Xpath2(), "."), default, out _);

        await Assert.That(literal).IsEqualTo(QueryStatus.Ok);
        await Assert.That(value).IsTrue();
        await Assert.That(contextual).IsEqualTo(QueryStatus.Problem);
        await Assert.That(run.GetProblem().Constraint).IsEqualTo("XPDY0002");
    }

    [Test]
    public async Task Evaluate_Items_AreDeliveredToTheSink()
    {
        using var run = Begin();
        var sink = new CountingSink();

        var status = run.TryEvaluate(
            Compile(Xpath2(), "r/a", [], null, QueryExpressionKind.Nodes),
            new QueryFocus(Document, 1, 1),
            sink
        );

        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        await Assert.That(sink.Items).IsEqualTo(2);
    }

    [Test]
    public async Task Evaluate_ItemsOfAFailingExpression_ReachNoSinkAndSetTheProblem()
    {
        using var run = Begin();
        var sink = new CountingSink();

        var status = run.TryEvaluate(
            Compile(Xpath2(), "r/a/xs:integer(.) div 0"),
            new QueryFocus(Document, 1, 1),
            sink
        );

        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(sink.Items).IsEqualTo(0);
        await Assert.That(run.GetProblem().Code).IsEqualTo(SchematronDiagnosticCodes.EvaluationFailed);
    }

    [Test]
    public async Task Evaluate_ValueOfAnAtomicItem_IsDeliveredAsAnItem()
    {
        using var run = Begin();
        var sink = new CountingSink();

        _ = run.TryEvaluate(Compile(Xpath2(), "1 + 1"), new QueryFocus(Document, 1, 1), sink);

        await Assert.That(sink.Items).IsEqualTo(1);
    }

    [Test]
    public async Task Evaluate_CompatibilityBinding_ComparesLikeXPath1()
    {
        using var xslt = Xslt().BeginRun(new QueryResources(DenyAllResourceResolver.Instance), default);
        using var xpath2 = Begin();
        const string text = "'abc' = 1";

        // XPath 1.0 converts the string to a number and gives false; XPath 2.0 raises XPTY0004.
        await Assert.That(Evaluate(xslt, Compile(Xslt(), text))).IsFalse();
        var status = xpath2.TryEvaluateBoolean(Compile(Xpath2(), text), new QueryFocus(Document, 1, 1), out _);

        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(xpath2.GetProblem().Constraint).IsEqualTo("XPTY0004");
    }

    [Test]
    public async Task Evaluate_CompatibilityBinding_ReadsVariablesBySlot()
    {
        var binding = Xslt();
        using var run = binding.BeginRun(new QueryResources(DenyAllResourceResolver.Instance), default);
        run.BindVariable(0, "x");

        await Assert.That(Evaluate(run, Compile(binding, "$v = 'x'", ["v"]))).IsTrue();
    }

    [Test]
    public async Task Evaluate_CancelledToken_Throws()
    {
        using var source = new CancellationTokenSource();
        using var run = BeginWith(source.Token);
        var expression = Compile(Xpath2(), "1 = 1");
#if NET8_0_OR_GREATER
        await source.CancelAsync();
#else
        source.Cancel();
#endif

        await Assert
            .That(() => run.TryEvaluateBoolean(expression, new QueryFocus(Document, 1, 1), out _))
            .Throws<OperationCanceledException>();
    }

    [Test]
    public async Task BeginRun_NullResources_Throws() =>
        await Assert.That(() => Xpath2().BeginRun(null!, default)).Throws<ArgumentNullException>();

    [Test]
    public async Task Constructor_NullName_Throws() =>
        await Assert.That(() => new XPathQueryBinding(null!, false)).Throws<ArgumentNullException>();

    [Test]
    public async Task TryEvaluateBoolean_NullExpression_Throws()
    {
        using var run = Begin();

        await Assert
            .That(() => run.TryEvaluateBoolean(null!, new QueryFocus(Document, 1, 1), out _))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task TryEvaluate_NullExpression_Throws()
    {
        using var run = Begin();

        await Assert
            .That(() => run.TryEvaluate(null!, new QueryFocus(Document, 1, 1), new CountingSink()))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task TryEvaluate_NullSink_Throws()
    {
        using var run = Begin();
        var expression = Compile(Xpath2(), "r/a", [], null, QueryExpressionKind.Nodes);

        await Assert
            .That(() => run.TryEvaluate(expression, new QueryFocus(Document, 1, 1), null!))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task TryBindVariable_NullValue_Throws()
    {
        using var run = Begin();

        await Assert
            .That(() => run.TryBindVariable(0, null!, new QueryFocus(Document, 1, 1)))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task BindVariable_NullValue_Throws()
    {
        using var run = Begin();

        await Assert.That(() => run.BindVariable(0, null!)).Throws<ArgumentNullException>();
    }

    private sealed class CountingSink : ISequenceSink
    {
        internal int Items { get; private set; }

        public SequenceSinkKind Kind => SequenceSinkKind.Items;

        public NetEvolve.Schemas.Xml.Contracts.IXmlSink Content => throw new NotSupportedException();

        public void Item(in NetEvolve.Schemas.XPath.Xdm.Values.XdmItem item) => Items++;

        public void Text(ReadOnlySpan<char> text) { }

        public NetEvolve.Schemas.Xml.Contracts.IXmlSink BeginTree() => throw new NotSupportedException();

        public NetEvolve.Schemas.XPath.Xdm.Values.XdmItem EndTree(NetEvolve.Schemas.Xml.Contracts.IXmlSink tree) =>
            throw new NotSupportedException();
    }
}
