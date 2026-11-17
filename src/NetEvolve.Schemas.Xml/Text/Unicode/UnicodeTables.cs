namespace NetEvolve.Schemas.Xml.Text.Unicode;

using System;

/// <summary>
/// Embedded lookup tables for the Unicode general category, the block, and the simple case folding of a code point.
/// </summary>
/// <remarks>
/// Results follow the one pinned Unicode version <see cref="Version"/> on every target framework, never the Unicode
/// version of the running .NET. Limitations: case folding is the simple folding only (statuses C and S of
/// <c>CaseFolding.txt</c>); full (F) and Turkic (T) foldings are not applied. A value outside U+0000 to U+10FFFF is
/// unassigned (<see cref="UnicodeGeneralCategory.Cn"/>), in no block, and folds to itself.
/// </remarks>
internal static partial class UnicodeTables
{
    /// <summary>The pinned Unicode Character Database version, identical to the version in <c>ucd.json</c>.</summary>
    internal const string Version = "18.0.0";

    /// <summary>The block value of a code point that is in no block (<c>No_Block</c> in <c>Blocks.txt</c>).</summary>
    internal const int NoBlock = -1;

    /// <summary>Returns the general category of a code point.</summary>
    /// <param name="codePoint">The code point.</param>
    /// <returns>The category; unassigned code points and noncharacters are <see cref="UnicodeGeneralCategory.Cn"/>.</returns>
    internal static UnicodeGeneralCategory GetCategory(int codePoint) => throw new NotSupportedException();

    /// <summary>Returns the block of a code point as its position in <c>Blocks.txt</c>.</summary>
    /// <param name="codePoint">The code point.</param>
    /// <returns>The block ordinal for <see cref="GetBlockName(int)"/>, or <see cref="NoBlock"/>.</returns>
    internal static int GetBlock(int codePoint) => throw new NotSupportedException();

    /// <summary>Returns the <c>Blocks.txt</c> name of a block ordinal, or <c>No_Block</c> for <see cref="NoBlock"/>.</summary>
    /// <param name="block">A value returned by <see cref="GetBlock(int)"/>.</param>
    /// <returns>The block name.</returns>
    internal static string GetBlockName(int block) => throw new NotSupportedException();

    /// <summary>Finds a block by its normalized name (XSD 1.1 Part 2 G.4.2.3), including the superseded names.</summary>
    /// <param name="normalizedName">The name without white space and underscores; hyphens and case are kept.</param>
    /// <returns>A handle for <see cref="IsInBlock(int, int)"/>, or <see cref="NoBlock"/> when the name is unknown.</returns>
    internal static int FindBlock(ReadOnlySpan<char> normalizedName) => throw new NotSupportedException();

    /// <summary>Tests whether a code point is in the block a name handle stands for.</summary>
    /// <param name="handle">A value returned by <see cref="FindBlock(ReadOnlySpan{char})"/>.</param>
    /// <param name="codePoint">The code point.</param>
    /// <returns><see langword="true"/> when the code point is in the block.</returns>
    internal static bool IsInBlock(int handle, int codePoint) => throw new NotSupportedException();

    /// <summary>Returns the simple case folding of a code point.</summary>
    /// <param name="codePoint">The code point.</param>
    /// <returns>The folded code point, or <paramref name="codePoint"/> when it has no simple folding.</returns>
    internal static int SimpleCaseFold(int codePoint) => throw new NotSupportedException();
}
