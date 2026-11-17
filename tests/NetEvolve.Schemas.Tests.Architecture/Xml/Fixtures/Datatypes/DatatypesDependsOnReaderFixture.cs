namespace NetEvolve.Schemas.Tests.Architecture.Xml.Fixtures.Datatypes;

/// <summary>Violating fixture: a datatype-layer type that depends on the reader layer.</summary>
internal static class DatatypesDependsOnReaderFixture
{
    public static object Create() => new Reader.ReaderLayerFixture();
}
