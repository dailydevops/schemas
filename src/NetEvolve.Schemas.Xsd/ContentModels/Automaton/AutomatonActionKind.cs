namespace NetEvolve.Schemas.Xsd.ContentModels.Automaton;

/// <summary>What a transition does to one counter slot.</summary>
internal enum AutomatonActionKind
{
    /// <summary>Requires <c>count &gt;= Limit</c> before the counted node is left.</summary>
    Exit = 0,

    /// <summary>Requires <c>count &lt; Limit</c>, then increments up to <c>Cap</c>.</summary>
    Iterate = 1,

    /// <summary>Resets the count to one on entering the counted node.</summary>
    Enter = 2,
}
