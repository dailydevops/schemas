namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary>Inline function expression (production InlineFunctionExpr, XPath 3.0).</summary>
internal sealed class InlineFunctionExpression : Expression
{
    /// <summary>Initializes a new instance of the <see cref="InlineFunctionExpression"/> class.</summary>
    /// <param name="location">The source location.</param>
    /// <param name="parameters">Gets the parameters.</param>
    /// <param name="returnType">Gets the declared return type, or <see langword="null"/> when there is none.</param>
    /// <param name="body">Gets the body.</param>
    public InlineFunctionExpression(
        SourceSpan location,
        IReadOnlyList<FunctionParameter> parameters,
        SequenceType? returnType,
        Expression body
    )
        : base(location)
    {
        Parameters = NodeList.Copy(parameters);
        ReturnType = returnType;
        Body = body;
    }

    /// <summary>Gets the parameters.</summary>
    public IReadOnlyList<FunctionParameter> Parameters { get; }

    /// <summary>Gets the declared return type, or <see langword="null"/> when there is none.</summary>
    public SequenceType? ReturnType { get; }

    /// <summary>Gets the body.</summary>
    public Expression Body { get; }

    /// <inheritdoc />
    public override IReadOnlyList<SyntaxNode> Children
    {
        get
        {
            List<SyntaxNode> children = [];
            children.AddRange(Parameters);
            if (ReturnType is not null)
            {
                children.Add(ReturnType);
            }

            children.Add(Body);
            return children.AsReadOnly();
        }
    }

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitInlineFunctionExpression(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitInlineFunctionExpression(this);
}
