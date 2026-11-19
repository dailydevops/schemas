namespace NetEvolve.Schemas.XPath.Functions.Strings;

using System;

/// <summary><c>fn:starts-with</c> (Functions and Operators 1.0, 7.5.2): whether the first string begins with the second; always true for a zero-length second string.</summary>
internal sealed class StartsWithFunction : SearchTestFunction
{
    /// <summary>Initializes a new instance of the <see cref="StartsWithFunction"/> class.</summary>
    public StartsWithFunction()
        : base("starts-with") { }

    /// <inheritdoc />
    protected override bool Test(ReadOnlySpan<char> text, ReadOnlySpan<char> value) =>
        text.StartsWith(value, StringComparison.Ordinal);
}
