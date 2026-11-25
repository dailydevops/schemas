namespace NetEvolve.Schemas.Schematron.Bindings.Registry;

using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.XPath.Api;

/// <summary>
/// A binding that can add the signature-only host functions of one schema (the stylesheet functions, design #150 EP-13) to the
/// static context of every expression it compiles. The registry holds one shared instance per name, so the functions of a schema
/// make a derived binding and never change the shared one.
/// </summary>
internal interface IFunctionHostBinding : IQueryBinding
{
    /// <summary>Creates a binding that compiles with the functions in addition to its own.</summary>
    /// <param name="functions">The host functions of the schema.</param>
    /// <returns>The derived binding with the same name and flags.</returns>
    IQueryBinding WithFunctions(IReadOnlyList<XPathHostFunction> functions);
}
