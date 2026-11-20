namespace NetEvolve.Schemas.Schematron.Xslt.Globals;

using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Scopes;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.XPath.Api;

/// <summary>
/// Collects the global variables and parameters of one schema. A variable and a parameter share the names (XSLT 2.0 section 9.5): two
/// declarations of one expanded name at the same import precedence are the static error XTSE0630, the one of the higher precedence
/// wins, and one of a lower precedence is ignored. The declarations keep the order of their first appearance.
/// </summary>
internal sealed class XsltGlobalTableBuilder
{
    private readonly List<Entry> _entries = [];
    private readonly Dictionary<XPathQName, int> _indexByName = [];

    /// <summary>Adds a declaration.</summary>
    /// <param name="global">The declaration.</param>
    /// <param name="precedence">The import precedence of its module; a higher number wins.</param>
    /// <param name="problem">The problem when the result is <see langword="false"/>: NES3200 with <c>XTSE0630</c>.</param>
    /// <returns><see langword="false"/> when a declaration of the same name and precedence is already added.</returns>
    /// <exception cref="System.ArgumentNullException"><paramref name="global"/> is <see langword="null"/>.</exception>
    internal bool TryAdd(XsltGlobalDeclaration global, int precedence, out SchematronProblem problem)
    {
        Argument.ThrowIfNull(global);

        problem = default;
        if (!_indexByName.TryGetValue(global.Name, out var index))
        {
            _indexByName.Add(global.Name, _entries.Count);
            _entries.Add(new Entry(global, precedence));
            return true;
        }

        var existing = _entries[index];
        if (existing.Precedence == precedence)
        {
            problem = new SchematronProblem(
                SchematronDiagnosticCodes.LetDuplicate,
                $"The global variable or parameter '{global.Declaration.Name}' is declared more than once at the same import precedence; the other declaration is at {Describe(existing.Global.Declaration.Location)}.",
                global.Declaration.Location,
                "XTSE0630"
            );
            return false;
        }

        if (precedence > existing.Precedence)
        {
            _entries[index] = new Entry(global, precedence);
        }

        return true;
    }

    /// <summary>Creates the table from a copy of the declarations added so far.</summary>
    /// <returns>The table.</returns>
    internal XsltGlobalTable Build()
    {
        var declarations = new GlobalDeclaration[_entries.Count];
        var byName = new Dictionary<XPathQName, GlobalDeclaration>(_entries.Count);
        for (var i = 0; i < declarations.Length; i++)
        {
            var global = _entries[i].Global;
            declarations[i] = global.Declaration;
            byName.Add(global.Name, global.Declaration);
        }

        return new XsltGlobalTable(declarations, byName);
    }

    private static string Describe(DocumentLocation location) =>
        location.Source is null ? $"line {location.Line}" : $"{location.Source} line {location.Line}";

    private readonly struct Entry
    {
        internal Entry(XsltGlobalDeclaration global, int precedence)
        {
            Global = global;
            Precedence = precedence;
        }

        internal XsltGlobalDeclaration Global { get; }

        internal int Precedence { get; }
    }
}
