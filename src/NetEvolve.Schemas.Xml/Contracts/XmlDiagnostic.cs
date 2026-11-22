namespace NetEvolve.Schemas.Xml.Contracts;

using System;
using NetEvolve.Arguments;

/// <summary>A fatal error found while reading: the table row and the position.</summary>
public sealed class XmlDiagnostic
{
    /// <summary>Initializes a new instance of the <see cref="XmlDiagnostic"/> class.</summary>
    /// <param name="code">The error code.</param>
    /// <param name="position">The position where the error was found.</param>
    /// <exception cref="ArgumentNullException"><paramref name="code"/> is <see langword="null"/>.</exception>
    public XmlDiagnostic(XmlErrorCode code, XmlPosition position)
    {
        ArgumentNullException.ThrowIfNull(code);
        Code = code;
        Position = position;
    }

    /// <summary>Gets the error code.</summary>
    public XmlErrorCode Code { get; }

    /// <summary>Gets the position where the error was found.</summary>
    public XmlPosition Position { get; }

    /// <summary>Gets the default message of the code.</summary>
    public string Message => Code.Message;
}
