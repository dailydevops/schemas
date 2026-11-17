namespace NetEvolve.Schemas.Schematron.Seams;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>What an XSLT instruction sees while it compiles.</summary>
internal sealed class XsltCompileContext
{
    internal XsltCompileContext(
        XPathNode element,
        QueryStaticScope scope,
        Func<XPathNode, XsltExecutable?> compileChild,
        IProblemSink problems
    )
    {
        Argument.ThrowIfNull(scope);
        Argument.ThrowIfNull(compileChild);
        Argument.ThrowIfNull(problems);

        Element = element;
        Scope = scope;
        CompileChild = compileChild;
        Problems = problems;
    }

    internal XPathNode Element { get; }

    internal QueryStaticScope Scope { get; }

    /// <summary>Compiles a child element; <see langword="null"/> when a problem was reported.</summary>
    internal Func<XPathNode, XsltExecutable?> CompileChild { get; }

    internal IProblemSink Problems { get; }
}
