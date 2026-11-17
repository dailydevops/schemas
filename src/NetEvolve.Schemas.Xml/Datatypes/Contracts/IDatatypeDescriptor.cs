namespace NetEvolve.Schemas.Xml.Datatypes.Contracts;

using System;
using System.Collections.Generic;

/// <summary>
/// The version-independent view of a datatype: its name, variety, base type, lexical mode, and facets, and validation of a
/// lexical form. The registry and the built-in rows are DT-02; this contract only fixes the signatures.
/// </summary>
/// <remarks>Limitation: the lexical mode selects the 1.0 or 1.1 rules per descriptor instance; there is no global version.</remarks>
public interface IDatatypeDescriptor
{
    /// <summary>Gets the name of the type.</summary>
    string Name { get; }

    /// <summary>Gets the variety.</summary>
    DatatypeVariety Variety { get; }

    /// <summary>Gets the base type, <see langword="null"/> for the root of the hierarchy.</summary>
    IDatatypeDescriptor? BaseType { get; }

    /// <summary>Gets the XSD version whose lexical and value-space rules this descriptor applies.</summary>
    XsdVersion LexicalMode { get; }

    /// <summary>Gets a value indicating whether values of the type are ordered; <c>Compare</c> yields <see cref="XsdOrder.Incomparable"/> otherwise.</summary>
    bool IsOrdered { get; }

    /// <summary>Gets the facets that constrain the type.</summary>
    IReadOnlyList<IFacet> Facets { get; }

    /// <summary>Validates a lexical form against the type and its facets without producing a value.</summary>
    /// <param name="lexical">The lexical form.</param>
    /// <returns>The result.</returns>
    ValidationResult Validate(ReadOnlySpan<char> lexical);
}
