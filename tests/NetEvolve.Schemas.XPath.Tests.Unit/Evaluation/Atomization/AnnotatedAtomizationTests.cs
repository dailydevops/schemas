namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Atomization;

using System.Threading.Tasks;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Syntax.Parser;
using NetEvolve.Schemas.XPath.Tests.Unit.Api;
using NetEvolve.Schemas.XPath.Tests.Unit.Functions.Arrays;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>Pins that the annotation source of the evaluation context reaches every atomization site (#791).</summary>
public class AnnotatedAtomizationTests
{
    // The text of an element decides its typed value: "5" is the integer 100, "1 2" is the list (1, 2), "nil" is empty, "boom" has none.
    private const string Document = "<r><a>5</a><a>7</a><l>1 2</l><n>nil</n><c>boom</c><b>9</b></r>";

    private static string Run(string expression, bool annotated, bool compatibility = false)
    {
        var compiled = compatibility
            ? CompileCompatible(expression)
            : ArrayFixture.TryCompile(expression, XPathVersion.V31, out _);
        using var context = new XPathEvaluationContext();
        context.SetContextItem(XdmItem.FromNode(ApiFixture.Load(Document)));
        if (annotated)
        {
            context.Annotations = new TextAnnotations();
        }

        try
        {
            return ApiFixture.Render(compiled!.Evaluate(context));
        }
        catch (XPathException exception)
        {
            return "error:" + exception.Diagnostic.QName.LocalName;
        }
    }

    private static CompiledExpression? CompileCompatible(string expression)
    {
        _ = XPathParser.TryParse(expression.AsSpan(), XPathVersion.V20, out var syntax, out _);
        var statics = ApiFixture.Builder().WithXPath10Compatibility(true).Build();
        _ = ExpressionCompiler.TryCompile(
            syntax!,
            expression,
            statics,
            XPathEngine.CreateRegistry(XPathEngine.AllSlots),
            out var compiled,
            out _
        );
        return compiled;
    }

    [Test]
    [Arguments("(/r/a)[1] eq 100")]
    [Arguments("(/r/a)[1] = 100")]
    [Arguments("(/r/a)[1] + 1 = 101")]
    [Arguments("-(/r/a)[1] = -100")]
    [Arguments("count(1 to (/r/a)[1]) = 100")]
    [Arguments("(/r/a)[1] || 'x' = '100x'")]
    [Arguments("map { (/r/a)[1] : 'v' }(100) = 'v'")]
    [Arguments("((/r/a)[1] cast as xs:integer) = 100")]
    [Arguments("(/r/a)[1] castable as xs:integer")]
    [Arguments("abs((/r/a)[1]) = 100")]
    [Arguments("(/r/a)[1]/number() = 100")]
    [Arguments("string(array:get(array:sort([(/r/a)[1], (/r/b)[1]]), 1)) = '9'")]
    [Arguments("map { 100 : 'v' }((/r/a)[1]) = 'v'")]
    [Arguments("(/r/l) = 2")]
    [Arguments("count(data(/r/l)) = 2")]
    [Arguments("count(data(((/r/a)[1], /r/l))) = 3")]
    [Arguments("empty(data(/r/n))")]
    public async Task Atomize_ContextWithAnnotations_AppliesTheTypedValue(string expression) =>
        await Assert.That(Run(expression, annotated: true)).IsEqualTo("true");

    [Test]
    [Arguments("(/r/a)[1] eq 100")]
    [Arguments("(/r/a)[1] + 1 = 101")]
    [Arguments("-(/r/a)[1] = -100")]
    [Arguments("abs((/r/a)[1]) = 100")]
    [Arguments("(/r/a)[1]/number() = 100")]
    [Arguments("(/r/a)[1] = 100")]
    public async Task Atomize_ContextWithoutAnnotations_KeepsTheUntypedDefault(string expression) =>
        await Assert.That(Run(expression, annotated: false)).IsNotEqualTo("true");

    [Test]
    [Arguments("(/r/a)[1] = 100")]
    [Arguments("(/r/a)[1] + 1 = 101")]
    [Arguments("-(/r/a)[1] = -100")]
    [Arguments("abs((/r/a)[1]) = 100")]
    [Arguments("(/r/a)[1]/number() = 100")]
    public async Task Atomize_CompatibilityModeWithAnnotations_AppliesTheTypedValue(string expression) =>
        await Assert.That(Run(expression, annotated: true, compatibility: true)).IsEqualTo("true");

    [Test]
    public async Task Atomize_ArrayLookupWithTypedPosition_UsesTheTypedValue() =>
        await Assert.That(Run("[1, 2, 3, 4, 5, 6]((/r/a)[1])", annotated: true)).IsEqualTo("error:FOAY0001");

    [Test]
    public async Task Atomize_ElementOnlyContent_RaisesFoty0012() =>
        await Assert.That(Run("(/r/c)[1] = 1", annotated: true)).IsEqualTo("error:FOTY0012");

    [Test]
    public async Task UntypedAnnotationSource_IsNilled_IsAlwaysFalse()
    {
        _ = XdmItem.FromNode(ApiFixture.Load(Document)).TryGetNode(out var node);

        await Assert.That(UntypedAnnotationSource.Instance.IsNilled(node)).IsFalse();
    }

    [Test]
    public async Task Annotations_Default_IsUntypedAndNullIsRejected()
    {
        using var context = new XPathEvaluationContext();

        await Assert.That(context.Annotations).IsSameReferenceAs(UntypedAnnotationSource.Instance);
        await Assert.That(() => context.Annotations = null!).Throws<System.ArgumentNullException>();
    }

    private sealed class TextAnnotations : IXdmTypeAnnotationSource
    {
        public XdmTypeId GetTypeAnnotation(in XPathNode node) => node.TypeAnnotation;

        public bool TryGetTypedValue(in XPathNode node, out XdmItem typedValue)
        {
            typedValue = default;
            return false;
        }

        public bool TryGetTypedValues(in XPathNode node, out XdmSequence values)
        {
            values = XdmSequence.Empty;
            if (node.Node.Kind != XmlNodeKind.Element)
            {
                return false;
            }

            switch (node.StringValue())
            {
                case "5":
                    values = XdmSequence.Create(XdmItem.FromInteger(100));
                    return true;
                case "9":
                    values = XdmSequence.Create(XdmItem.FromInteger(50));
                    return true;
                case "1 2":
                    values = XdmSequence.Create([XdmItem.FromInteger(1), XdmItem.FromInteger(2)]);
                    return true;
                case "nil":
                    return true;
                default:
                    return false;
            }
        }

        public bool HasTypedValue(in XPathNode node) => node.StringValue() != "boom";

        public bool IsNilled(in XPathNode node) => false;
    }
}
