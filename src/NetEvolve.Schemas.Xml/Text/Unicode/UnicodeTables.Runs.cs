namespace NetEvolve.Schemas.Xml.Text.Unicode;

/// <summary>Run and range access to the tables, so a caller can build a set of code points without probing every code point.</summary>
internal static partial class UnicodeTables
{
    /// <summary>Gets the number of general category runs; consecutive runs may share a category.</summary>
    internal static int CategoryRunCount => CategoryStarts.Length;

    /// <summary>Returns one general category run.</summary>
    /// <param name="index">The run, 0 to <see cref="CategoryRunCount"/> minus one.</param>
    /// <param name="first">The first code point of the run.</param>
    /// <param name="last">The last code point of the run.</param>
    /// <returns>The category of the run.</returns>
    internal static UnicodeGeneralCategory GetCategoryRun(int index, out int first, out int last)
    {
        first = CategoryStarts[index];
        last = index + 1 < CategoryStarts.Length ? CategoryStarts[index + 1] - 1 : MaxCodePoint;
        return (UnicodeGeneralCategory)CategoryValues[index];
    }

    /// <summary>Returns one range of a block name handle; a name with several ranges chains them through the returned next row.</summary>
    /// <param name="row">The handle of <see cref="FindBlock(System.ReadOnlySpan{char})"/>, then each returned next row.</param>
    /// <param name="first">The first code point of the range.</param>
    /// <param name="last">The last code point of the range.</param>
    /// <returns>The row of the next range; check it with <see cref="IsBlockRow(int)"/>.</returns>
    internal static int GetBlockRange(int row, out int first, out int last)
    {
        first = NameFirst[row];
        last = NameLast[row];
        return NameNext[row];
    }

    /// <summary>Tests whether a row of <see cref="GetBlockRange(int, out int, out int)"/> exists.</summary>
    /// <param name="row">The row.</param>
    /// <returns><see langword="true"/> when the row is valid.</returns>
    internal static bool IsBlockRow(int row) => (uint)row < (uint)NameFirst.Length;
}
