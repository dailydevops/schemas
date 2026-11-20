namespace NetEvolve.Schemas.Xsd.Validation.Xsi;

/// <summary>The outcome of resolving the <c>xsi:type</c> attribute of an element.</summary>
internal enum XsiTypeStatus
{
    /// <summary>The value names a type that may stand in for the declared type.</summary>
    Resolved = 0,

    /// <summary>The value is not a QName (<c>cvc-elt.4.1</c>).</summary>
    InvalidQName = 1,

    /// <summary>The prefix of the QName is not bound to a namespace (<c>cvc-elt.4.1</c>).</summary>
    UnboundPrefix = 2,

    /// <summary>No type of that name is in the schema set (<c>cvc-elt.4.2</c>).</summary>
    UnknownType = 3,

    /// <summary>The type is not validly derived from the declared type, or the derivation is blocked (<c>cvc-elt.4.3</c>).</summary>
    NotDerived = 4,
}
