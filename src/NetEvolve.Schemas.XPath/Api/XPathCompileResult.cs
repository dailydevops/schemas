#pragma warning disable CA1815 // Approved shape (#123): the struct defines no value equality.

namespace NetEvolve.Schemas.XPath.Api;

using System.Runtime.InteropServices;
using NetEvolve.Schemas.XPath.Errors;

/// <summary>The outcome of a compilation: the expression, or the diagnostic.</summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct XPathCompileResult
{
    /// <summary>Initializes a new instance of the <see cref="XPathCompileResult"/> struct.</summary>
    /// <param name="expression">The compiled expression, <see langword="null"/> on failure.</param>
    /// <param name="diagnostic">The diagnostic, <see langword="default"/> on success.</param>
    public XPathCompileResult(XPathExpression? expression, XPathDiagnostic diagnostic)
    {
        Expression = expression;
        Diagnostic = diagnostic;
    }

    /// <summary>Gets a value indicating whether the compilation succeeded.</summary>
    public bool Success => Expression is not null;

    /// <summary>Gets the compiled expression, <see langword="null"/> on failure.</summary>
    public XPathExpression? Expression { get; }

    /// <summary>Gets the diagnostic.</summary>
    public XPathDiagnostic Diagnostic { get; }
}
