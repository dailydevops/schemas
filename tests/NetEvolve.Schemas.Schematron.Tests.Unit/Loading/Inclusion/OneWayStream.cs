namespace NetEvolve.Schemas.Schematron.Tests.Unit.Loading.Inclusion;

using System.IO;

/// <summary>A readable stream that reports it cannot seek, so its size is only known by reading it.</summary>
internal sealed class OneWayStream(byte[] bytes) : MemoryStream(bytes)
{
    public override bool CanSeek => false;
}
