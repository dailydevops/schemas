namespace NetEvolve.Schemas.XPath.Functions.DateTime;

using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Functions.Binding;

/// <summary>The base of the functions of the date and time group: every one is a built-in function of the <c>fn</c> namespace.</summary>
/// <remarks>
/// Limitation: the group follows XPath 2.0 and its Functions and Operators 1.0; the lexical forms and the calendar are those of
/// the datatypes of the Xml package, so the year 0 of XSD 1.1 is accepted where XSD 1.0 has none (see <c>DateTimeTimeline</c>).
/// </remarks>
internal abstract class DateTimeFunction : XPathFunction
{
    /// <summary>The namespace of the built-in functions.</summary>
    internal const string FunctionNamespace = "http://www.w3.org/2005/xpath-functions";

    /// <summary>Initializes a new instance of the <see cref="DateTimeFunction"/> class.</summary>
    /// <param name="localName">The local name.</param>
    /// <param name="minArity">The minimum number of arguments.</param>
    /// <param name="maxArity">The maximum number of arguments.</param>
    /// <param name="parameters">The declared parameter types.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="localName"/> or <paramref name="parameters"/> is <see langword="null"/>.</exception>
    /// <exception cref="System.ArgumentException"><paramref name="localName"/> is empty.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">The arity range is invalid, or the number of parameter types lies outside it.</exception>
    protected DateTimeFunction(
        string localName,
        int minArity,
        int maxArity,
        IReadOnlyList<XPathParameterType> parameters
    )
        : base(new XPathQName(FunctionNamespace, localName), minArity, maxArity, parameters) { }
}
