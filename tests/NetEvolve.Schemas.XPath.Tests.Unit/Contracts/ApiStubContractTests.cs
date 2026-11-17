namespace NetEvolve.Schemas.XPath.Tests.Unit.Contracts;

using System;
using System.Runtime.CompilerServices;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;

public class ApiStubContractTests
{
    private const string WorkPackage = "XP-19";

    private static XPathExpression Instance() =>
        (XPathExpression)RuntimeHelpers.GetUninitializedObject(typeof(XPathExpression));

    private static async Task AssertStub(Func<object?> call)
    {
        var failure = Assert.Throws<NotImplementedException>(() => call());
        await Assert.That(failure.Message).Contains(WorkPackage);
    }

    [Test]
    public async Task Compile_Members_ThrowNotImplemented()
    {
        await AssertStub(() => XPathExpression.TryCompile("1".AsSpan()));
        await AssertStub(() => XPathExpression.TryCompile("1"));
        await AssertStub(() => XPathExpression.TryCompile("1", XPathStaticContext.Default));
        await AssertStub(() => XPathExpression.Compile("1"));
    }

    [Test]
    public async Task Expression_Members_ThrowNotImplemented()
    {
        var expression = Instance();
        using var context = new XPathEvaluationContext();

        await AssertStub(() => expression.Source);
        await AssertStub(() => expression.Version);
        await AssertStub(() => expression.TryEvaluate(context));
        await AssertStub(() => expression.Evaluate(context));
    }

    [Test]
    public async Task Expression_HasNoPublicConstructor() =>
        await Assert.That(typeof(XPathExpression).GetConstructors()).IsEmpty();
}
