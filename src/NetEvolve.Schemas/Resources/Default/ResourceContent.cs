namespace NetEvolve.Schemas.Resources.Default;

using System;
using System.Buffers;
using System.IO;

/// <summary>Reads the content of a found <see cref="ResourceResult"/> under the <see cref="ValidationLimits.MaxResourceSize"/> cap.</summary>
/// <remarks>Limitation: a stream is copied once into memory, so a loader holds at most one resource of at most the limit.</remarks>
internal static class ResourceContent
{
    private const int BufferLength = 8192;

    /// <summary>Reads the content of a found result.</summary>
    /// <param name="result">The found result; the caller disposes it.</param>
    /// <param name="limit">The maximum size in bytes.</param>
    /// <param name="content">The content, empty when the size exceeds <paramref name="limit"/>.</param>
    /// <returns><see langword="true"/> when the content is at most <paramref name="limit"/> bytes.</returns>
    internal static bool TryRead(in ResourceResult result, long limit, out ReadOnlyMemory<byte> content)
    {
        content = default;
        var stream = result.Stream;
        if (stream is null)
        {
            if (result.Content.Length > limit)
            {
                return false;
            }

            content = result.Content;
            return true;
        }

        if (stream.CanSeek && stream.Length > limit)
        {
            return false;
        }

        using var buffer = new MemoryStream();
        var rented = ArrayPool<byte>.Shared.Rent(BufferLength);
        try
        {
            long total = 0;
            int read;
            while ((read = stream.Read(rented, 0, rented.Length)) > 0)
            {
                total += read;
                if (total > limit)
                {
                    return false;
                }

                buffer.Write(rented, 0, read);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }

        content = buffer.ToArray();
        return true;
    }
}
