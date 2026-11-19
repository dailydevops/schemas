namespace NetEvolve.Schemas.XPath.Functions.Strings;

using System;

/// <summary><c>fn:contains</c> (Functions and Operators 1.0, 7.5.1): whether the first string holds the second; always true for a zero-length second string.</summary>
internal sealed class ContainsFunction : SearchTestFunction
{
    /// <summary>Initializes a new instance of the <see cref="ContainsFunction"/> class.</summary>
    public ContainsFunction()
        : base("contains") { }

    /// <inheritdoc />
    protected override bool Test(ReadOnlySpan<char> text, ReadOnlySpan<char> value) =>
        text.IndexOf(value, StringComparison.Ordinal) >= 0;
}
