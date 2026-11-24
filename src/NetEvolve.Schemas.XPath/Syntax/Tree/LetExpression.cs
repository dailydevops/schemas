namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary>Let expression (production LetExpr, XPath 3.0).</summary>
internal sealed class LetExpression : Expression
{
    /// <summary>Initializes a new instance of the <see cref="LetExpression"/> class.</summary>
    /// <param name="location">The source location.</param>
    /// <param name="bindings">Gets the bindings; each source is the value bound by <c>:=</c>.</param>
    /// <param name="return">Gets the return.</param>
    public LetExpression(SourceSpan location, IReadOnlyList<VariableBinding> bindings, Expression @return)
        : base(location)
    {
        Bindings = NodeList.Copy(bindings);
        Return = @return;
    }

    /// <summary>Gets the bindings; each source is the value bound by <c>:=</c>.</summary>
    public IReadOnlyList<VariableBinding> Bindings { get; }

    /// <summary>Gets the return.</summary>
    public Expression Return { get; }

    /// <inheritdoc />
    private protected override IReadOnlyList<SyntaxNode> CreateChildren()
    {
        List<SyntaxNode> children = [];
        children.AddRange(Bindings);
        children.Add(Return);
        return children.AsReadOnly();
    }

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitLetExpression(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitLetExpression(this);
}
