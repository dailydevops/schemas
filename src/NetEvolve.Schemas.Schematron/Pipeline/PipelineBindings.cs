namespace NetEvolve.Schemas.Schematron.Pipeline;

using NetEvolve.Schemas.Schematron.Bindings.Registry;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Library.Core;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;

/// <summary>
/// The bindings of a build: the bindings of the registry, with the functions that XSLT 2.0 adds to XPath 2.0 (<c>current</c>,
/// <c>system-property</c>, <c>function-available</c>, and the functions the XPath package opens to an XSLT host, such as
/// <c>format-number</c> and <c>generate-id</c>) for the names of the XSLT family.
/// </summary>
/// <remarks>
/// The layering rule keeps the XSLT library out of reach of a binding, so the composition hands the functions in. The stylesheet functions of a schema are added per schema by <see cref="XsltDeclarations.WithSignatures"/>. Limitation: keys and regex
/// groups of a stylesheet are not available (#832, #250, #287): <c>key</c> and <c>regex-group</c> are not found, because their slots are not offered.
/// </remarks>
internal static class PipelineBindings
{
    /// <summary>Gets the registry of a build.</summary>
    internal static QueryBindingRegistry Registry { get; } = QueryBindingRegistry.Create(Extend);

    private static XPathStaticContext Extend(
        IQueryBinding binding,
        XPathStaticContext statics,
        out XPathHostRequirements activeHost
    )
    {
        if (!binding.AllowsXsltInstructions)
        {
            activeHost = XPathEngine.AllSlots;
            return statics;
        }

        var functions = new XsltFunctionScope(binding, statics);
        activeHost = functions.ActiveHost;
        return functions.StaticContext;
    }
}
