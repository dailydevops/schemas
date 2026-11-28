namespace NetEvolve.Schemas.Schematron.Xslt.Functions;

using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.XPath.Api;

/// <summary>
/// Collects the functions of one schema. Two functions with the same expanded name and arity at the same import precedence are the
/// static error XTSE0770; the one of the higher precedence wins, and one of a lower precedence is ignored (XSLT 2.0 section 10.3).
/// </summary>
internal sealed class XsltFunctionTableBuilder
{
    private readonly Dictionary<XPathQName, List<Entry>> _byName = [];
    private int _count;

    /// <summary>Adds a function.</summary>
    /// <param name="function">The function.</param>
    /// <param name="precedence">The import precedence of its module; a higher number wins.</param>
    /// <param name="problem">The problem when the result is <see langword="false"/>: NES3200 with <c>XTSE0770</c>.</param>
    /// <returns><see langword="false"/> when a function of the same name, arity, and precedence is already added.</returns>
    internal bool TryAdd(XsltFunctionComponent function, int precedence, out SchematronProblem problem)
    {
        ArgumentNullException.ThrowIfNull(function);

        problem = default;
        if (!_byName.TryGetValue(function.Name, out var overloads))
        {
            overloads = [];
            _byName.Add(function.Name, overloads);
        }

        for (var i = 0; i < overloads.Count; i++)
        {
            var existing = overloads[i];
            if (existing.Function.Arity != function.Arity)
            {
                continue;
            }

            if (existing.Precedence == precedence)
            {
                problem = new SchematronProblem(
                    SchematronDiagnosticCodes.LetDuplicate,
                    $"The function '{function.DisplayName}' with {function.Arity} parameters is declared more than once at the same import precedence; the other declaration is at line {existing.Function.Location.Line}.",
                    function.Location,
                    "XTSE0770"
                );
                return false;
            }

            if (precedence > existing.Precedence)
            {
                overloads[i] = new Entry(function, precedence);
            }

            return true;
        }

        overloads.Add(new Entry(function, precedence));
        _count++;
        return true;
    }

    /// <summary>Creates the table from a copy of the functions added so far.</summary>
    /// <returns>The table.</returns>
    internal XsltFunctionTable Build()
    {
        var byName = new Dictionary<XPathQName, XsltFunctionComponent[]>(_byName.Count);
        foreach (var pair in _byName)
        {
            var functions = new XsltFunctionComponent[pair.Value.Count];
            for (var i = 0; i < functions.Length; i++)
            {
                functions[i] = pair.Value[i].Function;
            }

            byName.Add(pair.Key, functions);
        }

        return new XsltFunctionTable(byName, _count);
    }

    private readonly struct Entry
    {
        internal Entry(XsltFunctionComponent function, int precedence)
        {
            Function = function;
            Precedence = precedence;
        }

        internal XsltFunctionComponent Function { get; }

        internal int Precedence { get; }
    }
}
