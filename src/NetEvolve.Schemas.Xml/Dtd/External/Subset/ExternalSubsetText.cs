namespace NetEvolve.Schemas.Xml.Dtd.External.Subset;

using System;
using System.Text;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.External.Loader;

/// <summary>The decoded text of an external subset or an external parameter entity; it keeps the entity open for the recursion check until it is disposed.</summary>
internal sealed class ExternalSubsetText : IDisposable
{
    private readonly ExternalEntityFrame _frame;

    /// <summary>Initializes a new instance of the <see cref="ExternalSubsetText"/> class.</summary>
    /// <param name="frame">The open frame the text was read from; this instance closes it.</param>
    /// <param name="text">The decoded text.</param>
    internal ExternalSubsetText(ExternalEntityFrame frame, string text)
    {
        Argument.ThrowIfNull(frame);
        Argument.ThrowIfNull(text);

        _frame = frame;
        Text = text;
    }

    /// <summary>Gets the decoded text without the text declaration; the line ends are normalized.</summary>
    internal string Text { get; }

    /// <summary>Initializes a new instance of the <see cref="ExternalSubsetText"/> class that streams the frame.</summary>
    /// <param name="frame">The open frame the text is read from; this instance closes it.</param>
    internal ExternalSubsetText(ExternalEntityFrame frame) => throw new NotSupportedException();

    /// <summary>Gets the error that ended the text, <see langword="null"/> while it is read and after a complete read.</summary>
    internal XmlErrorCode? Error => throw new NotSupportedException();

    /// <summary>Gets a value indicating whether the end of the text was reached or an error ended it.</summary>
    internal bool IsEnd => throw new NotSupportedException();

    /// <summary>Appends the next chunk of the entity to the builder.</summary>
    /// <param name="builder">The builder.</param>
    /// <returns><see langword="false"/> at the end of the entity or after an error; look at <see cref="Error"/>.</returns>
    internal bool TryAppend(StringBuilder builder) => throw new NotSupportedException();

    /// <summary>Reads the rest of the entity into one string, as far as it is one unit of the declaration that includes it.</summary>
    /// <param name="maxLength">The most characters; more is <c>XML1115</c>.</param>
    /// <param name="text">The text.</param>
    /// <param name="error">The error when the method returns <see langword="false"/>.</param>
    /// <returns><see langword="true"/> when the entity was read completely.</returns>
    internal bool TryReadAll(int maxLength, out string? text, out XmlErrorCode? error) =>
        throw new NotSupportedException();

    /// <summary>Gets the base URI of the entity: the final URI the resolver reported, otherwise the resolved URI.</summary>
    internal string? BaseUri => _frame.BaseUri;

    /// <summary>Closes the entity; repeated calls do nothing.</summary>
    public void Dispose() => _frame.Dispose();
}
