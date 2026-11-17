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

    private const int MaxCodePoint = 0x10FFFF;
    private const string NoBlockName = "No_Block";

    /// <summary>Returns the general category of a code point.</summary>
    /// <param name="codePoint">The code point.</param>
    /// <returns>The category; unassigned code points and noncharacters are <see cref="UnicodeGeneralCategory.Cn"/>.</returns>
    internal static UnicodeGeneralCategory GetCategory(int codePoint) =>
        (uint)codePoint > MaxCodePoint
            ? UnicodeGeneralCategory.Cn
            : (UnicodeGeneralCategory)CategoryValues[FloorIndex(CategoryStarts, codePoint)];

    /// <summary>Returns the block of a code point as its position in <c>Blocks.txt</c>.</summary>
    /// <param name="codePoint">The code point.</param>
    /// <returns>The block ordinal for <see cref="GetBlockName(int)"/>, or <see cref="NoBlock"/>.</returns>
    internal static int GetBlock(int codePoint) =>
        (uint)codePoint > MaxCodePoint ? NoBlock : BlockValues[FloorIndex(BlockStarts, codePoint)];

    /// <summary>Returns the <c>Blocks.txt</c> name of a block ordinal, or <c>No_Block</c> for any other value.</summary>
    /// <param name="block">A value returned by <see cref="GetBlock(int)"/>.</param>
    /// <returns>The block name.</returns>
    internal static string GetBlockName(int block) =>
        (uint)block < (uint)BlockNames.Length ? BlockNames[block] : NoBlockName;

    /// <summary>Finds a block by its normalized name (XSD 1.1 Part 2 G.4.2.3), including the superseded names.</summary>
    /// <param name="normalizedName">The name without white space and underscores; hyphens and case are kept.</param>
    /// <returns>A handle for <see cref="IsInBlock(int, int)"/>, or <see cref="NoBlock"/> when the name is unknown.</returns>
    internal static int FindBlock(ReadOnlySpan<char> normalizedName)
    {
        var low = 0;
        var high = NameKeys.Length - 1;
        while (low <= high)
        {
            var mid = (int)((uint)(low + high) >> 1);
            var order = normalizedName.SequenceCompareTo(NameKeys[mid].AsSpan());
            if (order == 0)
            {
                return mid;
            }

            if (order < 0)
            {
                high = mid - 1;
            }
            else
            {
                low = mid + 1;
            }
        }

        return NoBlock;
    }

    /// <summary>Tests whether a code point is in the block a name handle stands for.</summary>
    /// <param name="handle">A value returned by <see cref="FindBlock(ReadOnlySpan{char})"/>.</param>
    /// <param name="codePoint">The code point.</param>
    /// <returns><see langword="true"/> when the code point is in the block.</returns>
    internal static bool IsInBlock(int handle, int codePoint)
    {
        // A name with several ranges (PrivateUse) chains its further ranges through NameNext.
        for (var row = handle; (uint)row < (uint)NameFirst.Length; row = NameNext[row])
        {
            if (codePoint >= NameFirst[row] && codePoint <= NameLast[row])
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns the simple case folding of a code point.</summary>
    /// <param name="codePoint">The code point.</param>
    /// <returns>The folded code point, or <paramref name="codePoint"/> when it has no simple folding.</returns>
    internal static int SimpleCaseFold(int codePoint)
    {
        var low = 0;
        var high = FoldKeys.Length - 1;
        while (low <= high)
        {
            var mid = (int)((uint)(low + high) >> 1);
            var key = FoldKeys[mid];
            if (key == codePoint)
            {
                return FoldValues[mid];
            }

            if (key < codePoint)
            {
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        return codePoint;
    }

    // Returns the index of the last run that starts at or before the code point; the first run starts at 0.
    private static int FloorIndex(int[] starts, int codePoint)
    {
        var low = 0;
        var high = starts.Length - 1;
        while (low < high)
        {
            var mid = (int)((uint)(low + high + 1) >> 1);
            if (starts[mid] <= codePoint)
            {
                low = mid;
            }
            else
            {
                high = mid - 1;
            }
        }

        return low;
    }
}
