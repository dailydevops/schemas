namespace NetEvolve.Schemas.Xml.Input.Decoding;

using System.Runtime.InteropServices;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>A fatal decoding error: the table row and the zero-based offset of the first offending byte.</summary>
/// <remarks>The default value means no error; its <see cref="Code"/> is <see langword="null"/>.</remarks>
[StructLayout(LayoutKind.Auto)]
internal readonly struct XmlDecodingError
{
    /// <summary>Initializes a new instance of the <see cref="XmlDecodingError"/> struct.</summary>
    /// <param name="code">The error code.</param>
    /// <param name="byteOffset">The offset of the first byte of the offending sequence, counted from the first input byte.</param>
    internal XmlDecodingError(XmlErrorCode code, long byteOffset)
    {
        Code = code;
        ByteOffset = byteOffset;
    }

    /// <summary>Gets the error code, or <see langword="null"/> when there is no error.</summary>
    internal XmlErrorCode? Code { get; }

    /// <summary>Gets the offset of the first byte of the offending sequence.</summary>
    internal long ByteOffset { get; }
}
