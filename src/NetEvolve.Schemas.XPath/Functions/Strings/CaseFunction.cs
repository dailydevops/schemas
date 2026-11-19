namespace NetEvolve.Schemas.XPath.Functions.Strings;

using NetEvolve.Schemas.XPath.Api;

/// <summary>
/// The base of <c>fn:upper-case</c> and <c>fn:lower-case</c> (Functions and Operators 1.0, 7.4.8 and 7.4.9): the full case mapping of
/// <see cref="CaseMapping"/>, which may change the length of the string; the empty sequence gives the zero-length string.
/// </summary>
internal abstract class CaseFunction : TextFunction
{
    private readonly bool _upper;

    /// <summary>Initializes a new instance of the <see cref="CaseFunction"/> class.</summary>
    /// <param name="localName">The local name.</param>
    /// <param name="upper"><see langword="true"/> for the upper-case mapping.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="localName"/> is <see langword="null"/>.</exception>
    /// <exception cref="System.ArgumentException"><paramref name="localName"/> is empty.</exception>
    protected CaseFunction(string localName, bool upper)
        : base(localName, 1, 1, [OptionalString]) => _upper = upper;

    /// <inheritdoc />
    public sealed override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var text = Text(invocation.Arguments[0]);
        var limits = invocation.Context.Limits;
        Return(ref invocation, _upper ? CaseMapping.ToUpper(text, limits) : CaseMapping.ToLower(text, limits));
        return XPathEvaluationStatus.Ok;
    }
}
