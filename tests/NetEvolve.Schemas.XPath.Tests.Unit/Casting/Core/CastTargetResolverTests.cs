namespace NetEvolve.Schemas.XPath.Tests.Unit.Casting.Core;

using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Syntax.Parser;
using NetEvolve.Schemas.XPath.Tests.Unit.Types;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The target type of a cast is a name in the default element namespace when it has no prefix (XPath 2.0, 3.12.3).</summary>
public class CastTargetResolverTests
{
    private static readonly XPathStaticContext SchemaDefault = TypeFixture
        .Statics.ToBuilder()
        .WithDefaultElementNamespace(TypeFixture.Xs)
        .Build();

    private static CompiledExpression? Compile(string expression, out XPathDiagnostic diagnostic)
    {
        _ = XPathParser.TryParse(expression.AsSpan(), out var syntax, out diagnostic);
        _ = ExpressionCompiler.TryCompile(
            syntax!,
            expression,
            SchemaDefault,
            CastFixture.Registry(),
            out var compiled,
            out diagnostic
        );
        return compiled;
    }

    [Test]
    public async Task UnprefixedTarget_ResolvesInTheDefaultElementNamespace()
    {
        var compiled = Compile("'12' cast as integer", out _);

        await Assert.That(compiled).IsNotNull();

        using var context = new XPathEvaluationContext();
        await Assert.That(CastFixture.Render(compiled!.Evaluate(context)[0])).IsEqualTo("integer:12");
    }

    [Test]
    public async Task UnprefixedTarget_WithoutARow_NamesTheTypeAsWritten()
    {
        var compiled = Compile("'x' cast as long", out var diagnostic);

        await Assert.That(compiled).IsNull();
        await Assert.That(diagnostic.Code).IsEqualTo("XPST0051");
        await Assert.That(diagnostic.Message).Contains("'long'");
    }
}
