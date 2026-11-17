namespace NetEvolve.Schemas.Tests.Architecture.Shared.Fixtures;

/// <summary>Violating fixture: uses System.Text.Json.</summary>
internal static class JsonUsageFixture
{
    public static System.Text.Json.JsonDocument? Document { get; set; }
}
