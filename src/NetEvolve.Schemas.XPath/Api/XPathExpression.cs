#pragma warning disable CA1065, MA0025 // Stub per #168: throwing bodies stay until XP-19 replaces them; the FND-50 stub convention of #117 and #103.

namespace NetEvolve.Schemas.XPath.Api;

using System;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>A compiled expression: immutable and thread-safe once implemented.</summary>
/// <remarks>Limitation: every member throws until XP-19 lands; the package is not usable by consumers until then.</remarks>
public sealed class XPathExpression
{
    private XPathExpression() { }

    /// <summary>Gets the source text.</summary>
    public string Source => throw Stub.For("XP-19");

    /// <summary>Gets the language version.</summary>
    public XPathVersion Version => throw Stub.For("XP-19");

    /// <summary>Compiles an expression; never throws for syntax errors.</summary>
    /// <param name="expression">The expression text.</param>
    /// <param name="context">The static context, <see langword="null"/> for <see cref="XPathStaticContext.Default"/>.</param>
    /// <returns>The result.</returns>
    public static XPathCompileResult TryCompile(ReadOnlySpan<char> expression, XPathStaticContext? context = null) =>
        throw Stub.For("XP-19");

    /// <summary>Compiles an expression under the host flags of a host; the flags make the functions that need them visible.</summary>
    /// <param name="expression">The expression text.</param>
    /// <param name="context">The static context.</param>
    /// <param name="activeHost">The host slots and flags the host provides.</param>
    /// <returns>The result.</returns>
    internal static XPathCompileResult TryCompile(
        ReadOnlySpan<char> expression,
        XPathStaticContext context,
        XPathHostRequirements activeHost
    ) => throw Stub.For("XP-19");

    /// <summary>Compiles an expression; never throws for syntax errors.</summary>
    /// <param name="expression">The expression text.</param>
    /// <param name="context">The static context, <see langword="null"/> for <see cref="XPathStaticContext.Default"/>.</param>
    /// <returns>The result.</returns>
    public static XPathCompileResult TryCompile(string expression, XPathStaticContext? context = null) =>
        throw Stub.For("XP-19");

    /// <summary>Compiles an expression.</summary>
    /// <param name="expression">The expression text.</param>
    /// <param name="context">The static context, <see langword="null"/> for <see cref="XPathStaticContext.Default"/>.</param>
    /// <returns>The compiled expression.</returns>
    /// <exception cref="Errors.XPathException">The expression does not compile.</exception>
    public static XPathExpression Compile(string expression, XPathStaticContext? context = null) =>
        throw Stub.For("XP-19");

    /// <summary>Evaluates the expression; never throws for dynamic errors.</summary>
    /// <param name="context">The evaluation context.</param>
    /// <returns>The result, valid until the next evaluation on, or disposal of, the context.</returns>
    public XPathResult TryEvaluate(XPathEvaluationContext context) => throw Stub.For("XP-19");

    /// <summary>Evaluates the expression.</summary>
    /// <param name="context">The evaluation context.</param>
    /// <returns>The value, valid until the next evaluation on, or disposal of, the context.</returns>
    /// <exception cref="Errors.XPathException">The evaluation fails.</exception>
    public XdmSequence Evaluate(XPathEvaluationContext context) => throw Stub.For("XP-19");
}
