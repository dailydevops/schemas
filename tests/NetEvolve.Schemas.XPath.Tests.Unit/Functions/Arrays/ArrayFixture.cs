namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Arrays;

using System;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Syntax.Parser;
using NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// Compiles and runs XPath 3.1 expressions through the production registry. The prefixes <c>array</c>, <c>fn</c>, and <c>xs</c> are
/// bound, so the tests write the functions of the <c>array</c> namespace the way the specification does.
/// </summary>
internal static class ArrayFixture
{
    public const string ArrayNamespace = "http://www.w3.org/2005/xpath-functions/array";

    public static CompiledExpression? TryCompile(
        string expression,
        XPathVersion version,
        out XPathDiagnostic diagnostic
    )
    {
        if (!XPathParser.TryParse(expression.AsSpan(), version, out var syntax, out diagnostic))
        {
            throw new InvalidOperationException("The test expression does not parse: " + diagnostic.Code);
        }

        var statics = XPathStaticContext
            .Default.ToBuilder()
            .WithVersion(version)
            .WithNamespace("fn", "http://www.w3.org/2005/xpath-functions")
            .WithNamespace("xs", "http://www.w3.org/2001/XMLSchema")
            .WithNamespace("array", ArrayNamespace)
            .Build();
        _ = ExpressionCompiler.TryCompile(
            syntax!,
            expression,
            statics,
            XPathEngine.CreateRegistry(XPathEngine.AllSlots),
            out var compiled,
            out diagnostic
        );
        return compiled;
    }

    public static XdmSequence Evaluate(string expression)
    {
        var compiled = TryCompile(expression, XPathVersion.V31, out var diagnostic)
            ?? throw new InvalidOperationException($"'{expression}' does not compile: {diagnostic.Code}");
        using var context = new XPathEvaluationContext();
        return compiled.Evaluate(context);
    }

    /// <summary>Evaluates an expression and describes the value: <c>()</c> for the empty sequence, otherwise the items.</summary>
    public static string Run(string expression) => ArithmeticFixture.Describe(Evaluate(expression));

    /// <summary>Compiles and evaluates an expression; the local name of the error code that either step raises, empty when none.</summary>
    public static string Code(string expression, XPathVersion version = XPathVersion.V31)
    {
        var compiled = TryCompile(expression, version, out var diagnostic);
        if (compiled is null)
        {
            return diagnostic.QName.LocalName;
        }

        using var context = new XPathEvaluationContext();
        try
        {
            _ = compiled.Evaluate(context);
            return string.Empty;
        }
        catch (XPathException exception)
        {
            return exception.Diagnostic.QName.LocalName;
        }
    }
}
