namespace NetEvolve.Schemas.XPath.Functions.DateTime;

using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// A component extraction function of one <c>xs:dateTime</c>, <c>xs:date</c>, or <c>xs:time</c> argument that may be empty: the
/// empty sequence gives the empty sequence, and a value gives the component of its local value.
/// </summary>
internal abstract class DateTimeComponentFunction : DateTimeFunction
{
    /// <summary>Initializes a new instance of the <see cref="DateTimeComponentFunction"/> class.</summary>
    /// <param name="localName">The local name.</param>
    /// <param name="argumentType"><see cref="XPathItemType.DateTime"/>, <see cref="XPathItemType.Date"/>, or <see cref="XPathItemType.Time"/>.</param>
    /// <exception cref="System.ArgumentException"><paramref name="localName"/> is empty.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="argumentType"/> is not a defined type.</exception>
    protected DateTimeComponentFunction(string localName, XPathItemType argumentType)
        : base(localName, 1, 1, [new XPathParameterType(argumentType, Occurrence.ZeroOrOne)]) { }

    /// <inheritdoc />
    public sealed override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var argument = invocation.Arguments[0];
        if (!argument.IsEmpty)
        {
            _ = argument[0].TryGetDateTime(out var value);
            var component = Extract(in value);
            if (component.Kind != XdmItemKind.None)
            {
                invocation.Result = XdmSequence.Create(component);
            }
        }

        return XPathEvaluationStatus.Ok;
    }

    /// <summary>Reads the component.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The component; <see langword="default"/> for no component, which is the empty sequence.</returns>
    protected abstract XdmItem Extract(in XsdDateTime value);
}
