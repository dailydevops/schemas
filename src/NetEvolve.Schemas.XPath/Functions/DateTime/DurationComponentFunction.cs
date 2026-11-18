namespace NetEvolve.Schemas.XPath.Functions.DateTime;

using NetEvolve.Schemas.Xml.Datatypes.Duration;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// A component extraction function of one <c>xs:duration</c> argument that may be empty: the empty sequence gives the empty
/// sequence, and a value gives one component of its canonical form.
/// </summary>
/// <remarks>The argument type is <c>xs:duration</c>, so <c>xs:yearMonthDuration</c> and <c>xs:dayTimeDuration</c> values are accepted as well; the components they lack are zero.</remarks>
internal abstract class DurationComponentFunction : DateTimeFunction
{
    /// <summary>Initializes a new instance of the <see cref="DurationComponentFunction"/> class.</summary>
    /// <param name="localName">The local name.</param>
    /// <exception cref="System.ArgumentException"><paramref name="localName"/> is empty.</exception>
    protected DurationComponentFunction(string localName)
        : base(localName, 1, 1, [new XPathParameterType(XPathItemType.Duration, Occurrence.ZeroOrOne)]) { }

    /// <inheritdoc />
    public sealed override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var argument = invocation.Arguments[0];
        if (!argument.IsEmpty)
        {
            _ = argument[0].TryGetDuration(out var value);
            invocation.Result = XdmSequence.Create(Extract(in value));
        }

        return XPathEvaluationStatus.Ok;
    }

    /// <summary>Reads the component.</summary>
    /// <param name="value">The duration.</param>
    /// <returns>The component.</returns>
    /// <exception cref="Errors.XPathException">The component does not fit the value space of the result (FOAR0002).</exception>
    protected abstract XdmItem Extract(in XsdDuration value);
}
