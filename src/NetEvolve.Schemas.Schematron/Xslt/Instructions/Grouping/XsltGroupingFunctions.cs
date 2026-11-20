namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Grouping;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.XPath.Api;

/// <summary>
/// Creates the two host functions of the grouping instruction for a binding, one instance per version of the language (design #150
/// EP-13): <c>current-group</c> and <c>current-grouping-key</c>. A binding that allows XSLT instructions has them; the <c>xpath</c> family
/// has none. The binding <c>xslt3</c> raises the dynamic errors XTDE1061 and XTDE1071 where a slot is absent, every other XSLT binding
/// returns the empty sequence (the names <c>xslt</c> and <c>xslt1</c> evaluate with XSLT 2.0 semantics, design #150 EP-12).
/// </summary>
/// <remarks>
/// Limitation: the binding does not carry the language version yet (design #150 EP-12 adds <c>Version</c> to <see cref="IQueryBinding"/>),
/// so the version is derived from the canonical name of the binding; the decision is made here and nowhere else.
/// </remarks>
internal static class XsltGroupingFunctions
{
    private const string Xslt3 = "xslt3";

    /// <summary>Creates the functions of a binding.</summary>
    /// <param name="binding">The binding.</param>
    /// <returns><c>current-group</c> and <c>current-grouping-key</c>, or no function for a binding that does not allow XSLT instructions.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="binding"/> is <see langword="null"/>.</exception>
    internal static XPathHostFunction[] Create(IQueryBinding binding)
    {
        Argument.ThrowIfNull(binding);

        if (!binding.AllowsXsltInstructions)
        {
            return [];
        }

        var raise = string.Equals(binding.Name, Xslt3, StringComparison.Ordinal);
        return [new CurrentGroupFunction(raise), new CurrentGroupingKeyFunction(raise)];
    }
}
