namespace NetEvolve.Schemas.Schematron.Bindings.Xslt1;

using NetEvolve.Schemas.Schematron.Bindings.Registry;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>
/// The registration file of the bindings with XPath 1.0 compatibility mode: <c>xslt</c> (which is also the binding of a missing
/// <c>queryBinding</c>), <c>xslt1</c>, and <c>xpath</c> (SCH-16, #310).
/// </summary>
/// <remarks>
/// <see cref="Registry.QueryBindingRegistry.Default"/> adds the result to its list. A module initializer that stages the bindings, as the
/// registration files of the XSLT instruction folders do, would need the CA2255 suppression that design #150 approved for those files
/// only, so none is used here.
/// </remarks>
internal static class Registration
{
    /// <summary>Creates the bindings of the folder.</summary>
    /// <param name="extension">Adds the host functions to every static context, or <see langword="null"/> for none.</param>
    /// <returns>The bindings, one instance per name.</returns>
    internal static IQueryBinding[] Create(XPathQueryBinding.StaticContextExtension? extension = null) =>
        [
            new XPath10QueryBinding("xpath", allowsXsltInstructions: false, extension),
            new XPath10QueryBinding("xslt", allowsXsltInstructions: true, extension),
            new XPath10QueryBinding("xslt1", allowsXsltInstructions: true, extension),
        ];
}
