namespace NetEvolve.Schemas.XPath.Tests.Unit.Casting.Other;

using System;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Syntax.Parser;
using NetEvolve.Schemas.XPath.Tests.Unit.Casting.Core;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>Compiles and runs an expression against a static context of the test: the QName casts depend on the namespaces and the version of the context.</summary>
internal static class QNameFixture
{
    public const string P = "urn:p";

    /// <summary>The static context of the casting tests, XPath 2.0, with the prefix <c>p</c> bound to <see cref="P"/> and <c>q</c> bound to the same namespace.</summary>
    public static readonly XPathStaticContext V20 = CastFixture.Statics.ToBuilder().WithNamespace("q", P).Build();

    public static readonly XPathStaticContext V30 = V20.ToBuilder().WithVersion(XPathVersion.V30).Build();

    public static readonly XPathStaticContext V31 = V20.ToBuilder().WithVersion(XPathVersion.V31).Build();

    /// <summary>Compiles an expression; <see langword="null"/> when a static error is found.</summary>
    public static CompiledExpression? Compile(
        string expression,
        XPathStaticContext statics,
        out XPathDiagnostic diagnostic
    )
    {
        if (!XPathParser.TryParse(expression.AsSpan(), out var syntax, out diagnostic))
        {
            throw new InvalidOperationException("The test expression does not parse: " + diagnostic.Code);
        }

        _ = ExpressionCompiler.TryCompile(
            syntax!,
            expression,
            statics,
            CastFixture.Registry(),
            out var compiled,
            out diagnostic
        );
        return compiled;
    }

    /// <summary>The error code that compiling or evaluating raises, or <see cref="string.Empty"/> when it succeeds.</summary>
    public static string Code(string expression, XPathStaticContext statics)
    {
        var compiled = Compile(expression, statics, out var diagnostic);
        if (compiled is null)
        {
            return diagnostic.Code;
        }

        try
        {
            using var context = new XPathEvaluationContext();
            _ = compiled.Evaluate(context);
            return string.Empty;
        }
        catch (XPathException failure)
        {
            return failure.Diagnostic.Code;
        }
    }

    public static XdmSequence Evaluate(string expression, XPathStaticContext statics)
    {
        var compiled = Compile(expression, statics, out var diagnostic)
            ?? throw new InvalidOperationException("The test expression does not compile: " + diagnostic.Code);

        using var context = new XPathEvaluationContext();
        return compiled.Evaluate(context);
    }

    /// <summary>The single item of the result.</summary>
    public static XdmItem Item(string expression, XPathStaticContext statics)
    {
        var result = Evaluate(expression, statics);
        return result.Count == 1 ? result[0] : throw new InvalidOperationException("count:" + result.Count);
    }

    /// <summary>The string value of the single item that the expression casts to <c>xs:string</c>, in the given context.</summary>
    public static string Text(string expression, XPathStaticContext statics)
    {
        _ = Item("(" + expression + ") cast as xs:string", statics).TryGetString(out var text);
        return text ?? string.Empty;
    }

    /// <summary>Whether the <c>castable as</c> of the expression is true.</summary>
    public static bool Castable(string expression, string type, XPathStaticContext statics)
    {
        _ = Item("(" + expression + ") castable as xs:" + type, statics).TryGetBoolean(out var value);
        return value;
    }
}
