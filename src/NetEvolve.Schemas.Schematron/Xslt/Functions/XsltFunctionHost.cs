namespace NetEvolve.Schemas.Schematron.Xslt.Functions;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>
/// The calls of the stylesheet functions of one run (design #150 EP-13): the table that resolves a name and arity at invoke time, and
/// the caller that runs the body. The run puts it into the host state slot of the evaluation context, where a signature-only host
/// function (<see cref="XsltUserFunction"/>) finds it, because a compiled expression is shared by concurrent runs and holds none of it.
/// </summary>
internal sealed class XsltFunctionHost
{
    /// <summary>Initializes a new instance of the <see cref="XsltFunctionHost"/> class.</summary>
    /// <param name="table">The functions that won the import precedence.</param>
    /// <param name="caller">The caller of the run.</param>
    /// <param name="run">The run that receives the problem of a failed call.</param>
    internal XsltFunctionHost(XsltFunctionTable table, XsltFunctionCaller caller, IXsltFunctionRun run)
    {
        ArgumentNullException.ThrowIfNull(table);
        ArgumentNullException.ThrowIfNull(caller);
        ArgumentNullException.ThrowIfNull(run);

        Table = table;
        Caller = caller;
        Run = run;
    }

    /// <summary>Gets the functions by expanded name and arity.</summary>
    internal XsltFunctionTable Table { get; }

    /// <summary>Gets the caller.</summary>
    internal XsltFunctionCaller Caller { get; }

    /// <summary>Gets the run that receives the problem of a failed call.</summary>
    internal IXsltFunctionRun Run { get; }
}
