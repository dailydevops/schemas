namespace NetEvolve.Schemas.XPath.Functions.Strings;

using System;

/// <summary><c>fn:ends-with</c> (Functions and Operators 1.0, 7.5.3): whether the first string ends with the second; always true for a zero-length second string.</summary>
internal sealed class EndsWithFunction : SearchTestFunction
{
    /// <summary>Initializes a new instance of the <see cref="EndsWithFunction"/> class.</summary>
    public EndsWithFunction()
        : base("ends-with") { }

    /// <inheritdoc />
    protected override bool Test(ReadOnlySpan<char> text, ReadOnlySpan<char> value) =>
        text.EndsWith(value, StringComparison.Ordinal);
}
