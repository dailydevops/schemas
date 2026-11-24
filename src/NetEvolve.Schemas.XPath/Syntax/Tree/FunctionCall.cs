namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary>Function call (production FunctionCall).</summary>
internal sealed class FunctionCall : Expression
{
    /// <summary>Initializes a new instance of the <see cref="FunctionCall"/> class.</summary>
    /// <param name="location">The source location.</param>
    /// <param name="name">Gets the name.</param>
    /// <param name="arguments">Gets the arguments.</param>
    public FunctionCall(SourceSpan location, QualifiedName name, IReadOnlyList<Expression> arguments)
        : base(location)
    {
        Name = name;
        Arguments = NodeList.Copy(arguments);
    }

    /// <summary>Gets the name.</summary>
    public QualifiedName Name { get; }

    /// <summary>Gets the arguments.</summary>
    public IReadOnlyList<Expression> Arguments { get; }

    /// <inheritdoc />
    private protected override IReadOnlyList<SyntaxNode> CreateChildren()
    {
        List<SyntaxNode> children = [];
        children.Add(Name);
        children.AddRange(Arguments);
        return children.AsReadOnly();
    }

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitFunctionCall(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitFunctionCall(this);
}
