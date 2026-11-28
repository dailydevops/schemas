namespace NetEvolve.Schemas.Schematron.Loading.XInclude;

using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;

/// <summary>What one open <c>xi:include</c> element has collected and how its load went.</summary>
internal sealed class IncludeState
{
    internal IncludeState(XmlPosition position) => Position = position;

    internal XmlPosition Position { get; }

    /// <summary>Gets the prefix bindings of the <c>xi:include</c> and <c>xi:fallback</c> elements, one per prefix, the innermost last.</summary>
    internal List<(XmlSymbol Prefix, XmlSymbol Uri)> Declarations { get; } = [];

    /// <summary>Records a declaration, replacing an earlier one of the same prefix.</summary>
    internal void Declare(XmlSymbol prefix, XmlSymbol uri)
    {
        _ = Declarations.RemoveAll(d => d.Prefix == prefix);
        Declarations.Add((prefix, uri));
    }

    internal void Inherit(IncludeState outer) => Declarations.AddRange(outer.Declarations);

    internal string? Href { get; set; }

    internal string? Parse { get; set; }

    internal string? XPointer { get; set; }

    internal string? Encoding { get; set; }

    /// <summary>Gets or sets a value indicating whether an <c>xi:fallback</c> child was seen.</summary>
    internal bool HasFallback { get; set; }

    /// <summary>Gets a value indicating whether the load failed with a resource error.</summary>
    internal bool Failed { get; private set; }

    /// <summary>Gets the resource error, meaningful when <see cref="Failed"/> is <see langword="true"/>.</summary>
    internal SchematronProblem Problem { get; private set; }

    internal void Fail(in SchematronProblem problem)
    {
        Failed = true;
        Problem = problem;
    }
}
