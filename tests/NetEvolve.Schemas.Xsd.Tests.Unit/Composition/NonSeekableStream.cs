namespace NetEvolve.Schemas.Xsd.Tests.Unit.Composition;

using System.IO;

/// <summary>A readable stream that reports no length, so the size limit is enforced while reading.</summary>
internal sealed class NonSeekableStream(byte[] content) : MemoryStream(content)
{
    public override bool CanSeek => false;
}
