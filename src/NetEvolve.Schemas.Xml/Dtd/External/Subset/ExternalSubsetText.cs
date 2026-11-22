namespace NetEvolve.Schemas.Xml.Dtd.External.Subset;

using System;
using System.Runtime.InteropServices;
using System.Text;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.External.Loader;

/// <summary>
/// The text of an external subset or an external parameter entity as a stream of chunks (design #122, D9 rule 5): the
/// entity is never buffered completely. It keeps the entity open for the recursion check until it is disposed.
/// </summary>
/// <remarks>Read it with <see cref="TryAppend"/> until it returns <see langword="false"/>, then look at <see cref="Error"/>. Not thread-safe.</remarks>
internal sealed class ExternalSubsetText : IDisposable
{
    private readonly ExternalEntityFrame _frame;

    /// <summary>Initializes a new instance of the <see cref="ExternalSubsetText"/> class.</summary>
    /// <param name="frame">The open frame the text is read from; this instance closes it.</param>
    internal ExternalSubsetText(ExternalEntityFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        _frame = frame;
    }

    /// <summary>Gets the error that ended the text, <see langword="null"/> while it is read and after a complete read.</summary>
    internal XmlErrorCode? Error { get; private set; }

    /// <summary>Gets a value indicating whether the end of the text was reached or an error ended it.</summary>
    internal bool IsEnd { get; private set; }

    /// <summary>Gets the base URI of the entity: the final URI the resolver reported, otherwise the resolved URI.</summary>
    internal string? BaseUri => _frame.BaseUri;

    /// <summary>Appends the next chunk of the entity to the builder; the text declaration is consumed and the line ends are normalized.</summary>
    /// <param name="builder">The builder.</param>
    /// <returns><see langword="false"/> at the end of the entity or after an error; look at <see cref="Error"/>.</returns>
    internal bool TryAppend(StringBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (IsEnd)
        {
            return false;
        }

        if (!_frame.TryRead(out var chunk))
        {
            IsEnd = true;
            Error = _frame.Error;
            return false;
        }

        if (MemoryMarshal.TryGetArray(chunk, out var segment) && segment.Array is not null)
        {
            _ = builder.Append(segment.Array, segment.Offset, segment.Count);
        }
        else
        {
            // The frame reads from pooled arrays, so this is not taken; the loop keeps the method total.
            foreach (var c in chunk.Span)
            {
                _ = builder.Append(c);
            }
        }

        return true;
    }

    /// <summary>Reads the rest of the entity into one string, for an entity that is one unit of the declaration that includes it.</summary>
    /// <param name="maxLength">The most characters; more is <c>XML1115</c>.</param>
    /// <param name="text">The text when the method returns <see langword="true"/>.</param>
    /// <param name="error">The error when the method returns <see langword="false"/>.</param>
    /// <returns><see langword="true"/> when the entity was read completely.</returns>
    internal bool TryReadAll(int maxLength, out string? text, out XmlErrorCode? error)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxLength, 0);

        text = null;
        var builder = new StringBuilder();
        while (TryAppend(builder))
        {
            if (builder.Length > maxLength)
            {
                error = XmlErrorCodes.XML1115;
                return false;
            }
        }

        error = Error;
        if (error is not null)
        {
            return false;
        }

        text = builder.ToString();
        return true;
    }

    /// <summary>Closes the entity; repeated calls do nothing.</summary>
    public void Dispose() => _frame.Dispose();
}
