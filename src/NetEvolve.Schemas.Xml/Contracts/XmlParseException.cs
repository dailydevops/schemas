namespace NetEvolve.Schemas.Xml.Contracts;

using System;

/// <summary>The exception the convenience facades throw for an <see cref="XmlReadStatus.Error"/>; the core never throws it.</summary>
public sealed class XmlParseException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="XmlParseException"/> class.</summary>
    public XmlParseException()
        : this(string.Empty) { }

    /// <summary>Initializes a new instance of the <see cref="XmlParseException"/> class.</summary>
    /// <param name="message">The message; there is no diagnostic.</param>
    public XmlParseException(string message)
        : base(message) { }

    /// <summary>Initializes a new instance of the <see cref="XmlParseException"/> class.</summary>
    /// <param name="message">The message; there is no diagnostic.</param>
    /// <param name="innerException">The cause.</param>
    public XmlParseException(string message, Exception innerException)
        : base(message, innerException) { }

    /// <summary>Initializes a new instance of the <see cref="XmlParseException"/> class.</summary>
    /// <param name="diagnostic">The diagnostic, or <see langword="null"/>.</param>
    public XmlParseException(XmlDiagnostic diagnostic)
        : base(diagnostic?.Message ?? string.Empty) => Diagnostic = diagnostic;

    /// <summary>Gets the diagnostic.</summary>
    public XmlDiagnostic? Diagnostic { get; }
}
