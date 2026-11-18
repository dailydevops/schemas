namespace NetEvolve.Schemas.Schematron.Tests.Unit.Scopes;

using System.Collections.Generic;
using System.Threading;
using NetEvolve.Schemas.Schematron.Model;

/// <summary>Builds the small schema models of the scope tests; every element gets its own line so problems can be told apart.</summary>
internal static class ScopeModel
{
    private static int _line;

    internal static DocumentLocation Next() => new("scope.sch", Interlocked.Increment(ref _line), 1);

    internal static SchematronLet Let(string name, string value, string? type = null) => new(Next(), name, type, value);

    internal static SchematronParameter Param(string name, string? value = null) => new(Next(), name, value);

    internal static SchematronNamespace Ns(string prefix, string uri) => new(Next(), prefix, uri);

    internal static SchematronRule Rule(string id, string context, params SchematronLet[] lets) =>
        new(Next(), id, context, null, [], null, null, false, null, lets, [], null);

    internal static SchematronPattern Pattern(string id, SchematronLet[] lets, params SchematronRule[] rules) =>
        new(Next(), id, false, null, null, null, null, [], [], lets, rules, null);

    internal static SchematronGroup Group(string id, SchematronLet[] lets, params SchematronRule[] rules) =>
        new(Next(), id, false, null, null, null, null, [], [], lets, rules, null);

    internal static SchematronPhase Phase(string id, SchematronLet[] lets, params string[] active)
    {
        var actives = new List<SchematronActive>();
        foreach (var pattern in active)
        {
            actives.Add(new SchematronActive(Next(), [], pattern));
        }

        return new SchematronPhase(Next(), id, null, null, [], lets, actives);
    }

    internal static SchematronSchema Schema(
        SchematronLet[]? lets = null,
        SchematronParameter[]? parameters = null,
        SchematronNamespace[]? namespaces = null,
        SchematronPhase[]? phases = null,
        SchematronEdition edition = SchematronEdition.Iso2016,
        params SchematronPatternBase[] patterns
    ) =>
        new(
            Next(),
            null,
            [],
            parameters ?? [],
            lets ?? [],
            patterns,
            null,
            null,
            "scope",
            null,
            null,
            edition,
            null,
            null,
            namespaces ?? [],
            phases ?? [],
            [],
            []
        );
}
