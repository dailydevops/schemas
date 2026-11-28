namespace NetEvolve.Schemas.Schematron.Xslt.Globals;

using NetEvolve.Schemas.Schematron.Xslt.Functions;

/// <summary>
/// The compiled sequence constructor of a global <c>xsl:variable</c> or <c>xsl:param</c> (#1160): the executable that builds the value
/// into the slot of the global, and the slots of the schema level that the body reads, which the run binds before the body starts.
/// Immutable and shared by every run.
/// </summary>
internal sealed class XsltGlobalContent
{
    internal XsltGlobalContent(XsltVariableContentExecutable executable, int[] schemaSlots)
    {
        ArgumentNullException.ThrowIfNull(executable);
        ArgumentNullException.ThrowIfNull(schemaSlots);

        Executable = executable;
        SchemaSlots = schemaSlots;
    }

    /// <summary>Gets the executable; its slot is the slot of the global.</summary>
    internal XsltVariableContentExecutable Executable { get; }

    /// <summary>Gets the slots of the schema level that the body reads.</summary>
    internal int[] SchemaSlots { get; }
}
