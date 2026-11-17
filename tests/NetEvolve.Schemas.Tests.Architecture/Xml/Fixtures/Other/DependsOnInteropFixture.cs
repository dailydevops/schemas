namespace NetEvolve.Schemas.Tests.Architecture.Xml.Fixtures.Other;

/// <summary>Violating fixture: a type outside the Interop namespace that depends on an Interop type.</summary>
internal static class DependsOnInteropFixture
{
    public static object Create() => new Interop.LeafFixture();
}
