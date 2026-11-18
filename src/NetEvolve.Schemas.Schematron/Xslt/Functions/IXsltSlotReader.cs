namespace NetEvolve.Schemas.Schematron.Xslt.Functions;

using System;

/// <summary>
/// Implemented by a compiled instruction that holds expressions: the variable slots those expressions read directly. A function
/// binds the schema-level slots of its body once when it is entered (design #150 EP-5), so an instruction that nests expressions
/// and does not implement this interface hides the slots it reads from the function.
/// </summary>
internal interface IXsltSlotReader
{
    /// <summary>Gets the variable slots read directly; empty when it reads none.</summary>
    ReadOnlySpan<int> ReferencedSlots { get; }
}
