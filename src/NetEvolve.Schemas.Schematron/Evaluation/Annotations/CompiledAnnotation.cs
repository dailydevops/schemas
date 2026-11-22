namespace NetEvolve.Schemas.Schematron.Evaluation.Annotations;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>
/// One <c>role</c>, <c>flag</c>, or <c>severity</c> attribute compiled once: either a static text, copied as written (the tokens of
/// a <c>flag</c> already joined with one space), or a dynamic value, a whole-value variable reference that is read when the assertion
/// fires. Immutable and shared by every run.
/// </summary>
internal sealed class CompiledAnnotation
{
    private CompiledAnnotation(
        AnnotationKind kind,
        string? text,
        string? source,
        QueryExpression? expression,
        DocumentLocation location
    )
    {
        Kind = kind;
        Text = text;
        Source = source;
        Expression = expression;
        Location = location;
    }

    /// <summary>Gets the attribute.</summary>
    internal AnnotationKind Kind { get; }

    /// <summary>Gets the static text, <see langword="null"/> for a dynamic value.</summary>
    internal string? Text { get; }

    /// <summary>Gets the variable reference as written, for example <c>$level</c>; <see langword="null"/> for a static value.</summary>
    internal string? Source { get; }

    /// <summary>Gets the compiled variable reference, <see langword="null"/> for a static value.</summary>
    internal QueryExpression? Expression { get; }

    /// <summary>Gets the location of the element that carries the attribute.</summary>
    internal DocumentLocation Location { get; }

    /// <summary>Creates a static attribute.</summary>
    /// <param name="kind">The attribute.</param>
    /// <param name="text">The text copied to the finding.</param>
    /// <param name="location">The location of the element that carries the attribute.</param>
    /// <returns>The attribute.</returns>
    internal static CompiledAnnotation CreateStatic(AnnotationKind kind, string text, DocumentLocation location)
    {
        ArgumentNullException.ThrowIfNull(text);

        return new CompiledAnnotation(kind, text, null, null, location);
    }

    /// <summary>Creates a dynamic attribute.</summary>
    /// <param name="kind">The attribute.</param>
    /// <param name="source">The variable reference as written.</param>
    /// <param name="expression">The variable reference compiled in the scope where the attribute is written.</param>
    /// <param name="location">The location of the element that carries the attribute.</param>
    /// <returns>The attribute.</returns>
    internal static CompiledAnnotation CreateDynamic(
        AnnotationKind kind,
        string source,
        QueryExpression expression,
        DocumentLocation location
    )
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(expression);

        return new CompiledAnnotation(kind, null, source, expression, location);
    }
}
