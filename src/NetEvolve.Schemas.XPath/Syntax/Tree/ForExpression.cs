namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary>For expression (production ForExpr).</summary>
internal sealed class ForExpression : Expression
{
    /// <summary>Initializes a new instance of the <see cref="ForExpression"/> class.</summary>
    /// <param name="location">The source location.</param>
    /// <param name="bindings">Gets the bindings.</param>
    /// <param name="return">Gets the return.</param>
    public ForExpression(SourceSpan location, IReadOnlyList<VariableBinding> bindings, Expression @return)
        : base(location)
    {
        Bindings = NodeList.Copy(bindings);
        Return = @return;
    }

    /// <summary>Gets the bindings.</summary>
    public IReadOnlyList<VariableBinding> Bindings { get; }

    /// <summary>Gets the return.</summary>
    public Expression Return { get; }

    /// <inheritdoc />
    public override IReadOnlyList<SyntaxNode> Children
    {
        get
        {
            List<SyntaxNode> children = [];
            children.AddRange(Bindings);
            children.Add(Return);
            return children.AsReadOnly();
        }
    }

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitForExpression(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitForExpression(this);
}
