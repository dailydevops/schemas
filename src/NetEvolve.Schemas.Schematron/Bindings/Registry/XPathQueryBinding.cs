namespace NetEvolve.Schemas.Schematron.Bindings.Registry;

using System;
using System.Collections.Generic;
using System.Threading;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;

/// <summary>
/// The binding for the XPath 2.0 contract, shared by the names <c>xpath2</c>, <c>xslt2</c>, and <c>xslt3</c> (design #137 AP-25). The
/// names <c>xslt</c>, <c>xslt1</c>, and <c>xpath</c> use the same implementation with XPath 1.0 compatibility mode on (SCH-16, #310).
/// </summary>
/// <remarks>
/// <para>
/// Compilation goes through the public API of the XPath package (XP-19, #297). The static context holds the namespaces and the
/// variables of the scope; a variable is found by its slot, because <see cref="QueryStaticScope.VariableNames"/> may hold a name
/// twice and the last entry wins (<see cref="SlotReferences"/>). A static error is <see cref="SchematronDiagnosticCodes.VariableUndefined"/>
/// for an undeclared variable (XPST0008) and <see cref="SchematronDiagnosticCodes.ExpressionInvalid"/> otherwise; the XPath code is the
/// constraint.
/// </para>
/// <para>
/// Limitations: the seam does not hand the binding the location of the construct that holds the expression, so a problem is located by
/// the base URI of the scope and by the line and column within the expression text (the convention of the pattern matcher), and the
/// location of a dynamic problem has no line and column. The decimal formats of an XSLT
/// stylesheet are not wired (#882): a run starts with an empty set of decimal formats, so a named format is unknown. The stylesheet functions are host functions of a derived binding (<see cref="WithFunctions"/>). Without
/// an extension the binding has the functions of XPath 2.0 only.
/// </para>
/// </remarks>
internal sealed class XPathQueryBinding : IFunctionHostBinding
{
    // The public code type, not the internal table of the XPath package, which the seam rule keeps out of reach.
    private static readonly XPathErrorCode UndeclaredVariable = new("XPST0008");

    private readonly bool _xpath10Compatibility;
    private readonly StaticContextExtension? _extension;
    private readonly IReadOnlyList<XPathHostFunction>? _functions;

    /// <summary>Initializes a new instance of the <see cref="XPathQueryBinding"/> class.</summary>
    /// <param name="name">The canonical lower-case name.</param>
    /// <param name="allowsXsltInstructions">Whether XSLT instructions are allowed; <see langword="false"/> for the <c>xpath</c> family.</param>
    /// <param name="xpath10Compatibility">Whether XPath 1.0 compatibility mode is on.</param>
    /// <param name="extension">Adds the host functions to every static context, or <see langword="null"/> for none.</param>
    /// <param name="functions">The host functions of one schema (the stylesheet functions), or <see langword="null"/> for none.</param>
    internal XPathQueryBinding(
        string name,
        bool allowsXsltInstructions,
        bool xpath10Compatibility = false,
        StaticContextExtension? extension = null,
        IReadOnlyList<XPathHostFunction>? functions = null
    )
    {
        ArgumentNullException.ThrowIfNull(name);

        Name = name;
        AllowsXsltInstructions = allowsXsltInstructions;
        _xpath10Compatibility = xpath10Compatibility;
        _extension = extension;
        _functions = functions;
    }

    /// <summary>
    /// Adds the host functions of a composition to the static context of an expression. The layering rule keeps the XSLT library out of
    /// reach of a binding, so the composition (<c>Pipeline</c>) hands the functions in.
    /// </summary>
    /// <param name="binding">The binding that compiles.</param>
    /// <param name="statics">The static context of the scope.</param>
    /// <param name="activeHost">The slots and flags that the host provides.</param>
    /// <returns>The static context with the functions.</returns>
    internal delegate XPathStaticContext StaticContextExtension(
        IQueryBinding binding,
        XPathStaticContext statics,
        out XPathHostRequirements activeHost
    );

    public string Name { get; }

    public bool AllowsXsltInstructions { get; }

    /// <inheritdoc />
    public IQueryBinding WithFunctions(IReadOnlyList<XPathHostFunction> functions)
    {
        ArgumentNullException.ThrowIfNull(functions);

        return new XPathQueryBinding(Name, AllowsXsltInstructions, _xpath10Compatibility, _extension, functions);
    }

    /// <summary>
    /// Creates the static context of the expressions in <paramref name="scope"/>: the prefixed namespace declarations, the visible
    /// variables, the base URI when it is absolute, and the compatibility mode when asked for.
    /// </summary>
    /// <param name="scope">The compile-time scope.</param>
    /// <param name="xpath10Compatibility">Whether XPath 1.0 compatibility mode is on.</param>
    /// <returns>The static context.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="scope"/> is <see langword="null"/>.</exception>
    internal static XPathStaticContext CreateStaticContext(QueryStaticScope scope, bool xpath10Compatibility)
    {
        ArgumentNullException.ThrowIfNull(scope);

        var builder = XPathStaticContext.Default.ToBuilder().WithXPath10Compatibility(xpath10Compatibility);
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
    )
    {
        ArgumentNullException.ThrowIfNull(scope);

        var source = text.ToString();

        var statics = CreateStaticContext(scope, _xpath10Compatibility);
        var activeHost = XPathEngine.AllSlots;
        if (_extension is not null)
        {
            statics = _extension(this, statics, out activeHost);
        }

        if (_functions is { Count: > 0 })
        {
            statics = statics
                .ToBuilder()
                .WithLibrary(new XPathFunctionLibrary("stylesheet-functions", _functions))
                .Build();
        }

        var result = XPathExpression.TryCompile(text, statics, activeHost);
        if (!result.Success)
        {
            expression = null!;
            problem = StaticProblem(source, scope, result.Diagnostic);
            return false;
        }

        SlotReferences.Collect(text, scope, out var slots, out var names);
        expression = new XPathQueryExpression(
            result.Expression!,
            new DocumentLocation(scope.BaseUri, 0, 0),
            slots,
            names,
            scope.DeclaredType
        );
        problem = default;
        return true;
    }

    public IQueryRun BeginRun(QueryResources resources, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(resources);

        return new XPathQueryRun(resources, cancellationToken);
    }

    private static SchematronProblem StaticProblem(string source, QueryStaticScope scope, in XPathDiagnostic diagnostic)
    {
        var (line, column) = LineAndColumn(source, diagnostic.Offset);
        return new SchematronProblem(
            diagnostic.QName == UndeclaredVariable
                ? SchematronDiagnosticCodes.VariableUndefined
                : SchematronDiagnosticCodes.ExpressionInvalid,
            diagnostic.Message,
            new DocumentLocation(scope.BaseUri, line, column),
            diagnostic.QName.LocalName
        );
    }

    // The same convention as the pattern matcher (namespace Patterns, which the layering rule keeps out of reach of a binding).
    private static (int Line, int Column) LineAndColumn(string text, int offset)
    {
        var line = 1;
        var column = 1;
        for (var i = 0; i < offset && i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                line++;
                column = 1;
            }
            else
            {
                column++;
            }
        }

        return (line, column);
    }
}
