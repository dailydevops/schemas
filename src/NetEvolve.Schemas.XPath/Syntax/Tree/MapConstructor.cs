namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary>Map constructor, <c>map { k : v, ... }</c> (production MapConstructor, XPath 3.1).</summary>
internal sealed class MapConstructor : Expression
{
    /// <summary>Initializes a new instance of the <see cref="MapConstructor"/> class.</summary>
    /// <param name="location">The source location.</param>
    /// <param name="entries">Gets the entries.</param>
    public MapConstructor(SourceSpan location, IReadOnlyList<MapEntry> entries)
        : base(location) => Entries = NodeList.Copy(entries);

    /// <summary>Gets the entries.</summary>
    public IReadOnlyList<MapEntry> Entries { get; }

    /// <inheritdoc />
    public override IReadOnlyList<SyntaxNode> Children
    {
        get
        {
            List<SyntaxNode> children = [];
            children.AddRange(Entries);
            return children.AsReadOnly();
        }
    }

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitMapConstructor(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitMapConstructor(this);
}
