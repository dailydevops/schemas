namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.FunctionItems;

using System;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Syntax.Parser;
using NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// Compiles and runs XPath 3.0 expressions through the production registry: the compiler of the public API enables XPath 2.0 only, so the
/// fixture parses with <see cref="XPathVersion.V30"/> and lowers with the registry the engine uses. The host function <c>tick()</c> counts
/// its calls on a <see cref="Probe"/> and returns the count.
/// </summary>
internal static class FunctionItemFixture
{
    public const string FnNamespace = "http://www.w3.org/2005/xpath-functions";

    /// <summary>The counter the host function <c>tick()</c> advances.</summary>
    public sealed class Probe
    {
        public int Ticks { get; set; }
    }

    public static CompiledExpression? TryCompile(string expression, Probe probe, out XPathDiagnostic diagnostic)
    {
        if (!XPathParser.TryParse(expression.AsSpan(), XPathVersion.V30, out var syntax, out diagnostic))
        {
            throw new InvalidOperationException("The test expression does not parse: " + diagnostic.Code);
        }

        var statics = XPathStaticContext
            .Default.ToBuilder()
            .WithVersion(XPathVersion.V30)
            .WithNamespace("fn", FnNamespace)
            .WithNamespace("xs", "http://www.w3.org/2001/XMLSchema")
            .WithFunction(new TickFunction(new XPathQName(FnNamespace, "tick"), probe))
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

    public static XdmSequence Evaluate(string expression, Probe? probe = null, bool withContextItem = false)
    {
        var compiled = TryCompile(expression, probe ?? new Probe(), out var diagnostic)
            ?? throw new InvalidOperationException($"'{expression}' does not compile: {diagnostic.Code}");
        using var context = new XPathEvaluationContext();
        if (withContextItem)
        {
            context.SetContextItem(XdmItem.FromInteger(5));
        }

        return compiled.Evaluate(context);
    }

    /// <summary>Evaluates an expression and describes the value: <c>()</c> for the empty sequence, otherwise the items.</summary>
    public static string Run(string expression, Probe? probe = null, bool withContextItem = false) =>
        ArithmeticFixture.Describe(Evaluate(expression, probe, withContextItem));

    /// <summary>Compiles and evaluates an expression; the local name of the error code that either step raises, empty when none.</summary>
    public static string Code(string expression, bool withContextItem = false)
    {
        var compiled = TryCompile(expression, new Probe(), out var diagnostic);
        if (compiled is null)
        {
            return diagnostic.QName.LocalName;
        }

        using var context = new XPathEvaluationContext();
        if (withContextItem)
        {
            context.SetContextItem(XdmItem.FromInteger(5));
        }

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

    private sealed class TickFunction(XPathQName name, Probe probe) : XPathHostFunction(name, 0, 0)
    {
        public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
        {
            probe.Ticks++;
            invocation.Result = XdmSequence.Create(XdmItem.FromInteger(probe.Ticks));
            return XPathEvaluationStatus.Ok;
        }
    }
}
