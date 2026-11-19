namespace NetEvolve.Schemas.XPath.Functions.Strings;

/// <summary><c>fn:lower-case</c> (Functions and Operators 1.0, 7.4.9): every character replaced by its lower-case mapping.</summary>
internal sealed class LowerCaseFunction : CaseFunction
{
    /// <summary>Initializes a new instance of the <see cref="LowerCaseFunction"/> class.</summary>
    public LowerCaseFunction()
        : base("lower-case", upper: false) { }
}
