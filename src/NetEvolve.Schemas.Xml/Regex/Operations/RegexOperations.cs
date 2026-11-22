namespace NetEvolve.Schemas.Xml.Regex.Operations;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Buffers;
using NetEvolve.Schemas.Xml.Regex.Contracts;
using NetEvolve.Schemas.Xml.Regex.Matching;
using NetEvolve.Schemas.Xml.Regex.Parsing;

/// <summary>
/// The operations behind the XPath functions <c>matches</c>, <c>replace</c>, and <c>tokenize</c> on a compiled pattern of the
/// XPath dialect: enumeration of matches with their capture groups, replacement with the replacement string syntax, and
/// tokenization. The instance is immutable and may be shared between threads.
/// </summary>
/// <remarks>
/// Limitations: the matches are those of <see cref="RegexProgram.Find"/>, so a pattern without a back reference runs on the
/// step-limited automaton, which keeps the capture bounds of each thread (the state grows with program size times group count
/// and is capped, a larger pattern is <see cref="RegexError.LimitExceeded"/>), and a pattern with a back reference runs on the
/// backtracking machine; a step ceiling ends an operation as <see cref="RegexOperationStatus.Aborted"/> although the
/// specification defines an answer. A pattern that matches the empty string is an error for replace and tokenize, also for the
/// empty input, and the check comes before the check of the replacement string; the replacement string is checked also when
/// nothing matches. Replace and tokenize use leftmost matches that do not overlap.
/// </remarks>
public sealed class RegexOperations
{
    private readonly RegexProgram _program;

    private RegexOperations(RegexProgram program, int groupCount, bool matchesEmptyString)
    {
        _program = program;
        GroupCount = groupCount;
        MatchesEmptyString = matchesEmptyString;
    }

    /// <summary>Gets the number of capture groups of the pattern, not counting group 0.</summary>
    public int GroupCount { get; }

    /// <summary>Gets a value indicating whether the pattern matches the empty string, which replace and tokenize reject.</summary>
    public bool MatchesEmptyString { get; }

    internal RegexProgram Program => _program;

    /// <summary>Compiles the tree of a parsed pattern of the XPath dialect for the operations.</summary>
    /// <param name="tree">The tree, parsed in the XPath dialect with the flags of the call.</param>
    /// <param name="operations">The operations, <see langword="null"/> on an error.</param>
    /// <returns><see cref="RegexError.None"/>, <see cref="RegexError.ConstructNotAllowed"/> for a tree of another dialect, <see cref="RegexError.InvalidBackReference"/>, or <see cref="RegexError.LimitExceeded"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="tree"/> is <see langword="null"/>.</exception>
    public static RegexError TryCreate(RegexTree tree, out RegexOperations? operations)
    {
        ArgumentNullException.ThrowIfNull(tree);

        operations = null;
        var error = RegexProgram.TryCompileForOperations(tree, out var program);
        if (error != RegexError.None)
        {
            return error;
        }

        var groupCount = RegexProgram.CountGroups(tree.Nodes);
        var probe = new int[2 * (groupCount + 1)];
        var matchesEmptyString =
            program!.FindGroups(ReadOnlySpan<char>.Empty, 0, null, probe) == RegexMatchResult.Match;
        operations = new RegexOperations(program, groupCount, matchesEmptyString);
        return RegexError.None;
    }

    /// <summary>Enumerates the matches of the pattern in an input.</summary>
    /// <param name="input">The input; it must stay unchanged while the enumerator is in use.</param>
    /// <param name="limits">The step ceilings, <see cref="RegexStepLimits.Default"/> for <see langword="null"/>; every search of the enumeration has its own budget.</param>
    /// <returns>The enumerator; dispose it, or use it in a <see langword="foreach"/> loop.</returns>
    public RegexMatchEnumerator EnumerateMatches(ReadOnlySpan<char> input, RegexStepLimits? limits = null) =>
        new(this, input, limits);

    /// <summary>Replaces every match with the replacement string and returns the result as a string.</summary>
    /// <param name="input">The input.</param>
    /// <param name="replacement">The replacement string: <c>$N</c> is the text of group N (<c>$0</c> the whole match, empty for a group that took no part or does not exist), <c>\$</c> a dollar sign, and <c>\\</c> a backslash.</param>
    /// <param name="limits">The step ceilings, <see cref="RegexStepLimits.Default"/> for <see langword="null"/>.</param>
    /// <param name="result">The replaced text, <see langword="null"/> unless the status is <see cref="RegexOperationStatus.Success"/>.</param>
    /// <returns>The status.</returns>
    public RegexOperationStatus Replace(
        ReadOnlySpan<char> input,
        ReadOnlySpan<char> replacement,
        RegexStepLimits? limits,
        out string? result
    )
    {
        using var buffer = new PooledBuffer<char>(input.Length);
        var status = ReplaceCore(input, replacement, limits, buffer);
        result = status == RegexOperationStatus.Success ? buffer.WrittenSpan.ToString() : null;
        return status;
    }

    /// <summary>Replaces every match with the replacement string and writes the result into a caller-supplied destination.</summary>
    /// <param name="input">The input.</param>
    /// <param name="replacement">The replacement string, see <see cref="Replace(ReadOnlySpan{char}, ReadOnlySpan{char}, RegexStepLimits, out string)"/>.</param>
    /// <param name="destination">The destination for the replaced text.</param>
    /// <param name="limits">The step ceilings, <see cref="RegexStepLimits.Default"/> for <see langword="null"/>.</param>
    /// <param name="charsWritten">The length of the replaced text, also when it does not fit the destination; zero on any other outcome than success.</param>
    /// <returns>The status; <see cref="RegexOperationStatus.DestinationTooSmall"/> when the result is longer than the destination, which is then unchanged.</returns>
    public RegexOperationStatus Replace(
        ReadOnlySpan<char> input,
        ReadOnlySpan<char> replacement,
        Span<char> destination,
        RegexStepLimits? limits,
        out int charsWritten
    )
    {
        using var buffer = new PooledBuffer<char>(input.Length);
        var status = ReplaceCore(input, replacement, limits, buffer);
        charsWritten = 0;
        if (status != RegexOperationStatus.Success)
        {
            return status;
        }

        charsWritten = buffer.Length;
        if (buffer.Length > destination.Length)
        {
            return RegexOperationStatus.DestinationTooSmall;
        }

        buffer.WrittenSpan.CopyTo(destination);
        return RegexOperationStatus.Success;
    }

    /// <summary>Splits an input at the matches of the pattern.</summary>
    /// <param name="input">The input; the empty input has no tokens.</param>
    /// <param name="limits">The step ceilings, <see cref="RegexStepLimits.Default"/> for <see langword="null"/>.</param>
    /// <param name="tokens">The tokens as ranges of the input; empty unless the status is <see cref="RegexOperationStatus.Success"/>.</param>
    /// <returns><see cref="RegexOperationStatus.Success"/> or <see cref="RegexOperationStatus.ZeroLengthMatch"/>; a step ceiling is reported by <see cref="RegexTokenEnumerator.IsAborted"/>.</returns>
    public RegexOperationStatus TryTokenize(
        ReadOnlySpan<char> input,
        RegexStepLimits? limits,
        out RegexTokenEnumerator tokens
    )
    {
        tokens = default;
        if (MatchesEmptyString)
        {
            return RegexOperationStatus.ZeroLengthMatch;
        }

        if (!input.IsEmpty)
        {
            tokens = new RegexTokenEnumerator(EnumerateMatches(input, limits), input.Length);
        }

        return RegexOperationStatus.Success;
    }

    private RegexOperationStatus ReplaceCore(
        ReadOnlySpan<char> input,
        ReadOnlySpan<char> replacement,
        RegexStepLimits? limits,
        PooledBuffer<char> destination
    )
    {
        if (MatchesEmptyString)
        {
            return RegexOperationStatus.ZeroLengthMatch;
        }

        if (!ReplacementSyntax.IsValid(replacement))
        {
            return RegexOperationStatus.InvalidReplacement;
        }

        var copied = 0;
        var matches = EnumerateMatches(input, limits);
        try
        {
            while (matches.MoveNext())
            {
                var match = matches.Current;
                destination.Append(input.Slice(copied, match.Index - copied));
                ReplacementSyntax.Append(destination, replacement, match);
                copied = match.Index + match.Length;
            }

            if (matches.IsAborted)
            {
                return RegexOperationStatus.Aborted;
            }
        }
        finally
        {
            matches.Dispose();
        }

        destination.Append(input.Slice(copied));
        return RegexOperationStatus.Success;
    }
}
