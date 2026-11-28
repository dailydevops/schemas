namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Maps;

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
/// fixture parses and binds with <see cref="XPathVersion.V31"/> and declares the <c>map</c> prefix.
/// </summary>
internal static class MapFixture
{
    public const string FnNamespace = "http://www.w3.org/2005/xpath-functions";
    public const string MapNamespace = "http://www.w3.org/2005/xpath-functions/map";

    public static CompiledExpression? TryCompile(
        string expression,
        out XPathDiagnostic diagnostic,
        XPathVersion version = XPathVersion.V31
    )
    {
        if (!XPathParser.TryParse(expression.AsSpan(), version, out var syntax, out diagnostic))
        {
            return null;
        }

        var statics = XPathStaticContext
            .Default.ToBuilder()
            .WithVersion(version)
            .WithNamespace("fn", FnNamespace)
            .WithNamespace("map", MapNamespace)
            .WithNamespace("array", "http://www.w3.org/2005/xpath-functions/array")
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

    public static XdmSequence Evaluate(string expression)
    {
        var compiled = TryCompile(expression, out var diagnostic)
            ?? throw new InvalidOperationException($"'{expression}' does not compile: {diagnostic.Code}");
        using var context = new XPathEvaluationContext();
        return compiled.Evaluate(context);
    }

    /// <summary>Evaluates an expression and describes the value: <c>()</c> for the empty sequence, otherwise the items.</summary>
    public static string Run(string expression) => ArithmeticFixture.Describe(Evaluate(expression));

    /// <summary>Compiles and evaluates an expression; the local name of the error code that either step raises, empty when none.</summary>
    public static string Code(string expression, XPathVersion version = XPathVersion.V31)
    {
        var compiled = TryCompile(expression, out var diagnostic, version);
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
