namespace NetEvolve.Schemas.XPath.Tests.Unit.Casting.Core;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Casting.Core;
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

    private static CompiledExpression? Compile(
        string expression,
        out XPathDiagnostic diagnostic,
        XPathStaticContext? statics = null,
        CastingTable? table = null
    )
    {
        _ = XPathParser.TryParse(expression.AsSpan(), out var syntax, out diagnostic);
        _ = ExpressionCompiler.TryCompile(
            syntax!,
            expression,
            statics ?? SchemaDefault,
            CastFixture.Registry(table),
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
        var table = new CastingCatalog();
        var compiled = Compile(
            "'x' cast as dateTimeStamp",
            out var diagnostic,
            SchemaDefault.ToBuilder().WithVersion(XPathVersion.V31).Build(),
            table.Table
        );

        await Assert.That(compiled).IsNull();
        await Assert.That(diagnostic.Code).IsEqualTo("XPST0051");
        await Assert.That(diagnostic.Message).Contains("'dateTimeStamp'");
    }

    [Test]
    public async Task UnprefixedDateTimeStamp_ResolvesInXPath31()
    {
        var compiled = Compile(
            "'2020-01-02T03:04:05Z' cast as dateTimeStamp",
            out _,
            SchemaDefault.ToBuilder().WithVersion(XPathVersion.V31).Build()
        );

        await Assert.That(compiled).IsNotNull();
    }
}
