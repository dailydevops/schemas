namespace NetEvolve.Schemas.Tests.Architecture.Xml.Fixtures.Core;

/// <summary>Violating fixture: a core-layer type that depends on the reader layer.</summary>
internal static class CoreDependsOnReaderFixture
{
    public static object Create() => new Reader.ReaderLayerFixture();
}
