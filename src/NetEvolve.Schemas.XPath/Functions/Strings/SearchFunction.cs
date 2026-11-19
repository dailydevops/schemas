namespace NetEvolve.Schemas.XPath.Functions.Strings;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Functions.Sequences;

/// <summary>
/// The base of the functions that look for one string in another (Functions and Operators 1.0, 7.5): two string arguments, which an
/// empty sequence makes the zero-length string, and an optional collation, which <see cref="CollationArgument"/> checks, so only the
/// codepoint collation is accepted (any other is FOCH0002). The codepoint collation compares code points, which an ordinal comparison of
/// the code units does as well for valid strings, and which allocates nothing.
/// </summary>
internal abstract class SearchFunction : TextFunction
{
    /// <summary>Initializes a new instance of the <see cref="SearchFunction"/> class.</summary>
    /// <param name="localName">The local name.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="localName"/> is <see langword="null"/>.</exception>
    /// <exception cref="System.ArgumentException"><paramref name="localName"/> is empty.</exception>
    protected SearchFunction(string localName)
        : base(localName, 2, 3, [OptionalString, OptionalString, OneString]) { }

    /// <inheritdoc />
    public sealed override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        CollationArgument.Check(invocation.Arguments, 2);

        Search(ref invocation, Text(invocation.Arguments[0]), Text(invocation.Arguments[1]));
        return XPathEvaluationStatus.Ok;
    }

    /// <summary>Looks for the value in the text and sets the result.</summary>
    /// <param name="invocation">The call.</param>
    /// <param name="text">The first argument.</param>
    /// <param name="value">The second argument.</param>
    protected abstract void Search(ref XPathFunctionCall invocation, string text, string value);
}
