namespace NetEvolve.Schemas.Xsd.OpenContent;

using System.Runtime.InteropServices;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.ContentModels.AllGroups;
using NetEvolve.Schemas.Xsd.ContentModels.Automaton;
using NetEvolve.Schemas.Xsd.Contracts;

/// <summary>
/// The match step of a content model with open content (Structures 1.1, 3.4.4.3): it wraps the run of the content model and
/// accepts a child that the content model refuses when the wildcard of the open content allows it. The content model decides
/// first, so a declared element, and a wildcard particle, take priority over the open content wildcard.
/// </summary>
/// <remarks>
/// <para>
/// In mode <see cref="OpenContentMode.Interleave"/> an element of the open content leaves the state of the content model as it
/// is. In mode <see cref="OpenContentMode.Suffix"/> it is accepted only when the content model may end, and it begins the
/// suffix: from then on every child must match the wildcard, even one that the content model would accept.
/// </para>
/// <para>
/// A struct with the state of one element frame; copy it only to keep a snapshot. The default value is inactive. A step
/// allocates nothing.
/// </para>
/// </remarks>
[StructLayout(LayoutKind.Auto)]
internal struct OpenContentRun
{
    private readonly OpenContentProperty? _open;
    private readonly CountingRun? _counting;
    private readonly AllGroupRun? _all;
    private bool _inSuffix;

    internal OpenContentRun(OpenContentProperty open, CountingRun counting)
    {
        Argument.ThrowIfNull(open);
        Argument.ThrowIfNull(counting);

        _open = open;
        _counting = counting;
        _all = null;
        _inSuffix = false;
    }

    internal OpenContentRun(OpenContentProperty open, AllGroupRun all)
    {
        Argument.ThrowIfNull(open);
        Argument.ThrowIfNull(all);

        _open = open;
        _counting = null;
        _all = all;
        _inSuffix = false;
    }

    /// <summary>Gets a value indicating whether the run wraps a content model run.</summary>
    public readonly bool IsActive => _open is not null;

    /// <summary>Gets the wildcard of the open content.</summary>
    public readonly IWildcard Wildcard => _open!.Wildcard;

    /// <summary>
    /// Gets a value indicating whether the open content wildcard may match the next child: always for interleave, for suffix
    /// once the content model may end.
    /// </summary>
    public readonly bool WildcardExpected =>
        _open is not null && (_open.Mode == OpenContentMode.Interleave || CanEnd());

    /// <summary>Gets a value indicating whether the suffix began: from then on only the wildcard accepts a child.</summary>
    public readonly bool InSuffix => _inSuffix;

    /// <summary>Returns to the state before the first child, also the wrapped run.</summary>
    public void Reset()
    {
        _inSuffix = false;
        _counting?.Reset();
        _all?.Reset();
    }

    /// <summary>Determines whether the content may end now.</summary>
    /// <returns><see langword="true"/> when the content model is complete.</returns>
    public readonly bool CanEnd() => _counting is not null ? _counting.CanEnd() : _all!.CanEnd();

    /// <summary>Tries to accept a child element. A refused child leaves the run unchanged.</summary>
    /// <param name="schemaNameId">The bound schema name id, <c>-1</c> when the name is unknown to the schema.</param>
    /// <param name="name">The expanded name.</param>
    /// <param name="matched">The matched particle when the content model accepted the child.</param>
    /// <param name="byOpenContent">Whether the open content wildcard accepted the child instead.</param>
    /// <returns><see langword="true"/> when the child is accepted.</returns>
    public bool TryStep(int schemaNameId, XPathQName name, out MatchedParticle matched, out bool byOpenContent)
    {
        byOpenContent = false;
        matched = default;
        if (!_inSuffix && StepContentModel(schemaNameId, name, out matched))
        {
            return true;
        }

        // A run that gave up on its counters (CeilingExceeded) is reported by the caller; open content must not hide it.
        if (_counting is { CeilingExceeded: true } || !_open!.Wildcard.Allows(name))
        {
            return false;
        }

        if (_open.Mode == OpenContentMode.Suffix && !_inSuffix)
        {
            if (!CanEnd())
            {
                return false;
            }

            _inSuffix = true;
        }

        byOpenContent = true;
        return true;
    }

    private readonly bool StepContentModel(int schemaNameId, XPathQName name, out MatchedParticle matched) =>
        _counting is not null
            ? _counting.TryStep(schemaNameId, name, out matched)
            : _all!.TryStep(schemaNameId, out matched);
}
