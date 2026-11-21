namespace NetEvolve.Schemas.Tests.Architecture.Schematron.Fixtures.Pipeline.Conforming;

using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.Xml.Model.Loading;

/// <summary>Conforming fixture: a pipeline type that reads an instance through the loader of the Xml package.</summary>
internal static class ReadsWithTheLoaderFixture
{
    public static XmlDocumentTree Read(string text) => XmlDocumentLoader.Load(text.AsSpan());
}
