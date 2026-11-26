namespace NetEvolve.Schemas.XPath.Api;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>A compiled expression: immutable, and safe to evaluate from many threads at once, each with a context of its own.</summary>
/// <remarks>
/// <para>
/// The compiler binds a function call over the host functions of the static context first, the later layer winning, and over the
/// built-in functions after them (design #123). A host function of the same name and arity as a built-in function therefore overrides it,
/// also in the namespace of the built-in functions; the static context does not reject such a registration.
/// </para>
/// <para>
/// Limitations: only XPath 2.0 is enabled, so a static context of another version does not compile (<c>XP0001</c>). The prefixes
/// <c>fn</c> and <c>xs</c> are not predeclared by <see cref="XPathStaticContext.Default"/> (#876); declare them with
/// <see cref="XPathStaticContext.Builder.WithNamespace"/>. Every host slot counts as provided at compile time, so a function that needs a
/// slot is visible and raises XPDY0002 when the slot of the evaluation context is unset; a function that needs the XSLT host flag is not
/// visible through this class. A dynamic error carries the location of the expression that raised it only where the operator knows it
/// (a cast, a treat, an evaluation limit); the offset is -1 otherwise.
/// </para>
/// </remarks>
public sealed class XPathExpression
{
    private readonly CompiledExpression _compiled;

    internal XPathExpression(CompiledExpression compiled)
    {
        ArgumentNullException.ThrowIfNull(compiled);

        _compiled = compiled;
    }

    /// <summary>Gets the compiled expression; tests read the executable tree through it.</summary>
    internal CompiledExpression Compiled => _compiled;

    /// <summary>Gets the source text.</summary>
    public string Source => _compiled.Source;

    /// <summary>Gets the language version.</summary>
    public XPathVersion Version => _compiled.Version;

    /// <summary>Compiles an expression; never throws for syntax errors.</summary>
    /// <param name="expression">The expression text; the compiled expression keeps a copy.</param>
    /// <param name="context">The static context, <see langword="null"/> for <see cref="XPathStaticContext.Default"/>.</param>
    /// <returns>The result.</returns>
    public static XPathCompileResult TryCompile(ReadOnlySpan<char> expression, XPathStaticContext? context = null) =>
        XPathEngine.TryCompile(expression, expression.ToString(), context, XPathEngine.AllSlots);

    /// <summary>Compiles an expression under the host slots and flags of a host; the flags make the functions that need them visible.</summary>
    /// <param name="expression">The expression text; the compiled expression keeps a copy.</param>
    /// <param name="context">The static context.</param>
    /// <param name="activeHost">The host slots and flags the host provides.</param>
    /// <returns>The result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    internal static XPathCompileResult TryCompile(
        ReadOnlySpan<char> expression,
        XPathStaticContext context,
        XPathHostRequirements activeHost
    )
    {
        ArgumentNullException.ThrowIfNull(context);

        return XPathEngine.TryCompile(expression, expression.ToString(), context, activeHost);
    }

    /// <summary>Compiles an expression; never throws for syntax errors.</summary>
    /// <param name="expression">The expression text.</param>
    /// <param name="context">The static context, <see langword="null"/> for <see cref="XPathStaticContext.Default"/>.</param>
    /// <returns>The result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="expression"/> is <see langword="null"/>.</exception>
    public static XPathCompileResult TryCompile(string expression, XPathStaticContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(expression);

        return XPathEngine.TryCompile(expression.AsSpan(), expression, context, XPathEngine.AllSlots);
    }

    /// <summary>Compiles an expression.</summary>
    /// <param name="expression">The expression text.</param>
    /// <param name="context">The static context, <see langword="null"/> for <see cref="XPathStaticContext.Default"/>.</param>
    /// <returns>The compiled expression.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="expression"/> is <see langword="null"/>.</exception>
    /// <exception cref="XPathException">The expression does not compile.</exception>
    public static XPathExpression Compile(string expression, XPathStaticContext? context = null)
    {
        var result = TryCompile(expression, context);
        return result.Success ? result.Expression! : throw new XPathException(result.Diagnostic);
    }

    /// <summary>Evaluates the expression; never throws for dynamic errors.</summary>
    /// <param name="context">The evaluation context.</param>
    /// <returns>The result, valid until the next evaluation on, or disposal of, the context.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">The context is disposed.</exception>
    public XPathResult TryEvaluate(XPathEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.ThrowIfDisposed();

        try
        {
            return new XPathResult(XPathEvaluationStatus.Ok, _compiled.Evaluate(context), default);
        }
        catch (XPathException failure)
        {
            return new XPathResult(XPathEvaluationStatus.Error, XdmSequence.Empty, failure.Diagnostic);
        }
    }

    /// <summary>Evaluates the expression.</summary>
    /// <param name="context">The evaluation context.</param>
    /// <returns>The value, valid until the next evaluation on, or disposal of, the context.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">The context is disposed.</exception>
    /// <exception cref="XPathException">The evaluation fails.</exception>
    public XdmSequence Evaluate(XPathEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.ThrowIfDisposed();

        return _compiled.Evaluate(context);
    }

    /// <summary>Evaluates the expression to its effective boolean value; never throws for dynamic errors.</summary>
    /// <param name="context">The evaluation context.</param>
    /// <param name="value">The effective boolean value, <see langword="false"/> when the evaluation fails.</param>
    /// <param name="diagnostic">The diagnostic, <see langword="default"/> on success.</param>
    /// <returns>The status.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">The context is disposed.</exception>
    public XPathEvaluationStatus TryEvaluateBoolean(
        XPathEvaluationContext context,
        out bool value,
        out XPathDiagnostic diagnostic
    )
    {
        ArgumentNullException.ThrowIfNull(context);
        context.ThrowIfDisposed();

        try
        {
            value = _compiled.EvaluateBoolean(context);
            diagnostic = default;
            return XPathEvaluationStatus.Ok;
        }
        catch (XPathException failure)
        {
            value = false;
            diagnostic = failure.Diagnostic;
            return XPathEvaluationStatus.Error;
        }
    }

    /// <summary>
    /// Evaluates the expression to its effective boolean value: the result of <see cref="Evaluate"/> reduced as <c>fn:boolean</c> does, without
    /// building the result sequence where the value is known earlier, for example at the first node of a path.
    /// </summary>
    /// <param name="context">The evaluation context.</param>
    /// <returns>The effective boolean value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">The context is disposed.</exception>
    /// <exception cref="XPathException">The evaluation fails, or the value has no effective boolean value (FORG0006).</exception>
    public bool EvaluateBoolean(XPathEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.ThrowIfDisposed();

        return _compiled.EvaluateBoolean(context);
    }
}
