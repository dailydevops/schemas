namespace NetEvolve.Schemas.XPath.Context;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Api;

/// <summary>The static context of an expression (XPath 2.0, 2.1.1): immutable and thread-safe.</summary>
/// <remarks>
/// Host functions form layers, in the order they were added through <see cref="Builder.WithLibrary"/> or
/// <see cref="Builder.WithFunction"/>. When two layers define the same expanded name and a matching arity, the later layer
/// wins; inside one library the later entry wins. A function whose <see cref="XPathHostFunction.Versions"/> exclude the
/// <see cref="Version"/> of the context is not visible.
/// </remarks>
public sealed class XPathStaticContext
{
    private const string FunctionNamespace = "http://www.w3.org/2005/xpath-functions";
    private const string XmlNamespace = "http://www.w3.org/XML/1998/namespace";
    private const int DefaultMaxRecursionDepth = 100;

    private readonly Dictionary<string, string> _namespaces;
    private readonly Dictionary<XPathQName, string?> _variables;
    private readonly XPathFunctionLibrary[] _layers;

    private XPathStaticContext(Builder builder)
    {
        Version = builder.Version;
        XPath10Compatibility = builder.Compatibility;
        BaseUri = builder.BaseUri;
        DefaultElementNamespace = builder.DefaultElementNamespace;
        MaxRecursionDepth = builder.MaxRecursionDepth ?? DefaultMaxRecursionDepth;
        HasRecursionDepth = builder.MaxRecursionDepth is not null;
        _namespaces = new Dictionary<string, string>(builder.Namespaces, StringComparer.Ordinal);
        _variables = new Dictionary<XPathQName, string?>(builder.Variables);
        _layers = [.. builder.Layers];
    }

    /// <summary>Gets the default static context: XPath 2.0, no compatibility mode, no host function.</summary>
    public static XPathStaticContext Default { get; } = new Builder().Build();

    /// <summary>Gets the language version.</summary>
    public XPathVersion Version { get; }

    /// <summary>Gets a value indicating whether XPath 1.0 compatibility mode is on.</summary>
    public bool XPath10Compatibility { get; }

    /// <summary>Gets the base URI, <see langword="null"/> when there is none.</summary>
    public Uri? BaseUri { get; }

    /// <summary>Gets the default element namespace, <see langword="null"/> for none.</summary>
    public string? DefaultElementNamespace { get; }

    /// <summary>Gets the deepest nesting the compiler accepts; the parser rejects a deeper expression with XPST0003, and, when it was set, the lowering and the evaluation recurse under the same bound.</summary>
    /// <remarks>The default is 100, the bound of the parser; until it is set, the lowering and the evaluation keep their own default depth.</remarks>
    public int MaxRecursionDepth { get; }

    internal bool HasRecursionDepth { get; }

    /// <summary>Gets the default function namespace.</summary>
    public string DefaultFunctionNamespace { get; } = FunctionNamespace;

    /// <summary>Creates a builder that starts from this context.</summary>
    /// <returns>The builder.</returns>
    public Builder ToBuilder() => new(this);

    internal bool TryResolveNamespace(string prefix, out string? namespaceUri) =>
        _namespaces.TryGetValue(prefix, out namespaceUri);

    internal bool TryGetVariableType(XPathQName name, out string? declaredType) =>
        _variables.TryGetValue(name, out declaredType);

    internal bool TryResolveFunction(XPathQName name, int arity, out XPathHostFunction? function)
    {
        var version = (XPathVersionMask)(1 << (int)Version);
        for (var layer = _layers.Length - 1; layer >= 0; layer--)
        {
            var functions = _layers[layer].Functions;
            for (var i = functions.Count - 1; i >= 0; i--)
            {
                var candidate = functions[i];
                if (
                    candidate.Name == name
                    && arity >= candidate.MinArity
                    && arity <= candidate.MaxArity
                    && (candidate.Versions & version) != XPathVersionMask.None
                )
                {
                    function = candidate;
                    return true;
                }
            }
        }

        function = null;
        return false;
    }

    /// <summary>Builds a static context. Not thread-safe.</summary>
    public sealed class Builder
    {
        internal Builder()
        {
            Namespaces = new Dictionary<string, string>(StringComparer.Ordinal) { ["xml"] = XmlNamespace };
            Variables = [];
            Layers = [];
        }

        internal Builder(XPathStaticContext source)
        {
            Version = source.Version;
            Compatibility = source.XPath10Compatibility;
            BaseUri = source.BaseUri;
            DefaultElementNamespace = source.DefaultElementNamespace;
            MaxRecursionDepth = source.HasRecursionDepth ? source.MaxRecursionDepth : null;
            Namespaces = new Dictionary<string, string>(source._namespaces, StringComparer.Ordinal);
            Variables = new Dictionary<XPathQName, string?>(source._variables);
            Layers = [.. source._layers];
        }

        internal XPathVersion Version { get; private set; }

        internal bool Compatibility { get; private set; }

        internal Uri? BaseUri { get; private set; }

        internal string? DefaultElementNamespace { get; private set; }

        internal int? MaxRecursionDepth { get; private set; }

        internal Dictionary<string, string> Namespaces { get; }

        internal Dictionary<XPathQName, string?> Variables { get; }

        internal List<XPathFunctionLibrary> Layers { get; }

        /// <summary>Sets the language version.</summary>
        /// <param name="version">The version.</param>
        /// <returns>This builder.</returns>
        public Builder WithVersion(XPathVersion version)
        {
            Version = version;
            return this;
        }

        /// <summary>Turns XPath 1.0 compatibility mode on or off.</summary>
        /// <param name="enabled">Whether the mode is on.</param>
        /// <returns>This builder.</returns>
        public Builder WithXPath10Compatibility(bool enabled)
        {
            Compatibility = enabled;
            return this;
        }

        /// <summary>Sets the base URI.</summary>
        /// <param name="uri">The base URI, <see langword="null"/> for none.</param>
        /// <returns>This builder.</returns>
        public Builder WithBaseUri(Uri? uri)
        {
            BaseUri = uri;
            return this;
        }

        /// <summary>Sets the deepest nesting the compiler accepts, for the parser and, with it, the lowering and the evaluation.</summary>
        /// <param name="depth">The depth, from one to the recursion depth ceiling of 10,000; the default is 100.</param>
        /// <returns>This builder.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="depth"/> is below one or above the ceiling.</exception>
        public Builder WithMaxRecursionDepth(int depth)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(depth, 1);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(depth, Limits.XPathLimits.MaxRecursionDepthCeiling);

            MaxRecursionDepth = depth;
            return this;
        }

        /// <summary>Binds a prefix to a namespace name; a later binding replaces an earlier one.</summary>
        /// <param name="prefix">The prefix.</param>
        /// <param name="namespaceName">The namespace name.</param>
        /// <returns>This builder.</returns>
        /// <exception cref="System.ArgumentNullException">An argument is <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="prefix"/> is empty.</exception>
        public Builder WithNamespace(string prefix, string namespaceName)
        {
            ArgumentNullException.ThrowIfNullOrEmpty(prefix);
            ArgumentNullException.ThrowIfNull(namespaceName);

            Namespaces[prefix] = namespaceName;
            return this;
        }

        /// <summary>Sets the default element namespace.</summary>
        /// <param name="namespaceName">The namespace name, <see langword="null"/> for none.</param>
        /// <returns>This builder.</returns>
        public Builder WithDefaultElementNamespace(string? namespaceName)
        {
            DefaultElementNamespace = namespaceName;
            return this;
        }

        /// <summary>Declares a variable.</summary>
        /// <param name="name">The variable name.</param>
        /// <param name="declaredType">The declared sequence type, <see langword="null"/> for none.</param>
        /// <returns>This builder.</returns>
        public Builder WithVariable(XPathQName name, string? declaredType = null)
        {
            Variables[name] = declaredType;
            return this;
        }

        /// <summary>Adds a host function as a layer of its own.</summary>
        /// <param name="function">The function.</param>
        /// <returns>This builder.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="function"/> is <see langword="null"/>.</exception>
        public Builder WithFunction(XPathHostFunction function)
        {
            ArgumentNullException.ThrowIfNull(function);

            return WithLibrary(new XPathFunctionLibrary(function.Name.LocalName, [function]));
        }

        /// <summary>Adds a host library as the topmost layer.</summary>
        /// <param name="library">The library.</param>
        /// <returns>This builder.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="library"/> is <see langword="null"/>.</exception>
        public Builder WithLibrary(XPathFunctionLibrary library)
        {
            ArgumentNullException.ThrowIfNull(library);

            Layers.Add(library);
            return this;
        }

        /// <summary>Builds an immutable static context; later changes to this builder do not affect it.</summary>
        /// <returns>The context.</returns>
        public XPathStaticContext Build() => new(this);
    }
}
