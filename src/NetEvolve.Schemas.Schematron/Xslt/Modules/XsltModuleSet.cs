namespace NetEvolve.Schemas.Schematron.Xslt.Modules;

using System.Collections.Generic;
using NetEvolve.Arguments;

/// <summary>
/// The declarations of a principal module and of every module it includes or imports, in the order of the walk (an include is spliced
/// at its position, an import is placed before the level that imports it), each with its import precedence. The module set is the
/// output of <see cref="XsltModuleLinker"/> and the input of <see cref="XsltModuleCompiler"/>.
/// </summary>
internal sealed class XsltModuleSet
{
    internal XsltModuleSet(IReadOnlyList<XsltModuleDeclaration> declarations)
    {
        Argument.ThrowIfNull(declarations);

        Declarations = declarations;
    }

    /// <summary>Gets the set without a declaration.</summary>
    internal static XsltModuleSet Empty { get; } = new([]);

    /// <summary>Gets the declarations in the order of the walk.</summary>
    internal IReadOnlyList<XsltModuleDeclaration> Declarations { get; }
}
