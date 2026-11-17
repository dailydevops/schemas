namespace NetEvolve.Schemas.Xml.Contracts;

using System;

/// <summary>The XML declaration, as spans into the reader's input that are valid only during <see cref="IXmlSink.StartDocument"/>.</summary>
public readonly ref struct XmlDeclaration
{
    /// <summary>Initializes a new instance of the <see cref="XmlDeclaration"/> struct.</summary>
    /// <param name="version">The version, for example <c>1.0</c>.</param>
    /// <param name="encoding">The declared encoding, empty when absent.</param>
    /// <param name="standalone">The standalone value, or <see langword="null"/> when absent.</param>
    public XmlDeclaration(ReadOnlySpan<char> version, ReadOnlySpan<char> encoding, bool? standalone)
    {
        Version = version;
        Encoding = encoding;
        Standalone = standalone;
    }

    /// <summary>Gets the version.</summary>
    public ReadOnlySpan<char> Version { get; }

    /// <summary>Gets the declared encoding, empty when absent.</summary>
    public ReadOnlySpan<char> Encoding { get; }

    /// <summary>Gets the standalone value, <see langword="null"/> when absent.</summary>
    public bool? Standalone { get; }
}
