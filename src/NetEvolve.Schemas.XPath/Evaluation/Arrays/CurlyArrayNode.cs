namespace NetEvolve.Schemas.XPath.Evaluation.Arrays;

using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Xdm.Arrays;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The executable form of a curly array constructor (XPath 3.1, 3.11.2.2): each item of the content is one member.</summary>
internal sealed class CurlyArrayNode : ExecutableNode
{
    private readonly ExecutableNode? _content;

    /// <summary>Initializes a new instance of the <see cref="CurlyArrayNode"/> class.</summary>
    /// <param name="content">The content expression; <see langword="null"/> for <c>array {}</c>.</param>
    public CurlyArrayNode(ExecutableNode? content) => _content = content;

    /// <inheritdoc />
    public override XdmSequence Evaluate(XPathEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (_content is null)
        {
            return XdmSequence.Create(XdmArray.Empty.ToItem());
        }

        var content = _content.Evaluate(context);
        var members = new XdmSequence[content.Count];
        for (var i = 0; i < members.Length; i++)
        {
            members[i] = XdmSequence.Create(content[i]);
        }

        return XdmSequence.Create(XdmArray.Create(members).ToItem());
    }
}
