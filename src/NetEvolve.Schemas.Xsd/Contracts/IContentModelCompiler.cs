namespace NetEvolve.Schemas.Xsd.Contracts;

using NetEvolve.Schemas.Xsd.Diagnostics;

/// <summary>Compiles a resolved model group or particle into a content model program.</summary>
internal interface IContentModelCompiler
{
    /// <summary>Compiles a model group or particle.</summary>
    /// <param name="group">The resolved model group or particle component.</param>
    /// <param name="reporter">The reporter for ceilings that the model exceeds.</param>
    /// <returns>The program.</returns>
    ICompiledContentModel Compile(IXsdComponent group, XsdIssueReporter reporter);
}
