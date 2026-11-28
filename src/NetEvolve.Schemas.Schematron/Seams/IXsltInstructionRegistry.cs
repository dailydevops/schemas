namespace NetEvolve.Schemas.Schematron.Seams;

using NetEvolve.Schemas.XPath.Api;

/// <summary>The registry of XSLT instructions, keyed by expanded name.</summary>
internal interface IXsltInstructionRegistry
{
    /// <summary>Classifies an element name for the position the compiler is filling; one lookup, one answer.</summary>
    /// <param name="name">The expanded name of the element.</param>
    /// <param name="where">The position the compiler is filling.</param>
    /// <param name="instruction">The entry for a registered name, also when it is misplaced.</param>
    XsltElementClass Classify(in XPathQName name, XsltPosition where, out IXsltInstruction instruction);

    /// <summary>Finds the entry of a name in any position.</summary>
    bool TryGet(in XPathQName name, out IXsltInstruction instruction);

    /// <summary>Finds the entry of a construct that has no XSLT name.</summary>
    bool TryGetSynthetic(XsltSyntheticElement kind, out IXsltInstruction instruction);

    /// <summary>Whether an instruction is registered; <c>element-available</c> asks this.</summary>
    bool IsAvailable(in XPathQName name);
}
