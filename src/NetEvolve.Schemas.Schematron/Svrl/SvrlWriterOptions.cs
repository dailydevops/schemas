namespace NetEvolve.Schemas.Schematron.Svrl;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Writer;

/// <summary>Immutable options of the <see cref="SvrlReportSink"/>.</summary>
public sealed class SvrlWriterOptions
{
    /// <summary>The default for <see cref="MaxBufferedBytes"/>, 16 MiB.</summary>
    public const int DefaultMaxBufferedBytes = 16 * 1024 * 1024;

    /// <summary>The highest allowed <see cref="MaxBufferedBytes"/>, below the maximum array length.</summary>
    public const int MaxBufferedBytesCeiling = 2_000_000_000;

    /// <summary>Initializes a new instance of the <see cref="SvrlWriterOptions"/> class.</summary>
    /// <param name="encoding">The encoding of byte output.</param>
    /// <param name="indent">Whether to indent the output.</param>
    /// <param name="emitNamespaceDeclarations">Whether to declare the namespace prefixes of the schema on the root element.</param>
    /// <param name="maxBufferedBytes">The cap of the pooled buffer of one active pattern, 1 to <see cref="MaxBufferedBytesCeiling"/>.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="maxBufferedBytes"/> is outside 1 to <see cref="MaxBufferedBytesCeiling"/>.</exception>
    public SvrlWriterOptions(
        XmlWriterEncoding encoding = XmlWriterEncoding.Utf8,
        bool indent = false,
        bool emitNamespaceDeclarations = true,
        int maxBufferedBytes = DefaultMaxBufferedBytes
    )
    {
        Argument.ThrowIfLessThan(maxBufferedBytes, 1);
        Argument.ThrowIfGreaterThan(maxBufferedBytes, MaxBufferedBytesCeiling);

        Encoding = encoding;
        Indent = indent;
        EmitNamespaceDeclarations = emitNamespaceDeclarations;
        MaxBufferedBytes = maxBufferedBytes;
    }

    /// <summary>Gets the default options: UTF-8, not indented, namespace declarations emitted, 16 MiB buffer.</summary>
    public static SvrlWriterOptions Default { get; } = new();

    /// <summary>Gets the encoding of byte output.</summary>
    public XmlWriterEncoding Encoding { get; }

    /// <summary>Gets a value indicating whether the output is indented.</summary>
    public bool Indent { get; }

    /// <summary>Gets a value indicating whether the namespace prefixes of the schema are declared on the root element.</summary>
    public bool EmitNamespaceDeclarations { get; }

    /// <summary>Gets the cap in bytes of the pooled buffer that holds the events of one active pattern.</summary>
    public int MaxBufferedBytes { get; }
}
