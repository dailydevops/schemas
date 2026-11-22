namespace NetEvolve.Schemas.Schematron.Xslt.Functions;

using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Api;

/// <summary>
/// The stylesheet functions of a compiled schema by expanded name and arity (XSLT 2.0 section 10.3: functions are not polymorphic, so
/// the arity is part of the key). Immutable and thread-safe; it is created by <see cref="XsltFunctionTableBuilder"/>.
/// </summary>
internal sealed class XsltFunctionTable
{
    private readonly Dictionary<XPathQName, XsltFunctionComponent[]> _byName;

    internal XsltFunctionTable(Dictionary<XPathQName, XsltFunctionComponent[]> byName, int count)
    {
        ArgumentNullException.ThrowIfNull(byName);
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 0);

        _byName = byName;
        Count = count;
    }

    /// <summary>Gets the number of functions.</summary>
    internal int Count { get; }

    /// <summary>Finds the function a call with an arity resolves to.</summary>
    /// <param name="name">The expanded name.</param>
    /// <param name="arity">The number of arguments.</param>
    /// <param name="function">The function when the result is <see langword="true"/>.</param>
    /// <returns><see langword="true"/> when a function has that name and arity.</returns>
    internal bool TryGet(in XPathQName name, int arity, out XsltFunctionComponent function)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(arity, 0);

        if (_byName.TryGetValue(name, out var overloads))
        {
            for (var i = 0; i < overloads.Length; i++)
            {
                if (overloads[i].Arity == arity)
                {
                    function = overloads[i];
                    return true;
                }
            }
        }

        function = null!;
        return false;
    }
}
