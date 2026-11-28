namespace NetEvolve.Schemas.Schematron.Evaluation.Subordinate;

using System.Collections.Generic;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>
/// Reports the two constructs that let a pattern look at nodes outside the main document walk: <c>rule/@visit-each</c> and
/// <c>pattern/@documents</c> (also <c>group/@documents</c>). Both are deferred by decisions Q6 and Q7 of #106 (#387, #388), so
/// evaluation never sees them.
/// </summary>
/// <remarks>
/// <para>
/// <c>visit-each</c> is <c>SCH-UNSUPPORTED</c>. <c>documents</c> is <c>SCH-DOC-DENIED</c> when the resolver is
/// <see cref="DenyAllResourceResolver.Instance"/> and <c>SCH-UNSUPPORTED</c> naming <c>documents</c> for any other resolver (design
/// #137 AP-22). The resolver is never called: <c>documents</c> is a path expression that computes URIs, so there is no concrete
/// reference to request. The check runs on the expanded patterns, so an instance that inherits <c>documents</c> or a rule that
/// carries <c>visit-each</c> from an abstract definition is reported; abstract patterns and abstract rules are never evaluated and
/// are skipped. An empty value counts as absent. Every problem is reported, in document order.
/// </para>
/// <para>
/// Limitations: the model keeps the location of the element, not of the attribute, so the problem points at the <c>rule</c> or
/// at the pattern or group. Like the phase check, the problems are raised in the compile stage, not the load stage of the design.
/// </para>
/// </remarks>
internal static class SubordinateConstructs
{
    /// <summary>Checks the patterns that evaluation would run.</summary>
    /// <param name="patterns">The expanded patterns and groups, in schema order.</param>
    /// <param name="resolver">The resolver of the build settings; only its identity is read.</param>
    /// <param name="problems">Receives one problem per construct, in document order.</param>
    /// <returns><see langword="true"/> when no problem was reported.</returns>
    internal static bool TryCheck(
        IReadOnlyList<SchematronPatternBase> patterns,
        IResourceResolver resolver,
        IProblemSink problems
    )
    {
        ArgumentNullException.ThrowIfNull(patterns);
        ArgumentNullException.ThrowIfNull(resolver);
        ArgumentNullException.ThrowIfNull(problems);

        var denied = ReferenceEquals(resolver, DenyAllResourceResolver.Instance);
        var ok = true;
        foreach (var pattern in patterns)
        {
            if (pattern.IsAbstract)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(pattern.Documents))
            {
                var code = denied ? SchematronDiagnosticCodes.DocumentDenied : SchematronDiagnosticCodes.Unsupported;
                var message = denied
                    ? $"The attribute 'documents' of '{pattern.Id}' is denied: the resolver refuses every document."
                    : $"The attribute 'documents' of '{pattern.Id}' is not supported.";
                problems.Add(new SchematronProblem(code, message, pattern.Location));
                ok = false;
            }

            foreach (var rule in pattern.Rules)
            {
                if (rule.IsAbstract || string.IsNullOrEmpty(rule.VisitEach))
                {
                    continue;
                }

                problems.Add(
                    new SchematronProblem(
                        SchematronDiagnosticCodes.Unsupported,
                        $"The attribute 'visit-each' of rule '{rule.Id}' is not supported.",
                        rule.Location
                    )
                );
                ok = false;
            }
        }

        return ok;
    }
}
