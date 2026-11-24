namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary>Quantified expression (production QuantifiedExpr).</summary>
internal sealed class QuantifiedExpression : Expression
{
    /// <summary>Initializes a new instance of the <see cref="QuantifiedExpression"/> class.</summary>
    /// <param name="location">The source location.</param>
    /// <param name="quantifier">Gets the quantifier.</param>
    /// <param name="bindings">Gets the bindings.</param>
    /// <param name="satisfies">Gets the satisfies.</param>
    public QuantifiedExpression(
        SourceSpan location,
        Quantifier quantifier,
        IReadOnlyList<VariableBinding> bindings,
        Expression satisfies
    )
        : base(location)
    {
        Quantifier = quantifier;
        Bindings = NodeList.Copy(bindings);
        Satisfies = satisfies;
    }

    /// <summary>Gets the quantifier.</summary>
    public Quantifier Quantifier { get; }

    /// <summary>Gets the bindings.</summary>
    public IReadOnlyList<VariableBinding> Bindings { get; }

    /// <summary>Gets the satisfies.</summary>
    public Expression Satisfies { get; }

    /// <inheritdoc />
    private protected override IReadOnlyList<SyntaxNode> CreateChildren()
    {
        List<SyntaxNode> children = [];
        children.AddRange(Bindings);
        children.Add(Satisfies);
        return children.AsReadOnly();
    }

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitQuantifiedExpression(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitQuantifiedExpression(this);
}
