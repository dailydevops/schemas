namespace NetEvolve.Schemas.Xsd.Attributes.Declarations;

/// <summary>The form of a local attribute declaration (Structures section 3.2.2).</summary>
internal enum AttributeForm
{
    /// <summary>The name is in the target namespace.</summary>
    Qualified = 0,

    /// <summary>The name is in no namespace.</summary>
    Unqualified = 1,
}
