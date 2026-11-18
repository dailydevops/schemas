namespace NetEvolve.Schemas.Xsd.Validation.Core;

/// <summary>How the content of an element is assessed.</summary>
internal enum FrameContent
{
    /// <summary>The element is not assessed (no declaration was found); its text is ignored and its children are assessed lax.</summary>
    Unassessed = 0,

    /// <summary>The type is a simple type; character data is the value and children are an error.</summary>
    SimpleType = 1,

    /// <summary>The content type is empty.</summary>
    Empty = 2,

    /// <summary>The content type is a simple type of a complex type.</summary>
    SimpleContent = 3,

    /// <summary>The content type is element-only.</summary>
    ElementOnly = 4,

    /// <summary>The content type is mixed.</summary>
    Mixed = 5,
}
