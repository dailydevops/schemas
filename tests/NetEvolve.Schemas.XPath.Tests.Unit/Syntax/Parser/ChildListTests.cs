namespace NetEvolve.Schemas.XPath.Tests.Unit.Syntax.Parser;

using System;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Syntax.Parser;
using NetEvolve.Schemas.XPath.Syntax.Tree;

public class ChildListTests
{
    [Test]
    [Arguments("a", null, "a", null)]
    [Arguments("p:a", "p", "a", null)]
    [Arguments("Q{urn:x}a", null, "a", "urn:x")]
    public async Task Parse_NameTest_SplitsPrefixLocalNameAndNamespace(
        string text,
        string? prefix,
        string local,
        string? namespaceName
    )
    {
        var step = (AxisStep)Parse(text, XPathVersion.V30);
        var test = (NameTest)step.Test;

        await Assert.That(test.Kind).IsEqualTo(NameTestKind.QualifiedName);
        await Assert.That(test.Prefix).IsEqualTo(prefix);
        await Assert.That(test.LocalName).IsEqualTo(local);
        await Assert.That(test.NamespaceUri).IsEqualTo(namespaceName);
        await Assert.That(test.Location.Length).IsEqualTo(step.Location.Length);
    }

    [Test]
    public async Task Parse_StepWithoutPredicates_HasEmptyPredicates()
    {
        var step = (AxisStep)Parse("a");

        await Assert.That(step.Predicates.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Parse_CallsAndPredicates_KeepTheirChildLists()
    {
        var call = (FunctionCall)Parse("f()");
        var dynamic = (DynamicFunctionCall)Parse("f()()", XPathVersion.V30);
        var path = (PathExpression)Parse("a[1][2]/b/c");
        var step = (AxisStep)path.Steps[0];

        await Assert.That(call.Arguments.Count).IsEqualTo(0);
        await Assert.That(dynamic.Arguments.Count).IsEqualTo(0);
        await Assert.That(path.Steps.Count).IsEqualTo(3);
        await Assert.That(step.Predicates.Count).IsEqualTo(2);
        await Assert.That(((FunctionCall)Parse("f(1, 2)")).Arguments.Count).IsEqualTo(2);
    }

    private static Expression Parse(string text, XPathVersion version = XPathVersion.V20) =>
        XPathParser.TryParse(text.AsSpan(), version, out var expression, out _)
            ? expression!
            : throw new InvalidOperationException(text);
}
