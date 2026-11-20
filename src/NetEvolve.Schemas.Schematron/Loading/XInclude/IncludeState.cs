namespace NetEvolve.Schemas.Schematron.Loading.XInclude;

using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>What one open <c>xi:include</c> element has collected and how its load went.</summary>
internal sealed class IncludeState
{
    internal IncludeState(XmlPosition position) => Position = position;

    internal XmlPosition Position { get; }

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
