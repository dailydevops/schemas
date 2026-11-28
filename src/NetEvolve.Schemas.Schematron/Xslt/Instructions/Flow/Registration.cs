namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;

using System.Runtime.CompilerServices;
using NetEvolve.Schemas.Schematron.Xslt.Registry;

/// <summary>
/// The registration file of the flow instructions: its module initializer stages <see cref="Register"/> into the shared catalog
/// (design #150 EP-10, option A), so no shared list names the folder. The folder registers <c>xsl:value-of</c>, <c>xsl:text</c>,
/// <c>xsl:if</c>, <c>xsl:choose</c>, <c>xsl:for-each</c>, and <c>xsl:message</c> (instructions), and <c>xsl:when</c>,
/// <c>xsl:otherwise</c>, and <c>xsl:sort</c> (structural children of their parents).
/// </summary>
/// <remarks>
/// <c>xsl:sort</c> is also a child of <c>xsl:for-each-group</c> and <c>xsl:apply-templates</c>; the registry holds one entry per
/// name, so those instructions read their sort keys with <see cref="XsltSortInstruction.TryReadKeys"/> and apply them with
/// <see cref="XsltSorter"/>, and do not register <c>xsl:sort</c> again.
/// </remarks>
internal static class Registration
{
    // Decision (#150, maintainer approval of 2026-11-17, point 2): the Schematron package registers its instruction folders through a
    // module initializer as the XPath package does (#123, AP8), so CA2255 is suppressed in the registration file of the folder, once.
#pragma warning disable CA2255
    [ModuleInitializer]
    internal static void Stage() => XsltRegistryCatalog.Shared.Stage(Register);
#pragma warning restore CA2255

    /// <summary>Adds the instructions of the folder.</summary>
    /// <param name="builder">The registry builder.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    /// <exception cref="System.InvalidOperationException">A name is already registered.</exception>
    internal static void Register(XsltRegistryBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Add(new XsltValueOfInstruction());
        builder.Add(new XsltTextInstruction());
        builder.Add(new XsltIfInstruction());
        builder.Add(new XsltChooseInstruction());
        builder.Add(new XsltWhenInstruction());
        builder.Add(new XsltOtherwiseInstruction());
        builder.Add(new XsltForEachInstruction());
        builder.Add(new XsltSortInstruction());
        builder.Add(new XsltMessageInstruction());
    }
}
