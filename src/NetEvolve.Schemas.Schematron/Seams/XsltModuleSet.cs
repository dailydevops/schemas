namespace NetEvolve.Schemas.Schematron.Seams;

using System.Collections.Generic;

/// <summary>
/// The declarations of a principal module and of every module it includes or imports, in the order of the walk (an include is spliced
/// at its position, an import is placed before the level that imports it), each with its import precedence. The module set is the
/// output of <c>XsltModuleLinker</c> and the input of <c>XsltModuleCompiler</c>.
/// </summary>
internal sealed class XsltModuleSet
{
    internal XsltModuleSet(IReadOnlyList<XsltModuleDeclaration> declarations)
    {
        ArgumentNullException.ThrowIfNull(declarations);

        Declarations = declarations;
    }

    /// <summary>Gets the set without a declaration.</summary>
    internal static XsltModuleSet Empty { get; } = new([]);

    /// <summary>Gets the declarations in the order of the walk.</summary>
    internal IReadOnlyList<XsltModuleDeclaration> Declarations { get; }
}
