namespace NetEvolve.Schemas.Testing.Readme;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>
/// Reads the <c>csharp</c> code blocks of a package readme and the <c>// &lt;sample&gt;</c> regions of a test source file,
/// so a test can prove that every documented sample is the code that is compiled and run.
/// </summary>
public static class ReadmeSamples
{
    private const string Fence = "```";

    /// <summary>Returns the normalized <c>csharp</c> code blocks of a repository file, in document order.</summary>
    /// <param name="relativePath">The path relative to the repository root.</param>
    /// <returns>The code blocks.</returns>
    public static IReadOnlyList<string> CodeBlocks(string relativePath)
    {
        var blocks = new List<string>();
        List<string>? current = null;
        foreach (var line in File.ReadAllLines(Resolve(relativePath)))
        {
            if (current is null)
            {
                if (line.Trim().Equals(Fence + "csharp", StringComparison.Ordinal))
                {
                    current = [];
                }
            }
            else if (line.Trim().Equals(Fence, StringComparison.Ordinal))
            {
                blocks.Add(Normalize(current));
                current = null;
            }
            else
            {
                current.Add(line);
            }
        }

        return blocks;
    }

    /// <summary>Returns the normalized regions between <c>// &lt;sample&gt;</c> and <c>// &lt;/sample&gt;</c>, in file order.</summary>
    /// <param name="relativePath">The path relative to the repository root.</param>
    /// <returns>The samples.</returns>
    public static IReadOnlyList<string> SamplesIn(string relativePath)
    {
        var samples = new List<string>();
        List<string>? current = null;
        foreach (var line in File.ReadAllLines(Resolve(relativePath)))
        {
            var trimmed = line.Trim();
            if (trimmed.Equals("// <sample>", StringComparison.Ordinal))
            {
                current = [];
            }
            else if (trimmed.Equals("// </sample>", StringComparison.Ordinal) && current is not null)
            {
                samples.Add(Normalize(current));
                current = null;
            }
            else
            {
                current?.Add(line);
            }
        }

        return samples;
    }

    /// <summary>Returns the text of a repository file.</summary>
    /// <param name="relativePath">The path relative to the repository root.</param>
    /// <returns>The text.</returns>
    public static string ReadText(string relativePath) => File.ReadAllText(Resolve(relativePath));

    private static string Resolve(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Schemas.slnx")))
        {
            directory = directory.Parent;
        }

        return directory is null
            ? throw new InvalidOperationException("Schemas.slnx was not found above the test output directory.")
            : Path.Combine(directory.FullName, relativePath);
    }

    // Removes the common indentation and trailing whitespace; line endings become \n.
    private static string Normalize(List<string> lines)
    {
        var indent = int.MaxValue;
        foreach (var line in lines)
        {
            if (line.Trim().Length == 0)
            {
                continue;
            }

            var leading = line.Length - line.TrimStart().Length;
            indent = Math.Min(indent, leading);
        }

        var text = new StringBuilder();
        foreach (var line in lines)
        {
            _ = text.Append(line.Trim().Length == 0 ? string.Empty : line.Substring(indent).TrimEnd()).Append('\n');
        }

        return text.ToString().Trim('\n');
    }
}
