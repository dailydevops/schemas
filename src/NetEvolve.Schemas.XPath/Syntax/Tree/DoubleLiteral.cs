namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary>Double literal (production DoubleLiteral).</summary>
internal sealed class DoubleLiteral : Expression
{
    /// <summary>Initializes a new instance of the <see cref="DoubleLiteral"/> class.</summary>
    /// <param name="location">The source location.</param>
    /// <param name="lexeme">Gets the literal text as written.</param>
    public DoubleLiteral(SourceSpan location, string lexeme)
        : base(location) => Lexeme = lexeme;

    /// <summary>Gets the literal text as written.</summary>
    public string Lexeme { get; }

    /// <inheritdoc />
    public override IReadOnlyList<SyntaxNode> Children => [];

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitDoubleLiteral(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitDoubleLiteral(this);
}
