namespace NetEvolve.Schemas.Tests.Architecture.Schematron.Fixtures.Pipeline.Violating;

using System.IO;

/// <summary>Violating fixture: a pipeline type that reads an instance through a text reader.</summary>
internal static class ReadsWithTextReaderFixture
{
    public static string Read(Stream stream)
    {
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
