namespace NetEvolve.Schemas.Tests.Architecture.Xsd.Fixtures.Composition;

/// <summary>Violating fixture: composition that depends on the component registry built from its result.</summary>
internal static class CompositionUsesComponentsFixture
{
    public static object Create() => new Components.ComponentFixture();
}
