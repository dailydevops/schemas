namespace NetEvolve.Schemas.Schematron.Xslt.Functions;

using System.Runtime.CompilerServices;
using NetEvolve.Schemas.Schematron.Xslt.Registry;

/// <summary>
/// The registration file of the function instructions: its module initializer stages <see cref="Register"/> into the shared catalog
/// (design #150 EP-10, option A), so no shared list names the folder. The folder registers <c>xsl:function</c> (a declaration),
/// <c>xsl:sequence</c> (an instruction), <c>xsl:variable</c> (an instruction and, as the global variable of SCH-66, a declaration),
/// and <c>xsl:param</c> (a structural child of a function and, as the global parameter of SCH-66, a declaration).
/// </summary>
/// <remarks>
/// The registry holds one entry per name, and two registrations of one name fail at the first build of the default registry, so
/// the entries of <c>xsl:variable</c> and <c>xsl:param</c> are widened here and read the global declaration through
/// <c>Xslt/Globals</c> (#252) when the element is a child of the schema or of a stylesheet module.
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

        builder.Add(new XsltFunctionInstruction());
        builder.Add(new XsltSequenceInstruction());
        builder.Add(new XsltVariableInstruction());
        builder.Add(new XsltParamInstruction());
    }
}
