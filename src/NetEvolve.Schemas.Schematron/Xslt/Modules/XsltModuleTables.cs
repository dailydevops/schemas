namespace NetEvolve.Schemas.Schematron.Xslt.Modules;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Globals;
using NetEvolve.Schemas.Schematron.Xslt.Keys;

/// <summary>The tables that the declarations of all modules merge into, after the import precedence was applied.</summary>
internal sealed class XsltModuleTables
{
    internal XsltModuleTables(XsltFunctionTable functions, XsltGlobalTable globals, XsltKeyTable keys)
    {
        Argument.ThrowIfNull(functions);
        Argument.ThrowIfNull(globals);
        Argument.ThrowIfNull(keys);

        Functions = functions;
        Globals = globals;
        Keys = keys;
    }

    /// <summary>Gets the stylesheet functions by expanded name and arity.</summary>
    internal XsltFunctionTable Functions { get; }

    /// <summary>Gets the global variables and parameters.</summary>
    internal XsltGlobalTable Globals { get; }

    /// <summary>Gets the key declarations.</summary>
    internal XsltKeyTable Keys { get; }
}
