namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary>Filter expression (production FilterExpr).</summary>
internal sealed class FilterExpression : Expression
{
    /// <summary>Initializes a new instance of the <see cref="FilterExpression"/> class.</summary>
    /// <param name="location">The source location.</param>
    /// <param name="primary">Gets the primary.</param>
    /// <param name="predicates">Gets the predicates (productions PredicateList and Predicate).</param>
    public FilterExpression(SourceSpan location, Expression primary, IReadOnlyList<Expression> predicates)
        : base(location)
    {
        Primary = primary;
        Predicates = NodeList.Copy(predicates);
    }

    /// <summary>Gets the primary.</summary>
    public Expression Primary { get; }

    /// <summary>Gets the predicates (productions PredicateList and Predicate).</summary>
    public IReadOnlyList<Expression> Predicates { get; }

    /// <inheritdoc />
    public override IReadOnlyList<SyntaxNode> Children
    {
        get
        {
            List<SyntaxNode> children = [];
            children.Add(Primary);
            children.AddRange(Predicates);
            return children.AsReadOnly();
        }
    }

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitFilterExpression(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitFilterExpression(this);
}
