namespace NetEvolve.Schemas.Tests.Architecture.Xsd.Fixtures.Components;

/// <summary>Violating fixture: the component registry that depends on schema composition.</summary>
internal static class ComponentsUseCompositionFixture
{
    public static object Create() => new Composition.LoaderFixture();
}
