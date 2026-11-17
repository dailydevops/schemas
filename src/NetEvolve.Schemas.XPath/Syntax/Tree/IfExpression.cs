namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary>If expression (production IfExpr).</summary>
internal sealed class IfExpression : Expression
{
    /// <summary>Initializes a new instance of the <see cref="IfExpression"/> class.</summary>
    /// <param name="location">The source location.</param>
    /// <param name="condition">Gets the condition.</param>
    /// <param name="then">Gets the then.</param>
    /// <param name="else">Gets the else.</param>
    public IfExpression(SourceSpan location, Expression condition, Expression then, Expression @else)
        : base(location)
    {
        Condition = condition;
        Then = then;
        Else = @else;
    }

    /// <summary>Gets the condition.</summary>
    public Expression Condition { get; }

    /// <summary>Gets the then.</summary>
    public Expression Then { get; }

    /// <summary>Gets the else.</summary>
    public Expression Else { get; }

    /// <inheritdoc />
    public override IReadOnlyList<SyntaxNode> Children => [Condition, Then, Else];

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitIfExpression(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitIfExpression(this);
}
