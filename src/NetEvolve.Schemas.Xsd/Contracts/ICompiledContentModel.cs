namespace NetEvolve.Schemas.Xsd.Contracts;

/// <summary>An immutable content model program. Its size depends on the number of particles, never on occurrence values.</summary>
internal interface ICompiledContentModel
{
    /// <summary>Gets the number of positions.</summary>
    int PositionCount { get; }

    /// <summary>Gets the number of counter slots a run needs.</summary>
    int CounterSlots { get; }
}
