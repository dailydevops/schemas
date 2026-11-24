namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary>Curly array constructor, <c>array { expr }</c> (production CurlyArrayConstructor, XPath 3.1).</summary>
internal sealed class CurlyArrayConstructor : Expression
{
    /// <summary>Initializes a new instance of the <see cref="CurlyArrayConstructor"/> class.</summary>
    /// <param name="location">The source location.</param>
    /// <param name="content">Gets the content; <see langword="null"/> for <c>array { }</c>.</param>
    public CurlyArrayConstructor(SourceSpan location, Expression? content)
        : base(location) => Content = content;

    /// <summary>Gets the content; <see langword="null"/> for <c>array { }</c>.</summary>
    public Expression? Content { get; }

    /// <inheritdoc />
    public override IReadOnlyList<SyntaxNode> Children
    {
        get
        {
            List<SyntaxNode> children = [];
            if (Content is not null)
            {
                children.Add(Content);
            }

            return children.AsReadOnly();
        }
    }

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitCurlyArrayConstructor(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitCurlyArrayConstructor(this);
}
