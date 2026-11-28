namespace NetEvolve.Schemas.Schematron.Xslt.Modules;

using System.Collections.Generic;
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
        XsltDecimalFormatTable decimalFormats,
        IReadOnlyDictionary<int, XsltGlobalContent>? globalContents = null
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
        GlobalContents = globalContents ?? new Dictionary<int, XsltGlobalContent>();
    }

    /// <summary>Gets the stylesheet functions by expanded name and arity.</summary>
    internal XsltFunctionTable Functions { get; }

    /// <summary>Gets the global variables and parameters.</summary>
    internal XsltGlobalTable Globals { get; }

    /// <summary>Gets the key declarations.</summary>
    internal XsltKeyTable Keys { get; }

    /// <summary>Gets the compiled sequence constructors of the globals that have one, by the slot of the global (#1160).</summary>
    internal IReadOnlyDictionary<int, XsltGlobalContent> GlobalContents { get; }

    /// <summary>Gets the decimal formats, merged by name with the import precedence applied.</summary>
    internal XsltDecimalFormatTable DecimalFormats { get; }
}
