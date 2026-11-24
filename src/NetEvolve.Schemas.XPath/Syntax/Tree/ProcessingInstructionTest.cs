namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary><c>processing-instruction()</c> (production PITest).</summary>
internal sealed class ProcessingInstructionTest : KindTest
{
    /// <summary>Initializes a new instance of the <see cref="ProcessingInstructionTest"/> class.</summary>
    /// <param name="location">The source location.</param>
    /// <param name="target">Gets the target name or string literal value, or <see langword="null"/>.</param>
    public ProcessingInstructionTest(SourceSpan location, string? target)
        : base(location) => Target = target;

    /// <summary>Gets the target name or string literal value, or <see langword="null"/>.</summary>
    public string? Target { get; }

    /// <inheritdoc />
    private protected override IReadOnlyList<SyntaxNode> CreateChildren() => [];

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitProcessingInstructionTest(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitProcessingInstructionTest(this);
}
