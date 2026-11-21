namespace NetEvolve.Schemas.XPath.Tests.Unit.Api;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;

public class XPathExpressionCompileTests
{
    private static XPathCompileResult CompileFromSpan(string expression, XPathStaticContext? statics = null) =>
        XPathExpression.TryCompile(expression.AsSpan(), statics);

    [Test]
    public async Task Compile_StringAndSpan_GiveTheSameSourceAndVersion()
    {
        var fromString = XPathExpression.TryCompile("1 + 2");
        var fromSpan = CompileFromSpan("1 + 2");

        await Assert.That(fromString.Success).IsTrue();
        await Assert.That(fromSpan.Success).IsTrue();
        await Assert.That(fromSpan.Expression!.Source).IsEqualTo(fromString.Expression!.Source);
        await Assert.That(fromSpan.Expression.Version).IsEqualTo(XPathVersion.V20);
        await Assert.That(fromString.Diagnostic == default).IsTrue();
    }

    [Test]
    public async Task Compile_Span_CopiesTheSourceOnce()
    {
        var buffer = "1 + 2".ToCharArray();
        var expression = XPathExpression.TryCompile(buffer).Expression!;

        Array.Clear(buffer, 0, buffer.Length);

        await Assert.That(expression.Source).IsEqualTo("1 + 2");
    }

    [Test]
    public async Task Compile_ConvenienceMember_ReturnsTheExpression()
    {
        var expression = XPathExpression.Compile("1 + 2");

        await Assert.That(expression.Source).IsEqualTo("1 + 2");
    }

    [Test]
    public async Task TryCompile_SyntaxError_ReturnsXpst0003WithLocationAndNeverThrows()
    {
        var fromString = XPathExpression.TryCompile("1 + )");
        var fromSpan = CompileFromSpan("1 + )");

        await Assert.That(fromString.Success).IsFalse();
        await Assert.That(fromString.Expression).IsNull();
        await Assert.That(fromString.Diagnostic.Code).IsEqualTo("XPST0003");
        await Assert.That(fromString.Diagnostic.Kind).IsEqualTo(XPathErrorKind.Static);
        await Assert.That(fromString.Diagnostic.Offset).IsEqualTo(4);
        await Assert.That(fromString.Diagnostic.Length).IsEqualTo(1);
        await Assert.That(fromSpan.Diagnostic == fromString.Diagnostic).IsTrue();
    }

    [Test]
    public async Task TryCompile_UndefinedVariable_ReturnsXpst0008WithLocation()
    {
        var result = XPathExpression.TryCompile("1 + $x", ApiFixture.Statics());

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Diagnostic.Code).IsEqualTo("XPST0008");
        await Assert.That(result.Diagnostic.Offset).IsEqualTo(5);
        await Assert.That(result.Diagnostic.Length).IsEqualTo(1);
    }

    [Test]
    public async Task TryCompile_DeclaredVariable_Compiles()
    {
        var statics = ApiFixture.Builder().WithVariable(new XPathQName(string.Empty, "x")).Build();

        await Assert.That(XPathExpression.TryCompile("1 + $x", statics).Success).IsTrue();
    }

    [Test]
    public async Task TryCompile_UnknownFunction_ReturnsXpst0017WithTheLocationOfTheCall()
    {
        var result = XPathExpression.TryCompile("1 + nosuch(2)");

        await Assert.That(result.Diagnostic.Code).IsEqualTo("XPST0017");
        await Assert.That(result.Diagnostic.Offset).IsEqualTo(4);
        await Assert.That(result.Diagnostic.Length).IsEqualTo(9);
    }

    [Test]
    public async Task TryCompile_KnownFunctionWithUnknownArity_ReturnsXpst0017() =>
        await Assert.That(XPathExpression.TryCompile("count()").Diagnostic.Code).IsEqualTo("XPST0017");

    [Test]
    public async Task TryCompile_UnboundFunctionPrefix_ReturnsXpst0081()
    {
        var result = XPathExpression.TryCompile("p:f()");

        await Assert.That(result.Diagnostic.Code).IsEqualTo("XPST0081");
        await Assert.That(result.Diagnostic.Offset).IsEqualTo(0);
    }

    [Test]
    public async Task TryCompile_PrefixedFunction_ResolvesThroughTheStaticContext()
    {
        await Assert.That(XPathExpression.TryCompile("fn:count(1)", ApiFixture.Statics()).Success).IsTrue();
        await Assert.That(XPathExpression.TryCompile("fn:count(1)").Diagnostic.Code).IsEqualTo("XPST0081");
    }

    [Test]
    [Arguments(XPathVersion.V30)]
    [Arguments(XPathVersion.V31)]
    public async Task TryCompile_VersionThatIsNotEnabled_ReturnsXp0001(XPathVersion version)
    {
        var statics = XPathStaticContext.Default.ToBuilder().WithVersion(version).Build();

        var result = XPathExpression.TryCompile("1", statics);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Diagnostic.Code).IsEqualTo("{urn:netevolve:schemas:xpath}XP0001");
        await Assert.That(result.Diagnostic.Kind).IsEqualTo(XPathErrorKind.Engine);
        await Assert.That(result.Diagnostic.Offset).IsEqualTo(-1);
    }

    [Test]
    public async Task TryCompile_VersionSelection_V20IsTheDefaultAndIsReported()
    {
        var statics = XPathStaticContext.Default.ToBuilder().WithVersion(XPathVersion.V20).Build();

        await Assert.That(XPathExpression.TryCompile("1", statics).Expression!.Version).IsEqualTo(XPathVersion.V20);
    }

    [Test]
    public async Task Compile_Failure_ThrowsXPathExceptionWithTheDiagnostic()
    {
        var notEnabled = XPathStaticContext.Default.ToBuilder().WithVersion(XPathVersion.V30).Build();

        var failure = Assert.Throws<XPathException>(() => XPathExpression.Compile("1 + )"));
        var version = Assert.Throws<XPathException>(() => XPathExpression.Compile("1", notEnabled));

        await Assert.That(failure.Diagnostic.Code).IsEqualTo("XPST0003");
        await Assert.That(failure.Diagnostic.Offset).IsEqualTo(4);
        await Assert.That(version.Diagnostic.Kind).IsEqualTo(XPathErrorKind.Engine);
    }

    [Test]
    public async Task Compile_NullExpression_IsAProgrammingError()
    {
        await Assert.That(() => XPathExpression.Compile(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => XPathExpression.TryCompile((string)null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Compile_CompatibilityMode_UsesTheCompatibilityCallNode()
    {
        var statics = ApiFixture.Builder().WithXPath10Compatibility(true).Build();

        await Assert.That(ApiFixture.Run("string-length(12)", null, statics)).IsEqualTo("2");
        await Assert.That(ApiFixture.Run("true()", null, statics)).IsEqualTo("true");
        await Assert.That(ApiFixture.Run("concat('a', 'b', 'c')", null, statics)).IsEqualTo("abc");
    }
}
