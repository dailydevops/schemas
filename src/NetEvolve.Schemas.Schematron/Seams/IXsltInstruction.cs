namespace NetEvolve.Schemas.Schematron.Seams;

using NetEvolve.Schemas.XPath.Api;

/// <summary>An XSLT instruction that can be compiled.</summary>
internal interface IXsltInstruction
{
    XPathQName Name { get; }

    /// <summary>Gets the positions where the element may appear.</summary>
    XsltPosition Position { get; }

    /// <summary>Compiles the instruction. Cold path.</summary>
    bool TryCompile(XsltCompileContext context, out XsltExecutable executable, out SchematronProblem problem);
}
