namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Arrow;

using System;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Syntax.Parser;
using NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// Compiles and runs XPath 3.1 expressions through the production registry: the compiler of the public API enables XPath 2.0 only, so the
/// fixture parses with the requested version and lowers with the registry the engine uses.
/// </summary>
internal static class ArrowFixture
{
    public static CompiledExpression? TryCompile(
        string expression,
        XPathVersion version,
        out XPathDiagnostic diagnostic
    )
    {
        if (!XPathParser.TryParse(expression.AsSpan(), version, out var syntax, out diagnostic))
        {
            return null;
        }

        var statics = XPathStaticContext
            .Default.ToBuilder()
            .WithVersion(version)
            .WithNamespace("fn", "http://www.w3.org/2005/xpath-functions")
            .WithNamespace("xs", "http://www.w3.org/2001/XMLSchema")
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

    public static CompiledExpression Compile(string expression) =>
        TryCompile(expression, XPathVersion.V31, out var diagnostic)
        ?? throw new InvalidOperationException($"'{expression}' does not compile: {diagnostic.QName.LocalName}");

    /// <summary>Evaluates an expression and describes the value: <c>()</c> for the empty sequence, otherwise the items.</summary>
    public static string Run(string expression)
    {
        using var context = new XPathEvaluationContext();
        return ArithmeticFixture.Describe(Compile(expression).Evaluate(context));
    }

    /// <summary>Compiles and evaluates an expression; the diagnostic of the error that either step raises, default when none.</summary>
    public static XPathDiagnostic Diagnose(string expression, XPathVersion version = XPathVersion.V31)
    {
        var compiled = TryCompile(expression, version, out var diagnostic);
        if (compiled is null)
        {
            return diagnostic;
        }

        using var context = new XPathEvaluationContext();
        try
        {
            _ = compiled.Evaluate(context);
            return default;
        }
        catch (XPathException exception)
        {
            return exception.Diagnostic;
        }
    }
}
