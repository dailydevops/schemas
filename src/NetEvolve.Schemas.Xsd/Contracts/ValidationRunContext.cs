namespace NetEvolve.Schemas.Xsd.Contracts;

using System.Runtime.InteropServices;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;

/// <summary>The input of <see cref="ValidationListener.BeginRun"/>.</summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct ValidationRunContext
{
    internal ValidationRunContext(ValidationLimits limits, XmlNameTable names, XsdIssueReporter reporter)
    {
        Limits = limits;
        Names = names;
        Reporter = reporter;
    }

    /// <summary>Gets the limits of the run.</summary>
    public ValidationLimits Limits { get; }

    /// <summary>Gets the name table that resolves the handles of the hook inputs.</summary>
    public XmlNameTable Names { get; }

    /// <summary>Gets the reporter for the violations a listener finds.</summary>
    public XsdIssueReporter Reporter { get; }
}
