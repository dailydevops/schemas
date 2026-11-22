namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Grouping;

using System.Runtime.CompilerServices;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Xslt.Registry;

/// <summary>
/// The registration file of the grouping instruction: its module initializer stages <see cref="Register"/> into the shared catalog
/// (design #150 EP-10, option A), so no shared list names the folder. The folder registers <c>xsl:for-each-group</c>, an instruction.
/// </summary>
/// <remarks>
/// <c>xsl:sort</c>, a child of <c>xsl:for-each-group</c>, is registered by the flow folder; the instruction reads its sort keys with
/// <see cref="Flow.XsltSortInstruction.TryReadKeys"/> and applies them with <see cref="Flow.XsltSorter"/>, and does not register it again.
/// </remarks>
internal static class Registration
{
    // Decision (#150, maintainer approval of 2026-11-17, point 2): the Schematron package registers its instruction folders through a
    // module initializer as the XPath package does (#123, AP8), so CA2255 is suppressed once per registration file (EP-10, option A).
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

        builder.Add(new XsltForEachGroupInstruction());
    }
}
