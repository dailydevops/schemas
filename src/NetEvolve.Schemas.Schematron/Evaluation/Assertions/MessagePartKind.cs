namespace NetEvolve.Schemas.Schematron.Evaluation.Assertions;

/// <summary>The kind of one part of a compiled message.</summary>
internal enum MessagePartKind : byte
{
    /// <summary>Character data, kept as written.</summary>
    Text = 0,

    /// <summary>A <c>value-of</c>: the string value of its <see langword="select"/> expression.</summary>
    ValueOf = 1,

    /// <summary>A <c>name</c>: the name of the context node, or of the node its <c>path</c> selects.</summary>
    Name = 2,

    /// <summary>An <c>emph</c>.</summary>
    Emphasis = 3,

    /// <summary>A <c>dir</c>.</summary>
    Direction = 4,

    /// <summary>A <c>span</c>.</summary>
    Span = 5,
}
