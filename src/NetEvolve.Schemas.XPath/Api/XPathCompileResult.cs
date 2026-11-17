namespace NetEvolve.Schemas.XPath.Api;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NetEvolve.Schemas.XPath.Errors;

/// <summary>The outcome of a compilation: the expression, or the diagnostic.</summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct XPathCompileResult : IEquatable<XPathCompileResult>
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

    /// <inheritdoc />
    public bool Equals(XPathCompileResult other) =>
        ReferenceEquals(Expression, other.Expression) && Diagnostic == other.Diagnostic;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is XPathCompileResult other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() =>
        unchecked((RuntimeHelpers.GetHashCode(Expression) * 31) + Diagnostic.GetHashCode());

    /// <summary>Determines whether two results are equal.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true"/> when equal.</returns>
    public static bool operator ==(XPathCompileResult left, XPathCompileResult right) => left.Equals(right);

    /// <summary>Determines whether two results differ.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true"/> when different.</returns>
    public static bool operator !=(XPathCompileResult left, XPathCompileResult right) => !left.Equals(right);
}
