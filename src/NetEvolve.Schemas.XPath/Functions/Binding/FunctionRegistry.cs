namespace NetEvolve.Schemas.XPath.Functions.Binding;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Functions.QNames;

/// <summary>
/// Resolves a function call by expanded name and arity over three layers, the first match wins: the host functions of the static
/// context (their own layering applies, the later layer wins), the user-defined functions (the later one wins), and the frozen
/// catalog of built-in functions. A function is visible only when its version mask contains the version of the static context and
/// every host slot it needs is active. Immutable and thread-safe.
/// </summary>
/// <remarks>
/// Limitations: the registry does not reject a host or user function that shadows a built-in one in the <c>fn</c> or <c>xs</c>
/// namespace; that policy belongs to the registration (#123 AP8, #297). A host function that the host flags hide does not reveal an
/// older host layer behind it, because the static context resolves one candidate only. The compiler is not wired yet:
/// <c>LoweringContext.ResolveFunction</c> still resolves host functions only and returns <see cref="XPathHostFunction"/>, whose
/// <c>Invoke</c> skips the conversion, so its call site must switch to <see cref="TryResolve"/> and <see cref="XPathFunction.Call"/>.
/// </remarks>
internal sealed class FunctionRegistry
{
    private readonly Dictionary<XPathQName, XPathFunction[]> _builtIn;
    private readonly XPathFunction[] _user;

    private FunctionRegistry(Dictionary<XPathQName, XPathFunction[]> builtIn, XPathFunction[] user)
    {
        _builtIn = builtIn;
        _user = user;
    }

    /// <summary>Resolves a function.</summary>
    /// <param name="context">The static context: the language version and the host functions.</param>
    /// <param name="name">The expanded name.</param>
    /// <param name="arity">The number of arguments.</param>
    /// <param name="activeHost">The host slots the host provides; a function that needs another slot is not found.</param>
    /// <param name="function">The function, <see langword="null"/> when there is none.</param>
    /// <returns><see langword="true"/> when a function matches the name and arity in this context.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="arity"/> is negative.</exception>
    public bool TryResolve(
        XPathStaticContext context,
        XPathQName name,
        int arity,
        XPathHostRequirements activeHost,
        out XPathFunction? function
    )
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentOutOfRangeException.ThrowIfLessThan(arity, 0);

        var version = (XPathVersionMask)(1 << (int)context.Version);

        if (context.TryResolveFunction(name, arity, out var host) && IsActive(host!, activeHost))
        {
            function = host as XPathFunction ?? new HostFunctionAdapter(host!);
            return true;
        }

        for (var i = _user.Length - 1; i >= 0; i--)
        {
            if (Matches(_user[i], name, arity, version, activeHost))
            {
                function = _user[i];
                return true;
            }
        }

        if (_builtIn.TryGetValue(name, out var candidates))
        {
            for (var i = 0; i < candidates.Length; i++)
            {
                if (Matches(candidates[i], name, arity, version, activeHost))
                {
                    function = BindStaticBaseUri(candidates[i], context);
                    return true;
                }
            }
        }

        function = null;
        return false;
    }

    // A call reaches the evaluation context only, so the functions that read the static base URI get the one of the static context here (#806).
    private static XPathFunction BindStaticBaseUri(XPathFunction candidate, XPathStaticContext context) =>
        context.BaseUri is null
            ? candidate
            : candidate switch
            {
                StaticBaseUriFunction => new StaticBaseUriFunction(context.BaseUri),
                ResolveUriFunction => new ResolveUriFunction(context.BaseUri),
                _ => candidate,
            };

    /// <summary>Answers <c>function-available</c>: whether <see cref="TryResolve"/> finds the function.</summary>
    /// <param name="context">The static context.</param>
    /// <param name="name">The expanded name.</param>
    /// <param name="arity">The number of arguments.</param>
    /// <param name="activeHost">The host slots the host provides.</param>
    /// <returns><see langword="true"/> when a function matches.</returns>
    /// <remarks>Limitation: the arity is required; the one-argument form of <c>function-available</c> would have to enumerate the host layers, which the static context does not expose (#587).</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="arity"/> is negative.</exception>
    public bool IsAvailable(XPathStaticContext context, XPathQName name, int arity, XPathHostRequirements activeHost) =>
        TryResolve(context, name, arity, activeHost, out _);

    /// <summary>Asks whether the catalog of built-in functions defines a name and arity under a language version; host and user functions do not count.</summary>
    /// <param name="version">The language version.</param>
    /// <param name="name">The expanded name.</param>
    /// <param name="arity">The number of arguments.</param>
    /// <returns><see langword="true"/> when a built-in function matches; the host slots a function needs do not matter.</returns>
    /// <remarks>The query behind a static-context option that accepts host functions in the default function namespace only for names the catalog does not define (#150, P-X2); the option itself is not part of this registry.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="version"/> is not a defined version, or <paramref name="arity"/> is negative.</exception>
    public bool IsDefinedByCatalog(XPathVersion version, XPathQName name, int arity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan((int)version, (int)XPathVersion.V20);
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)version, (int)XPathVersion.V31);
        ArgumentOutOfRangeException.ThrowIfLessThan(arity, 0);

        if (!_builtIn.TryGetValue(name, out var candidates))
        {
            return false;
        }

        var mask = (XPathVersionMask)(1 << (int)version);
        for (var i = 0; i < candidates.Length; i++)
        {
            if (
                arity >= candidates[i].MinArity
                && arity <= candidates[i].MaxArity
                && (candidates[i].Versions & mask) != XPathVersionMask.None
            )
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Creates a registry with a further layer of user-defined functions on top of this one.</summary>
    /// <param name="functions">The functions; the later entry wins, and the list is copied.</param>
    /// <returns>The new registry; this one does not change.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="functions"/> or one of its entries is <see langword="null"/>.</exception>
    public FunctionRegistry WithUserFunctions(IReadOnlyList<XPathFunction> functions)
    {
        ArgumentNullException.ThrowIfNull(functions);

        var user = new XPathFunction[_user.Length + functions.Count];
        Array.Copy(_user, user, _user.Length);
        for (var i = 0; i < functions.Count; i++)
        {
            ArgumentNullException.ThrowIfNull(functions[i]);

            user[_user.Length + i] = functions[i];
        }

        return new FunctionRegistry(_builtIn, user);
    }

    /// <summary>Creates the registry of the built-in functions.</summary>
    /// <param name="functions">The functions.</param>
    /// <returns>The registry.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="functions"/> or one of its entries is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Two functions register the same expanded name for an overlapping arity and version.</exception>
    internal static FunctionRegistry Create(IReadOnlyList<XPathFunction> functions)
    {
        ArgumentNullException.ThrowIfNull(functions);

        var byName = new Dictionary<XPathQName, List<XPathFunction>>();
        for (var i = 0; i < functions.Count; i++)
        {
            var function = functions[i];
            ArgumentNullException.ThrowIfNull(function);

            if (!byName.TryGetValue(function.Name, out var same))
            {
                same = [];
                byName.Add(function.Name, same);
            }

            for (var j = 0; j < same.Count; j++)
            {
                if (Overlaps(same[j], function))
                {
                    throw new InvalidOperationException(
                        $"The function {function.Name} is registered twice for an overlapping arity and version."
                    );
                }
            }

            same.Add(function);
        }

        var builtIn = new Dictionary<XPathQName, XPathFunction[]>(byName.Count);
        foreach (var entry in byName)
        {
            builtIn.Add(entry.Key, entry.Value.ToArray());
        }

        return new FunctionRegistry(builtIn, []);
    }

    private static bool Overlaps(XPathFunction a, XPathFunction b) =>
        a.MinArity <= b.MaxArity && b.MinArity <= a.MaxArity && (a.Versions & b.Versions) != XPathVersionMask.None;

    private static bool Matches(
        XPathFunction function,
        XPathQName name,
        int arity,
        XPathVersionMask version,
        XPathHostRequirements activeHost
    ) =>
        function.Name == name
        && arity >= function.MinArity
        && arity <= function.MaxArity
        && (function.Versions & version) != XPathVersionMask.None
        && IsActive(function, activeHost);

    private static bool IsActive(XPathHostFunction function, XPathHostRequirements activeHost) =>
        (function.HostRequirements & ~activeHost) == XPathHostRequirements.None;
}
