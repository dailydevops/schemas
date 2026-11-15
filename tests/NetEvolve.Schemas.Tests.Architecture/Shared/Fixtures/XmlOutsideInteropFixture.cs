namespace NetEvolve.Schemas.Tests.Architecture.Shared.Fixtures;

/// <summary>Violating fixture: uses a System.Xml type outside an Interop namespace.</summary>
internal static class XmlOutsideInteropFixture
{
    public static System.Xml.XmlDocument Document { get; } = new();
}
