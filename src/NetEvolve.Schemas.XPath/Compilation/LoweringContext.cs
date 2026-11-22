namespace NetEvolve.Schemas.XPath.Compilation;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Limits;
using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>
/// The state of one lowering pass: the static context, the lowerings, and the variables in scope. Name resolution and
/// the static errors XPST0008, XPST0017, and XPST0081 live here, so a lowering reports them with the node location.
/// Not thread-safe; one instance per compilation.
/// </summary>
/// <remarks>
/// Limitation: function lookup covers host functions of the static context; the built-in library and the conversion
/// rules arrive with XP-53. Static typing is not performed (DEF-41). The lowering recurses once per syntax level, so it counts
/// the levels and stops at <see cref="XPathLimits.MaxRecursionDepth"/>; the parser does not bound the depth of a left-deep chain
/// such as <c>1+1+1</c>.
/// </remarks>
internal sealed class LoweringContext
{
    // Engine error for a syntax node kind without a lowering; the engine range is XP0xxx.
    internal static readonly XPathErrorCode NoLowering = new(XPathLimitErrors.EngineNamespace, "XP0002");

    private readonly LoweringRegistry _registry;
    private readonly XPathLimits _limits;
    private readonly List<XPathQName> _scope = [];
    private int _depth;

    internal LoweringContext(XPathStaticContext staticContext, LoweringRegistry registry, XPathLimits? limits = null)
    {
        StaticContext = staticContext;
        _registry = registry;
        _limits = limits ?? XPathLimits.Default;
    }

    /// <summary>Gets the deepest level the lowering reached, the root being one.</summary>
    public int MaxDepth { get; private set; }

    /// <summary>Gets the static context.</summary>
    public XPathStaticContext StaticContext { get; }

    /// <summary>Lowers a node through the lowering registered for its kind.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The executable node.</returns>
    /// <exception cref="XPathException">No lowering is registered for the node kind, a static error is found, or the nesting is deeper than the recursion depth limit.</exception>
    public ExecutableNode Lower(SyntaxNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        // A failure ends the compilation, so the level is not restored on the way out.
        _limits.ThrowIfTooDeep(++_depth, node.Location);
        if (_depth > MaxDepth)
        {
            MaxDepth = _depth;
        }

        if (!_registry.TryGet(node.GetType(), out var lowering))
        {
            throw new XPathException(
                new XPathDiagnostic(
                    NoLowering,
                    XPathErrorKind.Engine,
                    $"No lowering is registered for the syntax node kind '{node.GetType().Name}'.",
                    node.Location.Offset,
                    node.Location.Length
                )
            );
        }

        var lowered = lowering!(node, this);
        _depth--;
        return lowered;
    }

    /// <summary>Resolves a variable name to an expanded name; it must be declared in the static context or in scope.</summary>
    /// <param name="name">The name.</param>
    /// <returns>The expanded name.</returns>
    /// <exception cref="XPathException">The prefix is undeclared (XPST0081) or the variable is not defined (XPST0008).</exception>
    public XPathQName ResolveVariable(QualifiedName name)
    {
        ArgumentNullException.ThrowIfNull(name);

        var qname = Expand(name, string.Empty);
        if (_scope.Contains(qname) || StaticContext.TryGetVariableType(qname, out _))
        {
            return qname;
        }

        throw XPathErrors.Exception(XPathErrorCodes.XPST0008, name.Location.Offset, name.Location.Length);
    }

    /// <summary>Resolves a function name and arity against the host functions of the static context.</summary>
    /// <param name="name">The function name.</param>
    /// <param name="arity">The number of arguments.</param>
    /// <param name="location">The location reported for XPST0017, usually the whole call.</param>
    /// <returns>The function.</returns>
    /// <exception cref="XPathException">The prefix is undeclared (XPST0081) or no function matches (XPST0017).</exception>
    public XPathHostFunction ResolveFunction(QualifiedName name, int arity, SourceSpan location)
    {
        ArgumentNullException.ThrowIfNull(name);

        var qname = Expand(name, StaticContext.DefaultFunctionNamespace);
        if (StaticContext.TryResolveFunction(qname, arity, out var function))
        {
            return function!;
        }

        throw XPathErrors.Exception(XPathErrorCodes.XPST0017, location.Offset, location.Length);
    }

    /// <summary>Declares a variable for the lifetime of the returned scope, as a binding of a for, some, or every expression does.</summary>
    /// <param name="name">The variable name.</param>
    /// <returns>The scope; dispose it to end the declaration.</returns>
    /// <exception cref="XPathException">The prefix is undeclared (XPST0081).</exception>
    public VariableScope DeclareVariable(QualifiedName name)
    {
        ArgumentNullException.ThrowIfNull(name);

        _scope.Add(Expand(name, string.Empty));
        return new VariableScope(this);
    }

    private XPathQName Expand(QualifiedName name, string unprefixedNamespace)
    {
        if (name.Prefix is null)
        {
            return new XPathQName(unprefixedNamespace, name.LocalName);
        }

        if (!StaticContext.TryResolveNamespace(name.Prefix, out var namespaceUri))
        {
            throw XPathErrors.Exception(XPathErrorCodes.XPST0081, name.Location.Offset, name.Location.Length);
        }

        return new XPathQName(namespaceUri!, name.LocalName);
    }

    private void EndScope() => _scope.RemoveAt(_scope.Count - 1);

    /// <summary>The scope of a declared variable.</summary>
    internal readonly struct VariableScope : IDisposable
    {
        private readonly LoweringContext _owner;

        internal VariableScope(LoweringContext owner) => _owner = owner;

        /// <summary>Ends the declaration.</summary>
        public void Dispose() => _owner.EndScope();
    }
}
