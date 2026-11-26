namespace NetEvolve.Schemas.Schematron.Patterns;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Syntax.Parser;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>
/// The one pattern matcher of the package: it decides whether a node matches a rule context, and later serves
/// <c>xsl:key</c> and grouping. The pattern is parsed by the XPath parser, accepted by <see cref="PatternProfile"/>, and
/// matched right to left; predicates are compiled and evaluated through the <see cref="IQueryBinding"/>.
/// </summary>
/// <remarks>
/// Limitations: the XSLT 3.0 forms <c>intersect</c>, <c>except</c>, parentheses, and predicates on a parenthesized
/// pattern are accepted under every XSLT binding (<c>xslt</c>, <c>xslt2</c>, <c>xslt3</c>), so also where the version
/// predates them, which makes the matcher a superset there; the <c>xpath</c> family keeps the XSLT 2.0 subset and reports
/// them as unsupported. No binding version reaches the matcher. Patterns rooted at a variable reference, and
/// and <c>id()</c> patterns raise the unsupported-pattern error (DEF-60, DEF-55); a pattern may start with <c>key()</c> under an XSLT binding (#780). The pattern use makes no
/// difference yet. A predicate is judged numeric by its syntax (see <see cref="PatternPredicates"/>).
/// </remarks>
internal sealed class PatternMatcher : IPatternMatcher
{
    private readonly IQueryBinding _binding;

    /// <summary>Initializes a new instance of the <see cref="PatternMatcher"/> class.</summary>
    /// <param name="binding">The binding that compiles and evaluates the predicates.</param>
    internal PatternMatcher(IQueryBinding binding)
    {
        ArgumentNullException.ThrowIfNull(binding);
        _binding = binding;
    }

    /// <summary>Gets the default priority of every alternative of a compiled pattern, in pattern order.</summary>
    /// <param name="compiled">The pattern this matcher compiled.</param>
    /// <returns>The priorities.</returns>
    internal static IReadOnlyList<decimal> DefaultPriorities(CompiledPattern compiled)
    {
        ArgumentNullException.ThrowIfNull(compiled);
        return ((PatternPlan)compiled).Priorities;
    }

    /// <inheritdoc />
    public bool TryCompile(
        ReadOnlySpan<char> pattern,
        PatternUse use,
        QueryStaticScope scope,
        out CompiledPattern compiled,
        out SchematronProblem problem
    )
    {
        ArgumentNullException.ThrowIfNull(scope);
        compiled = null!;
        var text = pattern.ToString();
        var locator = new PatternLocator(text, scope.BaseUri);
        if (!XPathParser.TryParse(pattern, out var tree, out var syntaxError))
        {
            problem = new SchematronProblem(
                SchematronDiagnosticCodes.ExpressionInvalid,
                syntaxError.Message,
                locator.Locate(syntaxError.Offset),
                syntaxError.QName.LocalName
            );
            return false;
        }

        // Under an XSLT binding the builder checks the paths itself, because the XSLT 3.0 forms wrap them (DEF-60).
        var extended = _binding.AllowsXsltInstructions;
        if (!extended && !PatternProfile.Checker.Accepts(tree!, out var violation))
        {
            problem = new SchematronProblem(
                SchematronDiagnosticCodes.PatternUnsupported,
                violation.Message,
                locator.Locate(violation.Offset),
                violation.QName.LocalName
            );
            return false;
        }

        var builder = new PatternBuilder(_binding, scope, StaticContext(scope), text, locator, extended);
        if (!builder.TryBuild(tree!, out var plan, out problem))
        {
            return false;
        }

        compiled = plan;
        return true;
    }

    /// <inheritdoc />
    public QueryStatus Matches(CompiledPattern compiled, IQueryRun run, in XPathNode candidate, out bool matched)
    {
        ArgumentNullException.ThrowIfNull(compiled);
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(candidate.Tree);

        var alternatives = ((PatternPlan)compiled).Alternatives;
        for (var i = 0; i < alternatives.Length; i++)
        {
            var status = alternatives[i].Matches(run, in candidate, out matched);
            if (status != QueryStatus.Ok || matched)
            {
                return status;
            }
        }

        matched = false;
        return QueryStatus.Ok;
    }

    private static XPathStaticContext StaticContext(QueryStaticScope scope)
    {
        var builder = XPathStaticContext.Default.ToBuilder();
        foreach (var declaration in scope.Namespaces)
        {
            if (declaration.Prefix.Length > 0)
            {
                _ = builder.WithNamespace(declaration.Prefix, declaration.NamespaceName);
            }
        }

        return builder.Build();
    }
}
