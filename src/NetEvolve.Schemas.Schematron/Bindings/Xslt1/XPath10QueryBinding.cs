namespace NetEvolve.Schemas.Schematron.Bindings.Xslt1;

using System;
using System.Collections.Generic;
using System.Threading;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Bindings.Registry;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;

/// <summary>
/// The binding of the names <c>xslt</c>, <c>xslt1</c>, and <c>xpath</c>, and of a missing <c>queryBinding</c>: the XPath 2.0 contract
/// with XPath 1.0 compatibility mode on (XPath 2.0, Appendix I; design #137 AP-25, design #150). The static context of every
/// expression of such a schema comes from <see cref="CreateStaticContext"/>.
/// </summary>
/// <remarks>
/// Compilation and evaluation are those of <see cref="XPathQueryBinding"/> with the mode on (SCH-55, #311). Only the mode is wired; the
/// incompatibilities of Appendix I and the missing namespace axis (XPST0010) remain, see the README of this folder.
/// </remarks>
internal sealed class XPath10QueryBinding : IFunctionHostBinding
{
    private readonly XPathQueryBinding _inner;

    /// <summary>Initializes a new instance of the <see cref="XPath10QueryBinding"/> class.</summary>
    /// <param name="name">The canonical lower-case name.</param>
    /// <param name="allowsXsltInstructions">Whether XSLT instructions are allowed; <see langword="false"/> for <c>xpath</c>.</param>
    /// <param name="extension">Adds the host functions to every static context, or <see langword="null"/> for none.</param>
    /// <param name="functions">The host functions of one schema (the stylesheet functions), or <see langword="null"/> for none.</param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    internal XPath10QueryBinding(
        string name,
        bool allowsXsltInstructions,
        XPathQueryBinding.StaticContextExtension? extension = null,
        IReadOnlyList<XPathHostFunction>? functions = null
    )
    {
        ArgumentNullException.ThrowIfNull(name);

        _inner = new XPathQueryBinding(name, allowsXsltInstructions, xpath10Compatibility: true, extension, functions);
    }

    private XPath10QueryBinding(XPathQueryBinding inner) => _inner = inner;

    public string Name => _inner.Name;

    public bool AllowsXsltInstructions => _inner.AllowsXsltInstructions;

    /// <inheritdoc />
    public IQueryBinding WithFunctions(IReadOnlyList<XPathHostFunction> functions) =>
        new XPath10QueryBinding((XPathQueryBinding)_inner.WithFunctions(functions));

    /// <summary>
    /// Creates the static context of the expressions in <paramref name="scope"/>: XPath 1.0 compatibility mode on, the prefixed
    /// namespace declarations, the visible variables, and the base URI when it is absolute.
    /// </summary>
    /// <param name="scope">The compile-time scope.</param>
    /// <returns>The static context.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="scope"/> is <see langword="null"/>.</exception>
    internal static XPathStaticContext CreateStaticContext(QueryStaticScope scope) =>
        XPathQueryBinding.CreateStaticContext(scope, xpath10Compatibility: true);

    public bool TryCompile(
        ReadOnlySpan<char> text,
        QueryExpressionKind kind,
        QueryStaticScope scope,
        out QueryExpression expression,
        out SchematronProblem problem
    ) => _inner.TryCompile(text, kind, scope, out expression, out problem);

    public IQueryRun BeginRun(QueryResources resources, CancellationToken cancellationToken) =>
        _inner.BeginRun(resources, cancellationToken);
}
