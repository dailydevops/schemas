namespace NetEvolve.Schemas.Tests.PublicApi.Fixture.Approved;

/// <summary>Approved fixture surface locked by the verified files in the parent folder.</summary>
public sealed class FixtureApi
{
    public int Value { get; init; }

    public string Describe() => $"Value={Value}";
}
