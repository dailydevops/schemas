namespace NetEvolve.Schemas.Xml.Writer;

using System;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>The exception an <see cref="XmlWriter"/> throws for an event that would produce ill-formed output; nothing is written for that event.</summary>
public sealed class XmlWriteException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="XmlWriteException"/> class.</summary>
    public XmlWriteException()
        : this(string.Empty) { }

    /// <summary>Initializes a new instance of the <see cref="XmlWriteException"/> class.</summary>
    /// <param name="message">The message; there is no code.</param>
    public XmlWriteException(string message)
        : base(message) { }

    /// <summary>Initializes a new instance of the <see cref="XmlWriteException"/> class.</summary>
    /// <param name="message">The message; there is no code.</param>
    /// <param name="innerException">The cause.</param>
    public XmlWriteException(string message, Exception innerException)
        : base(message, innerException) { }

    /// <summary>Initializes a new instance of the <see cref="XmlWriteException"/> class.</summary>
    /// <param name="code">The row of the error code table.</param>
    /// <exception cref="ArgumentNullException"><paramref name="code"/> is <see langword="null"/>.</exception>
    public XmlWriteException(XmlErrorCode code)
        : base(code?.Message ?? string.Empty)
    {
        ArgumentNullException.ThrowIfNull(code);
        Code = code;
    }

    /// <summary>Gets the row of the error code table, <see langword="null"/> for an exception created without one.</summary>
    public XmlErrorCode? Code { get; }
}
