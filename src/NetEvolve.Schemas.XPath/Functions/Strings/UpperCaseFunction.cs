namespace NetEvolve.Schemas.XPath.Functions.Strings;

/// <summary><c>fn:upper-case</c> (Functions and Operators 1.0, 7.4.8): every character replaced by its upper-case mapping.</summary>
internal sealed class UpperCaseFunction : CaseFunction
{
    /// <summary>Initializes a new instance of the <see cref="UpperCaseFunction"/> class.</summary>
    public UpperCaseFunction()
        : base("upper-case", upper: true) { }
}
