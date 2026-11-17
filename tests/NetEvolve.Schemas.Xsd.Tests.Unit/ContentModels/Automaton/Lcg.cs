namespace NetEvolve.Schemas.Xsd.Tests.Unit.ContentModels.Automaton;

/// <summary>A tiny deterministic generator, so the models are the same on every run and leg.</summary>
internal sealed class Lcg
{
    private ulong _state;

    public Lcg(ulong seed) => _state = seed;

    public int Next(int maxValue)
    {
        _state = unchecked((_state * 6364136223846793005UL) + 1442695040888963407UL);
        return (int)((_state >> 33) % (ulong)maxValue);
    }

    public int Next(int minValue, int maxValue) => minValue + Next(maxValue - minValue);
}
