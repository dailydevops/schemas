namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary>One parameter of an inline function (production Param, XPath 3.0).</summary>
internal sealed class FunctionParameter : SyntaxNode
{
    /// <summary>Initializes a new instance of the <see cref="FunctionParameter"/> class.</summary>
    /// <param name="location">The source location.</param>
    /// <param name="name">Gets the name.</param>
    /// <param name="type">Gets the declared type, or <see langword="null"/> when there is none.</param>
    public FunctionParameter(SourceSpan location, QualifiedName name, SequenceType? type)
        : base(location)
    {
        Name = name;
        Type = type;
    }

    /// <summary>Gets the name.</summary>
    public QualifiedName Name { get; }

    /// <summary>Gets the declared type, or <see langword="null"/> when there is none.</summary>
    public SequenceType? Type { get; }

    /// <inheritdoc />
    public override IReadOnlyList<SyntaxNode> Children
    {
        get
        {
            List<SyntaxNode> children = [Name];
            if (Type is not null)
            {
                children.Add(Type);
            }

            return children.AsReadOnly();
        }
    }

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitFunctionParameter(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitFunctionParameter(this);
}
