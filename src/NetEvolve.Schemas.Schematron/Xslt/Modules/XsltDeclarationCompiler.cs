namespace NetEvolve.Schemas.Schematron.Xslt.Modules;

using NetEvolve.Schemas.Schematron.Seams;

/// <summary>
/// Compiles one declaration of a module with the instruction that the registry resolved for it. The compiler of the schema owns the
/// scope, the binding, and the callbacks of the compile context, so it supplies this step; a problem goes to <paramref name="problems"/>,
/// which locates it in the module, and <see langword="null"/> is returned when the declaration did not compile.
/// </summary>
/// <param name="declaration">The placement of the declaration.</param>
/// <param name="instruction">The registered instruction of the element.</param>
/// <param name="problems">The sink of the compile problems of the declaration.</param>
/// <returns>The executable, or <see langword="null"/>.</returns>
internal delegate XsltExecutable? XsltDeclarationCompiler(
    XsltModuleDeclaration declaration,
    IXsltInstruction instruction,
    IProblemSink problems
);
