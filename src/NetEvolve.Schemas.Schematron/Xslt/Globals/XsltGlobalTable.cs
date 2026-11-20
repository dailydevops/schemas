namespace NetEvolve.Schemas.Schematron.Xslt.Globals;

using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Scopes;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.XPath.Api;

/// <summary>
/// The global variables and parameters of a compiled schema, one per expanded name after the import precedence was applied. The
/// planner gives <see cref="Declarations"/> their slots; the table also answers the build-time question whether every required
/// parameter has a value from the caller. Immutable and thread-safe; it is created by <see cref="XsltGlobalTableBuilder"/>.
/// </summary>
/// <remarks>
/// Limitation: a value from the caller is a literal string, as the builder takes it (design #137 AP-9), and it is bound without a
/// conversion to the <see langword="as"/> type of the parameter, so XTTE0590 for a value that does not convert is not raised (#795).
/// A caller value is matched with the name of the parameter as written.
/// </remarks>
internal sealed class XsltGlobalTable
{
    private readonly GlobalDeclaration[] _declarations;
    private readonly Dictionary<XPathQName, GlobalDeclaration> _byName;

    internal XsltGlobalTable(GlobalDeclaration[] declarations, Dictionary<XPathQName, GlobalDeclaration> byName)
    {
        Argument.ThrowIfNull(declarations);
        Argument.ThrowIfNull(byName);

        _declarations = declarations;
        _byName = byName;
    }

    /// <summary>Gets the number of declarations.</summary>
    internal int Count => _declarations.Length;

    /// <summary>Gets the declarations in the order of their first appearance.</summary>
    internal IReadOnlyList<GlobalDeclaration> Declarations => _declarations;

    /// <summary>Finds a declaration by expanded name.</summary>
    /// <param name="name">The expanded name.</param>
    /// <param name="declaration">The declaration when the result is <see langword="true"/>.</param>
    /// <returns><see langword="true"/> when a declaration has the name.</returns>
    internal bool TryGet(in XPathQName name, out GlobalDeclaration? declaration) =>
        _byName.TryGetValue(name, out declaration);

    /// <summary>
    /// Checks that the caller gave a value to every required parameter. A stylesheet that declares a required parameter without a
    /// value for it cannot run (XTDE0050); the builder knows the values, so the error is a build error.
    /// </summary>
    /// <param name="supplied">The values of the builder, by parameter name.</param>
    /// <param name="problems">Receives one problem for every required parameter without a value.</param>
    /// <returns><see langword="true"/> when no parameter is missing.</returns>
    /// <exception cref="System.ArgumentNullException"><paramref name="supplied"/> or <paramref name="problems"/> is <see langword="null"/>.</exception>
    internal bool CheckParameters(IReadOnlyDictionary<string, string> supplied, IProblemSink problems)
    {
        Argument.ThrowIfNull(supplied);
        Argument.ThrowIfNull(problems);

        var ok = true;
        for (var i = 0; i < _declarations.Length; i++)
        {
            var declaration = _declarations[i];
            if (!declaration.IsRequired || supplied.ContainsKey(declaration.Name))
            {
                continue;
            }

            var problem = new SchematronProblem(
                SchematronDiagnosticCodes.ParamMissing,
                $"The parameter '{declaration.Name}' is required and has no value; set it with WithParameter.",
                declaration.Location,
                "XTDE0050"
            );
            problems.Add(in problem);
            ok = false;
        }

        return ok;
    }
}
