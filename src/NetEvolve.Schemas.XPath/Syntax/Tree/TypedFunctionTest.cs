namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary><c>function(types) as type</c> (production TypedFunctionTest, XPath 3.0).</summary>
internal sealed class TypedFunctionTest : ItemType
{
    /// <summary>Initializes a new instance of the <see cref="TypedFunctionTest"/> class.</summary>
    /// <param name="location">The source location.</param>
    /// <param name="parameterTypes">Gets the parameter types.</param>
    /// <param name="returnType">Gets the return type.</param>
    public TypedFunctionTest(SourceSpan location, IReadOnlyList<SequenceType> parameterTypes, SequenceType returnType)
        : base(location)
    {
        ParameterTypes = NodeList.Copy(parameterTypes);
        ReturnType = returnType;
    }

    /// <summary>Gets the parameter types.</summary>
    public IReadOnlyList<SequenceType> ParameterTypes { get; }

    /// <summary>Gets the return type.</summary>
    public SequenceType ReturnType { get; }

    /// <inheritdoc />
    public override IReadOnlyList<SyntaxNode> Children
    {
        get
        {
            List<SyntaxNode> children = [];
            children.AddRange(ParameterTypes);
            children.Add(ReturnType);
            return children.AsReadOnly();
        }
    }

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitTypedFunctionTest(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitTypedFunctionTest(this);
}
