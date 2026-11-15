namespace NetEvolve.Schemas.Tests.PublicApi.Fixture.Seeded;

/// <summary>Public type deliberately absent from the verified files; proves unapproved members fail the harness.</summary>
public sealed class SeededMember
{
    public int Unapproved { get; init; }
}
