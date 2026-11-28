namespace NetEvolve.Schemas.Schematron.Seams;

using System;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>What an XSLT instruction sees while it compiles.</summary>
internal sealed class XsltCompileContext
{
    private readonly Func<XPathNode, QueryStaticScope, XsltExecutable?>? _compileChildIn;

    internal XsltCompileContext(
        XPathNode element,
        QueryStaticScope scope,
        Func<XPathNode, XsltExecutable?> compileChild,
        IProblemSink problems,
        IQueryBinding? binding = null,
        Func<XPathNode, QueryStaticScope, XsltExecutable?>? compileChildIn = null
    )
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(compileChild);
        ArgumentNullException.ThrowIfNull(problems);

        Element = element;
        Scope = scope;
        CompileChild = compileChild;
        Problems = problems;
        Binding = binding;
        _compileChildIn = compileChildIn;
    }

    internal XPathNode Element { get; }

    internal QueryStaticScope Scope { get; }

    /// <summary>Compiles a child element; <see langword="null"/> when a problem was reported.</summary>
    internal Func<XPathNode, XsltExecutable?> CompileChild { get; }

    internal IProblemSink Problems { get; }

    /// <summary>
    /// Gets the binding that compiles the expressions of an instruction, or <see langword="null"/> when the compiler gave none; an
    /// instruction with an expression needs it. Design #150 EP-12 carries it on the scope; until the scope has it, the context does.
    /// </summary>
    internal IQueryBinding? Binding { get; }

    /// <summary>
    /// Compiles a child element in a scope that the parent extended, for example with the parameters of a function or a local
    /// variable that the later siblings see; <see langword="null"/> when a problem was reported.
    /// </summary>
    /// <param name="child">The child element.</param>
    /// <param name="scope">The scope of the child.</param>
    /// <returns>The executable, or <see langword="null"/>.</returns>
    /// <exception cref="InvalidOperationException">The compiler gave no callback for a scoped child.</exception>
    internal XsltExecutable? CompileChildIn(XPathNode child, QueryStaticScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);

        return (
            _compileChildIn
            ?? throw new InvalidOperationException(
                "The compiler of the context cannot compile a child in another scope."
            )
        )(child, scope);
    }
}
