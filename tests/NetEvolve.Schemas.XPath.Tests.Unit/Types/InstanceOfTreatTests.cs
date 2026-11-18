namespace NetEvolve.Schemas.XPath.Tests.Unit.Types;

using System;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Types;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static TypeFixture;

public class InstanceOfTreatTests
{
    private static async Task AssertBoolean(string expression, bool expected)
    {
        var result = Evaluate(expression);

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0].TryGetBoolean(out var value) && value == expected).IsTrue();
    }

    [Test]
    [Arguments("5 instance of xs:integer", true)]
    [Arguments("5 instance of xs:decimal", true)]
    [Arguments("5 instance of xs:string", false)]
    [Arguments("'a' instance of xs:string", true)]
    [Arguments("'a' instance of xs:anyAtomicType", true)]
    [Arguments("'a' instance of item()", true)]
    [Arguments("'a' instance of node()", false)]
    [Arguments("(1, 2) instance of xs:integer+", true)]
    [Arguments("(1, 2) instance of xs:integer", false)]
    [Arguments("(1, 2) instance of xs:integer?", false)]
    [Arguments("(1, 'a') instance of xs:integer*", false)]
    [Arguments("() instance of empty-sequence()", true)]
    [Arguments("() instance of xs:integer?", true)]
    [Arguments("() instance of xs:integer", false)]
    [Arguments("5 instance of empty-sequence()", false)]
    public async Task InstanceOf_EvaluatesTheMatch(string expression, bool expected) =>
        await AssertBoolean(expression, expected);

    [Test]
    public async Task InstanceOf_ResultIsASingleBoolean()
    {
        var result = Evaluate("5 instance of xs:integer");

        await Assert.That(result[0].Kind).IsEqualTo(XdmItemKind.Boolean);
    }

    [Test]
    public async Task Treat_MatchingOperand_ReturnsItUnchanged()
    {
        var single = Evaluate("5 treat as xs:integer");
        var many = Evaluate("(1, 2, 3) treat as xs:integer+");
        var none = Evaluate("() treat as xs:integer*");

        await Assert.That(single).IsEqualTo(Seq(XdmItem.FromInteger(5)));
        await Assert.That(many).IsEqualTo(Seq(XdmItem.FromInteger(1), XdmItem.FromInteger(2), XdmItem.FromInteger(3)));
        await Assert.That(none.IsEmpty).IsTrue();
    }

    [Test]
    [Arguments("'a' treat as xs:integer")]
    [Arguments("(1, 2) treat as xs:integer")]
    [Arguments("() treat as xs:integer")]
    [Arguments("(1, 'a') treat as xs:integer*")]
    [Arguments("5 treat as empty-sequence()")]
    [Arguments("5 treat as node()")]
    public async Task Treat_NonMatchingOperand_RaisesXpdy0050(string expression)
    {
        var compiled = Compile(expression, out _);
        using var context = new XPathEvaluationContext();

        XPathException? error = null;
        try
        {
            _ = compiled!.Evaluate(context);
        }
        catch (XPathException exception)
        {
            error = exception;
        }

        await Assert.That(error).IsNotNull();
        await Assert.That(error!.Diagnostic.QName).IsEqualTo(XPathErrorCodes.XPDY0050);
        await Assert.That(error.Diagnostic.Kind).IsEqualTo(XPathErrorKind.Dynamic);
    }

    [Test]
    [Arguments("5 instance of xs:nosuchtype", 14)]
    [Arguments("5 treat as xs:nosuchtype", 11)]
    [Arguments("5 treat as xs:anyType", 11)]
    [Arguments("5 instance of foo", 14)]
    public async Task UnknownTypeName_FailsToCompileWithXpst0051AtTheTypeName(string expression, int offset)
    {
        var compiled = Compile(expression, out var diagnostic);

        await Assert.That(compiled).IsNull();
        await Assert.That(diagnostic.QName).IsEqualTo(XPathErrorCodes.XPST0051);
        await Assert.That(diagnostic.Offset).IsEqualTo(offset);
    }

    [Test]
    public async Task UnboundPrefix_FailsToCompileWithXpst0081()
    {
        var compiled = Compile("5 instance of q:integer", out var diagnostic);

        await Assert.That(compiled).IsNull();
        await Assert.That(diagnostic.QName).IsEqualTo(XPathErrorCodes.XPST0081);
    }

    [Test]
    public async Task SchemaKindTest_FailsToCompileWithXpst0008()
    {
        var compiled = Compile("5 instance of schema-element(a)", out var diagnostic);

        await Assert.That(compiled).IsNull();
        await Assert.That(diagnostic.QName).IsEqualTo(XPathErrorCodes.XPST0008);
    }

    [Test]
    public async Task CompiledExpression_IsReusableAcrossEvaluations()
    {
        var compiled = Compile("(1, 2) instance of xs:integer+", out _)!;

        using var first = new XPathEvaluationContext();
        using var second = new XPathEvaluationContext();

        await Assert.That(compiled.Evaluate(first)).IsEqualTo(compiled.Evaluate(second));
    }

    [Test]
    public async Task Register_AddsTheLoweringsOfBothNodeKindsAndCannotRunTwice()
    {
        var registry = new LoweringRegistry();
        SequenceTypeLowerings.Register(registry);

        await Assert.That(registry.TryGet(typeof(InstanceOfExpression), out var instance)).IsTrue();
        await Assert.That(registry.TryGet(typeof(TreatExpression), out var treat)).IsTrue();
        await Assert.That(instance).IsNotNull();
        await Assert.That(treat).IsNotNull();
        await Assert.That(() => SequenceTypeLowerings.Register(registry)).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Register_NullRegistry_Throws() =>
        await Assert.That(() => SequenceTypeLowerings.Register(null!)).Throws<ArgumentNullException>();
}
