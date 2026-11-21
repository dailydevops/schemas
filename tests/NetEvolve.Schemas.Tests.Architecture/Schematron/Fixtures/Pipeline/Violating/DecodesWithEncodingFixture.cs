namespace NetEvolve.Schemas.Tests.Architecture.Schematron.Fixtures.Pipeline.Violating;

using System.Text;

/// <summary>Violating fixture: a pipeline type that decodes the bytes of an instance itself.</summary>
internal static class DecodesWithEncodingFixture
{
    public static string Decode(byte[] bytes) => Encoding.UTF8.GetString(bytes);
}
