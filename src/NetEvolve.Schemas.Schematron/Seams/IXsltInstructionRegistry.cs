namespace NetEvolve.Schemas.Schematron.Seams;

using NetEvolve.Schemas.XPath.Api;

/// <summary>The registry of XSLT instructions, keyed by expanded name.</summary>
internal interface IXsltInstructionRegistry
{
    bool TryGet(in XPathQName name, out IXsltInstruction instruction);

    /// <summary>Whether an instruction is registered; the future <c>element-available</c> asks this.</summary>
    bool IsAvailable(in XPathQName name);
}
