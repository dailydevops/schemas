namespace NetEvolve.Schemas.XPath.Evaluation.Flow30;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The executable form of a <see langword="let"/> expression with one binding (XPath 3.0, 3.12): the source is evaluated once, its whole
/// value is bound to the variable, and the return expression is evaluated with the variable in scope. The binding it shadows is put
/// back afterwards, by completion or by an error. The focus is not changed.
/// </summary>
internal sealed class LetNode : ExecutableNode
{
    private readonly XPathQName _name;
    private readonly ExecutableNode _source;
    private readonly ExecutableNode _body;

    /// <summary>Initializes a new instance of the <see cref="LetNode"/> class.</summary>
    /// <param name="name">The expanded name of the variable.</param>
    /// <param name="source">The expression whose value the variable takes.</param>
    /// <param name="body">The return expression.</param>
    /// <exception cref="System.ArgumentNullException">An expression is <see langword="null"/>.</exception>
    /// <exception cref="System.ArgumentException">The local name of <paramref name="name"/> is empty.</exception>
    public LetNode(XPathQName name, ExecutableNode source, ExecutableNode body)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(name.LocalName, nameof(name));
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(body);

        _name = name;
        _source = source;
        _body = body;
    }

    /// <inheritdoc />
    public override XdmSequence Evaluate(XPathEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var value = _source.Evaluate(context);
        var variables = context.Variables;
        var shadowed = variables.TryGet(_name, out var previous);
        variables.Set(_name, value);
        try
        {
            return _body.Evaluate(context);
        }
        finally
        {
            if (shadowed)
            {
                variables.Set(_name, previous);
            }
            else
            {
                variables.Remove(_name);
            }
        }
    }
}
