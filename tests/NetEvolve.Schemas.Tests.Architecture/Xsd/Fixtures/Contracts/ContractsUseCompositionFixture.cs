namespace NetEvolve.Schemas.Tests.Architecture.Xsd.Fixtures.Contracts;

/// <summary>Violating fixture: a seam type that depends on schema composition.</summary>
internal static class ContractsUseCompositionFixture
{
    public static object Create() => new Composition.LoaderFixture();
}
