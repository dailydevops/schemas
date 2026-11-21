namespace NetEvolve.Schemas.Tests.Architecture.Schematron.Fixtures.Seams;

/// <summary>Violating fixture: a seam type that depends on a namespace no rule names, which is forbidden by default.</summary>
internal static class SeamsUseNewcomerFixture
{
    public static object Create() => new Newcomer.NewcomerFixture();
}
