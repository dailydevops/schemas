namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Construction;

using System.Runtime.CompilerServices;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Registry;

/// <summary>
/// The registration file of the construction instructions: its module initializer stages <see cref="Register"/> into the shared
/// catalog (design #150 EP-10, option A), so no shared list names the folder. The folder registers <c>xsl:element</c>,
/// <c>xsl:attribute</c>, <c>xsl:copy</c>, <c>xsl:copy-of</c>, <c>xsl:comment</c>, <c>xsl:processing-instruction</c>,
/// <c>xsl:namespace</c>, and <c>xsl:document</c> (instructions), and the two synthetic entries that have no XSLT name: the literal
/// result element and the text node of a sequence constructor (design #150 EP-9).
/// </summary>
/// <remarks>
/// Limitation: <c>xsl:value-of</c> and <c>xsl:text</c> belong to the flow folder, <c>xsl:attribute-set</c> and
/// <c>xsl:namespace-alias</c> are not supported (#34, #35), and the schema-aware <c>type</c> and <c>validation</c> attributes are
/// the unsupported construct error (#23).
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
    /// <exception cref="System.InvalidOperationException">A name or a synthetic entry is already registered.</exception>
    internal static void Register(XsltRegistryBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Add(new XsltElementInstruction());
        builder.Add(new XsltAttributeInstruction());
        builder.Add(new XsltCopyInstruction());
        builder.Add(new XsltCopyOfInstruction());
        builder.Add(new XsltCommentInstruction());
        builder.Add(new XsltProcessingInstructionInstruction());
        builder.Add(new XsltNamespaceInstruction());
        builder.Add(new XsltDocumentInstruction());
        builder.AddSynthetic(XsltSyntheticElement.LiteralResultElement, new XsltLiteralElementInstruction());
        builder.AddSynthetic(XsltSyntheticElement.TextNode, new XsltLiteralTextInstruction());
    }
}
