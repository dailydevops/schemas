namespace NetEvolve.Schemas.XPath.Errors;

using System;

/// <summary>The exception thrown by the convenience members of the XPath API; it carries the diagnostic.</summary>
public sealed class XPathException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="XPathException"/> class.</summary>
    public XPathException()
        : this(string.Empty) { }

    /// <summary>Initializes a new instance of the <see cref="XPathException"/> class.</summary>
    /// <param name="message">The message; the diagnostic is an engine error without a code or location.</param>
    public XPathException(string message)
        : base(message) => Diagnostic = ForMessage(message);

    /// <summary>Initializes a new instance of the <see cref="XPathException"/> class.</summary>
    /// <param name="message">The message; the diagnostic is an engine error without a code or location.</param>
    /// <param name="innerException">The cause.</param>
    public XPathException(string message, Exception innerException)
        : base(message, innerException) => Diagnostic = ForMessage(message);

    /// <summary>Initializes a new instance of the <see cref="XPathException"/> class.</summary>
    /// <param name="diagnostic">The diagnostic.</param>
    public XPathException(XPathDiagnostic diagnostic)
        : base(diagnostic.Message) => Diagnostic = diagnostic;

    /// <summary>Gets the diagnostic.</summary>
    public XPathDiagnostic Diagnostic { get; }

    private static XPathDiagnostic ForMessage(string message) =>
        new(default, XPathErrorKind.Engine, message ?? string.Empty, -1, 0);
}
