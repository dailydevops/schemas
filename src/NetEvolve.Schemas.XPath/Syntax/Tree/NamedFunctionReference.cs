namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary>Named function reference, <c>name#arity</c> (production NamedFunctionRef, XPath 3.0).</summary>
internal sealed class NamedFunctionReference : Expression
{
    /// <summary>Initializes a new instance of the <see cref="NamedFunctionReference"/> class.</summary>
    /// <param name="location">The source location.</param>
    /// <param name="name">Gets the name.</param>
    /// <param name="arity">Gets the arity.</param>
    public NamedFunctionReference(SourceSpan location, QualifiedName name, int arity)
        : base(location)
    {
        Name = name;
        Arity = arity;
    }

    /// <summary>Gets the name.</summary>
    public QualifiedName Name { get; }

    /// <summary>Gets the arity.</summary>
    public int Arity { get; }

    /// <inheritdoc />
    private protected override IReadOnlyList<SyntaxNode> CreateChildren() => [Name];

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitNamedFunctionReference(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitNamedFunctionReference(this);
}
