namespace NetEvolve.Schemas.Xml.Dtd.External.Loader;

using System.Runtime.InteropServices;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>Why a request of the <see cref="ExternalEntityLoader"/> did not produce a frame: the error row and a message.</summary>
/// <remarks>The message of a denial carries the kind, the requested URI, and the base URI (design #122, D6).</remarks>
[StructLayout(LayoutKind.Auto)]
internal readonly struct ExternalEntityFailure
{
    /// <summary>Initializes a new instance of the <see cref="ExternalEntityFailure"/> struct.</summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The message.</param>
    internal ExternalEntityFailure(XmlErrorCode code, string message)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(message);

        Code = code;
        Message = message;
    }

    /// <summary>Gets the error code, <see langword="null"/> for the default value.</summary>
    internal XmlErrorCode? Code { get; }

    /// <summary>Gets the message, <see langword="null"/> for the default value.</summary>
    internal string? Message { get; }
}
