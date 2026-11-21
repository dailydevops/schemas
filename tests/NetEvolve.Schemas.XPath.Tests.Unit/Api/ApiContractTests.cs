namespace NetEvolve.Schemas.XPath.Tests.Unit.Api;

using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;

public class ApiContractTests
{
    [Test]
    public async Task Expression_HasNoPublicConstructor() =>
        await Assert.That(typeof(XPathExpression).GetConstructors()).IsEmpty();

    [Test]
    public async Task Members_DoNotThrowTheNotImplementedException()
    {
        var statics = XPathStaticContext.Default;
        using var context = new XPathEvaluationContext();
        var compiled = XPathExpression.TryCompile("1".AsSpan(), statics);
        var expression = compiled.Expression!;

        await Assert.That(compiled.Success).IsTrue();
        await Assert.That(XPathExpression.TryCompile("1").Success).IsTrue();
        await Assert.That(XPathExpression.TryCompile("1", statics).Success).IsTrue();
        await Assert.That(XPathExpression.Compile("1").Source).IsEqualTo("1");
        await Assert.That(expression.Source).IsEqualTo("1");
        await Assert.That(expression.Version).IsEqualTo(XPathVersion.V20);
        await Assert.That(expression.TryEvaluate(context).Status).IsEqualTo(XPathEvaluationStatus.Ok);
        await Assert.That(expression.Evaluate(context).Count).IsEqualTo(1);
    }

    [Test]
    public async Task CompileResult_OfTheSameExpression_IsEqual()
    {
        var result = XPathExpression.TryCompile("1");
        var same = new XPathCompileResult(result.Expression, result.Diagnostic);
        var other = XPathExpression.TryCompile("1");

        await Assert.That(result == same).IsTrue();
        await Assert.That(result != other).IsTrue();
    }

    [Test]
    public async Task Source_AndVersion_AreTheStateOfTheCompiledExpression()
    {
        var expression = XPathExpression.Compile("  1 +  2 ");

        await Assert.That(expression.Source).IsEqualTo("  1 +  2 ");
        await Assert.That(expression.Version).IsEqualTo(XPathVersion.V20);
    }
}
