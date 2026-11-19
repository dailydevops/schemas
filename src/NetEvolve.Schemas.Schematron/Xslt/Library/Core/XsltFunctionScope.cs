namespace NetEvolve.Schemas.Schematron.Xslt.Library.Core;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Functions.Binding;

/// <summary>
/// The functions in scope for the expressions of one binding: the XPath catalog under the host flags of the binding, the core library
/// of this folder (<c>current</c>, <c>system-property</c>, <c>function-available</c>), and the stylesheet functions. An XSLT binding
/// sets the XSLT 2.0 host flag of the XPath package, which makes <c>generate-id</c> visible there; this folder holds no second
/// implementation of it (design #150 ownership table, rule: a host function is added only if the catalog does not define it). An
/// XPath binding sets no flag and adds no function, so none of the XSLT functions is found. Immutable and thread-safe.
/// </summary>
/// <remarks>
/// Limitations: the one-argument form of <c>function-available</c> probes the arities 0 to <see cref="MaxProbedArity"/>, because the
/// static context does not enumerate its functions (#587); a function whose smallest arity is higher is not found. The compiler of the
/// XPath package does not pass the host flags to the registry yet (#297), and the stylesheet functions become host functions with
/// #628, so until then the scope answers the lookups and the compiled call path does not use it.
/// </remarks>
internal sealed class XsltFunctionScope
{
    /// <summary>The highest arity that the one-argument form of <c>function-available</c> probes.</summary>
    internal const int MaxProbedArity = 255;

    private readonly XsltFunctionTable? _user;

    /// <summary>Initializes a new instance of the <see cref="XsltFunctionScope"/> class.</summary>
    /// <param name="binding">The binding; one that allows XSLT instructions gets the core library and the XSLT 2.0 host flag.</param>
    /// <param name="statics">The static context of the expressions: version, namespaces, and the layers below the core library.</param>
    /// <param name="userFunctions">The stylesheet functions, or <see langword="null"/> for none.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="binding"/> or <paramref name="statics"/> is <see langword="null"/>.</exception>
    internal XsltFunctionScope(
        IQueryBinding binding,
        XPathStaticContext statics,
        XsltFunctionTable? userFunctions = null
    )
    {
        Argument.ThrowIfNull(binding);
        Argument.ThrowIfNull(statics);

        _user = userFunctions;
        if (!binding.AllowsXsltInstructions)
        {
            ActiveHost = XPathHostRequirements.None;
            CoreFunctions = [];
            StaticContext = statics;
            return;
        }

        // The slot the run of an XSLT binding provides, and the flag that opens the functions XSLT 2.0 adds to XPath 2.0.
        ActiveHost = XPathHostRequirements.Xslt20Host | XPathHostRequirements.CurrentItem;
        CoreFunctions =
        [
            new CurrentFunction(),
            new SystemPropertyFunction(statics),
            new FunctionAvailableFunction(this),
        ];
        StaticContext = statics.ToBuilder().WithLibrary(new XPathFunctionLibrary("xslt-core", CoreFunctions)).Build();
    }

    /// <summary>Gets the static context with the core library as its top layer.</summary>
    internal XPathStaticContext StaticContext { get; }

    /// <summary>Gets the host slots and flags that the host of this binding provides.</summary>
    internal XPathHostRequirements ActiveHost { get; }

    /// <summary>Gets the functions that this folder adds to the catalog of the XPath package.</summary>
    internal XPathHostFunction[] CoreFunctions { get; }

    /// <summary>Gets the expanded name of a function of the default function namespace.</summary>
    /// <param name="localName">The local name.</param>
    /// <returns>The name.</returns>
    /// <exception cref="System.ArgumentNullException"><paramref name="localName"/> is <see langword="null"/>.</exception>
    /// <exception cref="System.ArgumentException"><paramref name="localName"/> is empty.</exception>
    internal static XPathQName Name(string localName)
    {
        Argument.ThrowIfNullOrEmpty(localName);

        return new(XPathStaticContext.Default.DefaultFunctionNamespace, localName);
    }

    /// <summary>Finds a function of the XPath catalog or of the core library.</summary>
    /// <param name="name">The expanded name.</param>
    /// <param name="arity">The number of arguments.</param>
    /// <param name="function">The function when the result is <see langword="true"/>.</param>
    /// <returns><see langword="true"/> when a function matches the name and arity under the host flags.</returns>
    internal bool TryResolve(XPathQName name, int arity, out XPathHostFunction? function)
    {
        function = null;
        if (
            arity < 0
            || !FunctionCatalog.Default.Registry.TryResolve(StaticContext, name, arity, ActiveHost, out var found)
        )
        {
            return false;
        }

        function = found;
        return true;
    }

    /// <summary>Asks whether a function of a name and arity is in scope, stylesheet functions included.</summary>
    /// <param name="name">The expanded name.</param>
    /// <param name="arity">The number of arguments.</param>
    /// <returns><see langword="true"/> when the function is in scope.</returns>
    internal bool IsAvailable(XPathQName name, int arity) =>
        TryResolve(name, arity, out _) || (arity >= 0 && _user is not null && _user.TryGet(in name, arity, out _));

    /// <summary>Asks whether a function of a name and some arity is in scope.</summary>
    /// <param name="name">The expanded name.</param>
    /// <returns><see langword="true"/> when the function is in scope with an arity up to <see cref="MaxProbedArity"/>.</returns>
    internal bool IsAvailable(XPathQName name)
    {
        for (var arity = 0; arity <= MaxProbedArity; arity++)
        {
            if (IsAvailable(name, arity))
            {
                return true;
            }
        }

        return false;
    }
}
