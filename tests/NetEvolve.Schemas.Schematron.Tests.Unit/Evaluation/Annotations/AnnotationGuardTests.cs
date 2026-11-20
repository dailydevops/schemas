namespace NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Annotations;

using NetEvolve.Schemas.Schematron.Evaluation.Annotations;
using NetEvolve.Schemas.Schematron.Evaluation.Assertions;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Assertions;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>Every internal boundary of the annotation package rejects null arguments.</summary>
public class AnnotationGuardTests
{
    private static readonly QueryStaticScope Scope = new([], [], null, SchematronEdition.Iso2016);

    [Test]
    public async Task Compiler_NullArguments_Throw()
    {
        var binding = new AssertionTestBinding();
        var problems = new FakeProblemSink();

        await Assert
            .That(() => AnnotationCompiler.TryCompile(null, null, null, default, null!, binding, problems, out _))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => AnnotationCompiler.TryCompile(null, null, null, default, Scope, null!, problems, out _))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => AnnotationCompiler.TryCompile(null, null, null, default, Scope, binding, null!, out _))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Compiler_WithoutAttributes_ReturnsTheSharedEmptyInstance()
    {
        var ok = AnnotationCompiler.TryCompile(
            null,
            [],
            null,
            default,
            Scope,
            new AssertionTestBinding(),
            new FakeProblemSink(),
            out var compiled
        );

        await Assert.That(ok).IsTrue();
        await Assert.That(compiled).IsSameReferenceAs(CompiledAnnotations.None);
    }

    [Test]
    public async Task CompiledAnnotation_NullArguments_Throw()
    {
        var expression = new AssertionTestExpression("$a", QueryExpressionKind.Value, []);

        await Assert
            .That(() => CompiledAnnotation.CreateStatic(AnnotationKind.Role, null!, default))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => CompiledAnnotation.CreateDynamic(AnnotationKind.Role, null!, expression, default))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => CompiledAnnotation.CreateDynamic(AnnotationKind.Role, "$a", null!, default))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task CompiledAnnotation_StaticAndDynamic_CarryTheirParts()
    {
        var expression = new AssertionTestExpression("$a", QueryExpressionKind.Value, []);

        var fixedValue = CompiledAnnotation.CreateStatic(AnnotationKind.Flag, "x y", default);
        var dynamic = CompiledAnnotation.CreateDynamic(AnnotationKind.Severity, "$a", expression, default);

        await Assert.That(fixedValue.Kind).IsEqualTo(AnnotationKind.Flag);
        await Assert.That(fixedValue.Text).IsEqualTo("x y");
        await Assert.That(fixedValue.Source).IsNull();
        await Assert.That(fixedValue.Expression).IsNull();
        await Assert.That(dynamic.Kind).IsEqualTo(AnnotationKind.Severity);
        await Assert.That(dynamic.Text).IsNull();
        await Assert.That(dynamic.Source).IsEqualTo("$a");
        await Assert.That(dynamic.Expression).IsSameReferenceAs(expression);
    }

    [Test]
    public async Task CompiledAnnotations_StaticValues_AreTheEventValues_AndDynamicOnesAreEmpty()
    {
        var expression = new AssertionTestExpression("$a", QueryExpressionKind.Value, []);
        var fixedAnnotations = new CompiledAnnotations(
            CompiledAnnotation.CreateStatic(AnnotationKind.Role, "r", default),
            CompiledAnnotation.CreateStatic(AnnotationKind.Flag, "a b", default),
            null
        );
        var dynamicAnnotations = new CompiledAnnotations(
            CompiledAnnotation.CreateDynamic(AnnotationKind.Role, "$a", expression, default),
            CompiledAnnotation.CreateDynamic(AnnotationKind.Flag, "$a", expression, default),
            null
        );

        await Assert.That(fixedAnnotations.StaticRole).IsEqualTo("r");
        await Assert.That(fixedAnnotations.StaticFlags).IsEqualTo("a b");
        await Assert.That(dynamicAnnotations.StaticRole).IsEqualTo(string.Empty);
        await Assert.That(dynamicAnnotations.StaticFlags).IsEqualTo(string.Empty);
        await Assert.That(CompiledAnnotations.None.StaticRole).IsEqualTo(string.Empty);
        await Assert.That(CompiledAnnotations.None.StaticFlags).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task ResolvedAnnotations_NullArguments_Throw()
    {
        await Assert.That(() => new ResolvedAnnotations(null!, "", "")).Throws<ArgumentNullException>();
        await Assert.That(() => new ResolvedAnnotations("", null!, "")).Throws<ArgumentNullException>();
        await Assert.That(() => new ResolvedAnnotations("", "", null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Evaluator_Constructor_NullArguments_Throw()
    {
        using var h = new AssertionHarness();

        await Assert
            .That(() => new AnnotationEvaluator(null!, h.Binder, h.Problems, 10))
            .Throws<ArgumentNullException>();
        await Assert.That(() => new AnnotationEvaluator(h.Run, null!, h.Problems, 10)).Throws<ArgumentNullException>();
        await Assert.That(() => new AnnotationEvaluator(h.Run, h.Binder, null!, 10)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Evaluator_Constructor_NegativeMaximum_Throws()
    {
        using var h = new AssertionHarness();

        await Assert
            .That(() => new AnnotationEvaluator(h.Run, h.Binder, h.Problems, -1))
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Evaluator_Methods_NullArguments_Throw()
    {
        using var h = new AssertionHarness();
        var evaluator = h.Evaluator.Annotations;
        var focus = new QueryFocus(AssertionTree.Node("order1"), 1, 1);

        await Assert.That(() => evaluator.BeginPattern(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => evaluator.BeginRule(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => evaluator.Resolve(null!, "c", in focus)).Throws<ArgumentNullException>();
        await Assert
            .That(() => evaluator.Resolve(CompiledAnnotations.None, null!, in focus))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Evaluator_WithoutAnyAnnotation_ResolvesEmptyValues()
    {
        using var h = new AssertionHarness();
        var focus = new QueryFocus(AssertionTree.Node("order1"), 1, 1);

        var resolved = h.Evaluator.Annotations.Resolve(CompiledAnnotations.None, "c", in focus);

        await Assert.That(resolved.Role).IsEqualTo(string.Empty);
        await Assert.That(resolved.Flags).IsEqualTo(string.Empty);
        await Assert.That(resolved.Severity).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Assertion_WithoutBeginRuleOrBeginPattern_InheritsNothing()
    {
        using var h = new AssertionHarness();
        var assertion = h.Compile(AnnotationModel.Failing(role: "own"));

        _ = h.Evaluate(assertion);

        await Assert.That(h.Sink.Findings[0].Role).IsEqualTo("own");
        await Assert.That(h.Sink.Findings[0].Flags).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task SingleValueSink_NullTextSink_Throws()
    {
        var sink = new SingleValueSink();

        await Assert.That(() => sink.Begin(null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task SingleValueSink_IsATextSink_ThatNeverBuildsATree()
    {
        var sink = new SingleValueSink();

        await Assert.That(sink.Kind).IsEqualTo(SequenceSinkKind.Text);
        await Assert.That(() => sink.Content).Throws<InvalidOperationException>();
        await Assert.That(() => sink.BeginTree()).Throws<InvalidOperationException>();
        await Assert.That(() => sink.EndTree(null!)).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task SingleValueSink_ForwardsOnlyTheFirstValue_AndCountsEveryOne()
    {
        using var text = new TextSink(100);
        var sink = new SingleValueSink();
        var second = XdmItem.FromString("never");
        sink.Begin(text);

        sink.Text("first".AsSpan());
        sink.Text("second".AsSpan());
        sink.Item(in second);

        await Assert.That(sink.Count).IsEqualTo(3);
        await Assert.That(text.ToString()).IsEqualTo("first");
    }

    [Test]
    public async Task SingleValueSink_Begin_StartsANewCount()
    {
        using var text = new TextSink(100);
        var sink = new SingleValueSink();
        var item = XdmItem.FromString("x");
        sink.Begin(text);
        sink.Item(in item);

        sink.Begin(text);

        await Assert.That(sink.Count).IsEqualTo(0);
    }
}
