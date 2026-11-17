namespace NetEvolve.Schemas.Xml.Input.Decoding;

using System.Runtime.InteropServices;

/// <summary>The result of the Appendix F detection: the encoding and the length of the byte order mark.</summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct XmlEncodingDetection
{
    /// <summary>Initializes a new instance of the <see cref="XmlEncodingDetection"/> struct.</summary>
    /// <param name="encoding">The detected encoding.</param>
    /// <param name="bomLength">The length of the byte order mark in bytes, zero when there is none.</param>
    internal XmlEncodingDetection(XmlInputEncoding encoding, int bomLength)
    {
        Encoding = encoding;
        BomLength = bomLength;
    }

    /// <summary>Gets the detected encoding.</summary>
    internal XmlInputEncoding Encoding { get; }

    /// <summary>Gets the length of the byte order mark in bytes, zero when there is none.</summary>
    internal int BomLength { get; }

    /// <summary>Gets a value indicating whether the input starts with a byte order mark.</summary>
    internal bool HasByteOrderMark => BomLength > 0;
}
