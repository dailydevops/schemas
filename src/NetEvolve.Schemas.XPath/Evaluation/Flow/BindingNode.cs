namespace NetEvolve.Schemas.XPath.Evaluation.Flow;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// Base of the <see langword="for"/> and quantified expressions: one range variable that takes each item of the source in turn, and a body that is
/// evaluated with the variable bound. An expression with several variables is a chain of these nodes, the next variable being bound
/// inside the body of the previous one, so a later source sees the earlier variables.
/// </summary>
/// <remarks>
/// <para>
/// The variable is bound in the variables of the evaluation context and the binding it shadows is put back when the loop ends, by
/// completion, by a decision, or by an error, so an outer variable of the same name, or none, is what the rest of the evaluation sees.
/// The focus is not changed.
/// </para>
/// <para>
/// A source that is a <see cref="RangeNode"/> is counted through without building a sequence. Any other source is evaluated once per
/// binding of the outer variables and read by index.
/// </para>
/// </remarks>
internal abstract class BindingNode : ExecutableNode
{
    private readonly XPathQName _name;
    private readonly ExecutableNode _source;

    /// <summary>Initializes a new instance of the <see cref="BindingNode"/> class.</summary>
    /// <param name="name">The expanded name of the range variable.</param>
    /// <param name="source">The expression whose items the variable takes.</param>
    /// <param name="body">The expression evaluated for each item.</param>
    /// <exception cref="System.ArgumentNullException">An expression is <see langword="null"/>.</exception>
    /// <exception cref="System.ArgumentException">The local name of <paramref name="name"/> is empty.</exception>
    protected BindingNode(XPathQName name, ExecutableNode source, ExecutableNode body)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(name.LocalName, nameof(name));
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(body);

        _name = name;
        _source = source;
        Body = body;
    }

    /// <summary>Gets the expression evaluated for each item.</summary>
    protected ExecutableNode Body { get; }

    /// <summary>Visits one binding: the variable is bound.</summary>
    /// <param name="context">The evaluation context.</param>
    /// <param name="state">The state of the loops.</param>
    /// <returns><see langword="false"/> to stop with the remaining items unread.</returns>
    protected abstract bool Visit(XPathEvaluationContext context, ref BindingState state);

    /// <summary>Binds the variable to each item of the source in turn and visits it.</summary>
    /// <param name="context">The evaluation context.</param>
    /// <param name="state">The state of the loops.</param>
    /// <returns><see langword="false"/> when a visit stopped the loop.</returns>
    protected bool Iterate(XPathEvaluationContext context, ref BindingState state)
    {
        var variables = context.Variables;
        var shadowed = variables.TryGet(_name, out var previous);
        try
        {
            var items = default(XdmSequence);
            if (_source is RangeNode range)
            {
                if (range.TryGetBounds(context, out var first, out var last, out items))
                {
                    for (var value = first; ; value++)
                    {
                        variables.Set(_name, XdmItem.FromInteger(value));
                        if (!Visit(context, ref state))
                        {
                            return false;
                        }

                        // The comparison comes before the increment, so a range that ends at long.MaxValue does not overflow.
                        if (value == last)
                        {
                            return true;
                        }
                    }
                }
            }
            else
            {
                items = _source.Evaluate(context);
            }

            var count = items.Count;
            for (var i = 0; i < count; i++)
            {
                variables.Set(_name, items[i]);
                if (!Visit(context, ref state))
                {
                    return false;
                }
            }

            return true;
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
