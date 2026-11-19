namespace NetEvolve.Schemas.Schematron.Xslt.Functions;

/// <summary>
/// Implemented by a compiled instruction that compiles a sequence constructor of its own, such as the body of <c>xsl:if</c>: the
/// local variables of that body take slots above the variables visible at the instruction. A function saves and restores every
/// slot of its body when it is called, so the sequence constructor that holds the instruction reserves those slots (as slots that
/// no name can reach) and the later variables of the constructor take the slots above them.
/// </summary>
internal interface IXsltLocalSlots
{
    /// <summary>Gets the first slot above every slot the instruction uses.</summary>
    int SlotEnd { get; }
}
