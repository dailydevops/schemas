namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary>Dynamic function call, an argument list applied to a primary expression (production PostfixExpr, XPath 3.0).</summary>
internal sealed class DynamicFunctionCall : Expression
{
    /// <summary>Initializes a new instance of the <see cref="DynamicFunctionCall"/> class.</summary>
    /// <param name="location">The source location.</param>
    /// <param name="function">Gets the function.</param>
    /// <param name="arguments">Gets the arguments; an argument may be an <see cref="ArgumentPlaceholder"/>.</param>
    public DynamicFunctionCall(SourceSpan location, Expression function, IReadOnlyList<Expression> arguments)
        : base(location)
    {
        Function = function;
        Arguments = NodeList.Copy(arguments);
    }

    /// <summary>Gets the function.</summary>
    public Expression Function { get; }

    /// <summary>Gets the arguments; an argument may be an <see cref="ArgumentPlaceholder"/>.</summary>
    public IReadOnlyList<Expression> Arguments { get; }

    /// <inheritdoc />
    private protected override IReadOnlyList<SyntaxNode> CreateChildren()
    {
        List<SyntaxNode> children = [Function];
        children.AddRange(Arguments);
        return children.AsReadOnly();
    }

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitDynamicFunctionCall(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitDynamicFunctionCall(this);
}
