namespace NetEvolve.Schemas.Schematron.Bindings.Xslt1;

using System;
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
/// Limitations: compilation and evaluation stay stubbed like those of <see cref="XPathQueryBinding"/> until the public API of the XPath
/// package lands (XP-19, #297), so this binding hands out the static context and nothing evaluates under it in production yet. Only
/// the mode is wired; the incompatibilities of Appendix I and the missing namespace axis (XPST0010) remain, see the README of this folder.
/// </remarks>
internal sealed class XPath10QueryBinding : IQueryBinding
{
    private readonly XPathQueryBinding _inner;

    /// <summary>Initializes a new instance of the <see cref="XPath10QueryBinding"/> class.</summary>
    /// <param name="name">The canonical lower-case name.</param>
    /// <param name="allowsXsltInstructions">Whether XSLT instructions are allowed; <see langword="false"/> for <c>xpath</c>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    internal XPath10QueryBinding(string name, bool allowsXsltInstructions)
    {
        Argument.ThrowIfNull(name);

        _inner = new XPathQueryBinding(name, allowsXsltInstructions);
    }

    public string Name => _inner.Name;

    public bool AllowsXsltInstructions => _inner.AllowsXsltInstructions;

    /// <summary>
    /// Creates the static context of the expressions in <paramref name="scope"/>: XPath 1.0 compatibility mode on, the prefixed
    /// namespace declarations, the visible variables, and the base URI when it is absolute.
    /// </summary>
    /// <param name="scope">The compile-time scope.</param>
    /// <returns>The static context.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="scope"/> is <see langword="null"/>.</exception>
    internal static XPathStaticContext CreateStaticContext(QueryStaticScope scope)
    {
        Argument.ThrowIfNull(scope);

        var builder = XPathStaticContext.Default.ToBuilder().WithXPath10Compatibility(true);
        for (var i = 0; i < scope.Namespaces.Count; i++)
        {
            var declaration = scope.Namespaces[i];
            if (declaration.Prefix.Length > 0)
            {
                _ = builder.WithNamespace(declaration.Prefix, declaration.NamespaceName);
            }
        }

        // An empty entry is a slot that is not visible at this position.
        for (var i = 0; i < scope.VariableNames.Count; i++)
        {
            if (scope.VariableNames[i].Length > 0)
            {
                _ = builder.WithVariable(new XPathQName(string.Empty, scope.VariableNames[i]));
            }
        }

        if (scope.BaseUri is not null && Uri.TryCreate(scope.BaseUri, UriKind.Absolute, out var baseUri))
        {
            _ = builder.WithBaseUri(baseUri);
        }

        return builder.Build();
    }

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
