namespace NetEvolve.Schemas.Xsd.Validation.Annotations;

/// <summary>What the typed value of an annotated node is (XDM, typed-value accessor); the count of items alone is ambiguous for zero.</summary>
internal enum XsdTypedValueKind
{
    /// <summary>The node has no typed value: its type has element-only content (an XDM error when atomized).</summary>
    None = 0,

    /// <summary>The typed value is the empty sequence: the element is nilled or its content type is empty.</summary>
    Empty = 1,

    /// <summary>The typed value is the lexical mapping of a simple type: one item for an atomic type, any number for a list type.</summary>
    Atomic = 2,

    /// <summary>The typed value is one <c>xs:untypedAtomic</c> item that holds the string value.</summary>
    Untyped = 3,
}
