namespace NetEvolve.Schemas.Schematron.Evaluation.Assertions;

using NetEvolve.Schemas.Schematron.Seams;

/// <summary>One part of a compiled message: text, a compiled expression, or an inline element with its children.</summary>
internal sealed class MessagePart
{
    internal MessagePart(
        MessagePartKind kind,
        DocumentLocation location,
        string? text = null,
        QueryExpression? expression = null,
        string? attribute = null,
        MessagePart[]? children = null
    )
    {
        Kind = kind;
        Location = location;
        Text = text ?? string.Empty;
        Expression = expression;
        Attribute = attribute;
        Children = children ?? [];
    }

    internal MessagePartKind Kind { get; }

    internal DocumentLocation Location { get; }

    /// <summary>Gets the characters of a <see cref="MessagePartKind.Text"/> part, empty for every other kind.</summary>
    internal string Text { get; }

    /// <summary>Gets the <c>@select</c> of a <see cref="MessagePartKind.ValueOf"/>, or the <c>@path</c> of a <see cref="MessagePartKind.Name"/>; <see langword="null"/> for the context node.</summary>
    internal QueryExpression? Expression { get; }

    /// <summary>Gets the <c>@value</c> of a <see cref="MessagePartKind.Direction"/> or the <c>@class</c> of a <see cref="MessagePartKind.Span"/>, as written.</summary>
    internal string? Attribute { get; }

    internal MessagePart[] Children { get; }
}
