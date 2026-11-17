namespace NetEvolve.Schemas.Schematron.Seams;

/// <summary>What an expression is used for.</summary>
internal enum QueryExpressionKind : byte
{
    /// <summary>A <c>test</c>.</summary>
    Boolean = 0,

    /// <summary>A <see langword="select"/>, <c>path</c>, <see langword="let"/> value, or dynamic attribute.</summary>
    Value = 1,

    /// <summary>A <c>subject</c>.</summary>
    Nodes = 2,
}
