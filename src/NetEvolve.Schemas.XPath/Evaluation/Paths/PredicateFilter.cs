namespace NetEvolve.Schemas.XPath.Evaluation.Paths;

using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;

/// <summary>Applies predicates to a list of items (XPath 2.0, 3.2.2): each predicate keeps the items it selects and the next one counts positions among those.</summary>
internal static class PredicateFilter
{
    /// <summary>Filters a list in place.</summary>
    /// <param name="context">The evaluation context; the focus is restored afterwards, also when a predicate raises an error.</param>
    /// <param name="predicates">The predicates, first to last.</param>
    /// <param name="items">The items in the order that positions count in; they keep that order.</param>
    /// <exception cref="XPathException">A predicate value has no effective boolean value (FORG0006), or a predicate raises an error.</exception>
    public static void Apply(XPathEvaluationContext context, ExecutableNode[] predicates, ref XdmItemList items)
    {
        Argument.ThrowIfNull(context);
        Argument.ThrowIfNull(predicates);

        var outer = context.Focus;
        try
        {
            for (var p = 0; p < predicates.Length && items.Count > 0; p++)
            {
                var size = items.Count;
                var span = items.AsSpan();
                var kept = 0;
                for (var i = 0; i < size; i++)
                {
                    var item = span[i];
                    _ = context.EnterFocus(item, i + 1, size);
                    if (PredicateTruth.Test(predicates[p].Evaluate(context), i + 1))
                    {
                        span[kept++] = item;
                    }
                }

                items.Truncate(kept);
            }
        }
        finally
        {
            context.RestoreFocus(outer);
        }
    }
}
