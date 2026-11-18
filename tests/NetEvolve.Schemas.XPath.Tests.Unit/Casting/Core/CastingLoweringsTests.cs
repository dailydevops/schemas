namespace NetEvolve.Schemas.XPath.Tests.Unit.Casting.Core;

using NetEvolve.Schemas.XPath.Casting.Core;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Syntax.Parser;
using NetEvolve.Schemas.XPath.Tests.Unit.Types;

/// <summary>The registration of the lowerings over the default casting table.</summary>
public class CastingLoweringsTests
{
    [Test]
    public async Task Register_OverTheDefaultTable_LowersCastAndCastable()
    {
        var registry = TypeFixture.Registry();
        CastingLowerings.Register(registry);

        const string expression = "'12' cast as xs:integer";
        _ = XPathParser.TryParse(expression.AsSpan(), out var syntax, out _);
        var compiled = ExpressionCompiler.TryCompile(
            syntax!,
            expression,
            TypeFixture.Statics,
            registry,
            out var cast,
            out _
        );
        using var context = new XPathEvaluationContext();

        await Assert.That(compiled).IsTrue();
        await Assert.That(CastFixture.Render(cast!.Evaluate(context)[0])).IsEqualTo("integer:12");
        await Assert.That(() => CastingLowerings.Register(registry)).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Register_WithoutARegistry_Throws() =>
        await Assert.That(() => CastingLowerings.Register(null!)).Throws<ArgumentNullException>();
}
