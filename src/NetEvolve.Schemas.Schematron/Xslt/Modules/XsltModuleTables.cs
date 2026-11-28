namespace NetEvolve.Schemas.Schematron.Xslt.Modules;

using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Globals;
using NetEvolve.Schemas.Schematron.Xslt.Keys;
using NetEvolve.Schemas.Schematron.Xslt.Library.Formatting;

/// <summary>The tables that the declarations of all modules merge into, after the import precedence was applied.</summary>
internal sealed class XsltModuleTables
{
    internal XsltModuleTables(
        XsltFunctionTable functions,
        XsltGlobalTable globals,
        XsltKeyTable keys,
        XsltDecimalFormatTable decimalFormats
    )
    {
        ArgumentNullException.ThrowIfNull(functions);
        ArgumentNullException.ThrowIfNull(globals);
        ArgumentNullException.ThrowIfNull(keys);
        ArgumentNullException.ThrowIfNull(decimalFormats);

        Functions = functions;
        Globals = globals;
        Keys = keys;
        DecimalFormats = decimalFormats;
    }

    /// <summary>Gets the stylesheet functions by expanded name and arity.</summary>
    internal XsltFunctionTable Functions { get; }

    /// <summary>Gets the global variables and parameters.</summary>
    internal XsltGlobalTable Globals { get; }

    /// <summary>Gets the key declarations.</summary>
    internal XsltKeyTable Keys { get; }

    /// <summary>Gets the decimal formats, merged by name with the import precedence applied.</summary>
    internal XsltDecimalFormatTable DecimalFormats { get; }
}
