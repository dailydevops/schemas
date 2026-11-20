namespace NetEvolve.Schemas.Xsd.OpenContent;

using System.Runtime.InteropServices;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.ContentModels.AllGroups;
using NetEvolve.Schemas.Xsd.ContentModels.Automaton;
using NetEvolve.Schemas.Xsd.Contracts;

/// <summary>The match step of a content model with open content: wraps the run of the content model.</summary>
[StructLayout(LayoutKind.Auto)]
internal struct OpenContentRun
{
    private readonly OpenContentProperty? _open;
    private readonly CountingRun? _counting;
    private readonly AllGroupRun? _all;

    internal OpenContentRun(OpenContentProperty open, CountingRun counting)
    {
        Argument.ThrowIfNull(open);
        Argument.ThrowIfNull(counting);

        _open = open;
        _counting = counting;
        _all = null;
    }

    internal OpenContentRun(OpenContentProperty open, AllGroupRun all)
    {
        Argument.ThrowIfNull(open);
        Argument.ThrowIfNull(all);

        _open = open;
        _counting = null;
        _all = all;
    }

    /// <summary>Gets a value indicating whether the run wraps a content model run.</summary>
    public readonly bool IsActive => _open is not null;

    /// <summary>Gets the wildcard of the open content.</summary>
    public readonly IWildcard Wildcard => _open!.Wildcard;

    /// <summary>Gets a value indicating whether the open content wildcard may match the next child.</summary>
    public readonly bool WildcardExpected => _open is { Mode: OpenContentMode.None };

    /// <summary>Returns to the state before the first child, also the wrapped run.</summary>
    public void Reset()
    {
        _counting?.Reset();
        _all?.Reset();
    }

    /// <summary>Determines whether the content may end now.</summary>
    /// <returns><see langword="true"/> when the content model is complete.</returns>
    public readonly bool CanEnd() => _counting is not null ? _counting.CanEnd() : _all!.CanEnd();

    /// <summary>Tries to accept a child element.</summary>
    /// <param name="schemaNameId">The bound schema name id, <c>-1</c> when unknown.</param>
    /// <param name="name">The expanded name.</param>
    /// <param name="matched">The matched particle when the content model accepted the child.</param>
    /// <param name="byOpenContent">Whether the open content wildcard accepted the child instead.</param>
    /// <returns><see langword="true"/> when the child is accepted.</returns>
    public bool TryStep(int schemaNameId, XPathQName name, out MatchedParticle matched, out bool byOpenContent)
    {
        byOpenContent = false;
        return _counting is not null
            ? _counting.TryStep(schemaNameId, name, out matched)
            : _all!.TryStep(schemaNameId, out matched);
    }
}
