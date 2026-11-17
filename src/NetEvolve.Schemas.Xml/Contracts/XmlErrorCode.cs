namespace NetEvolve.Schemas.Xml.Contracts;

/// <summary>One row of the error code table: the <c>XMLnnnn</c> alias, the <c>NES</c> code, the spec constraint, and the message.</summary>
public sealed class XmlErrorCode
{
    internal XmlErrorCode(string alias, DiagnosticCode code, string constraint, string message, DiagnosticCode coreCode)
    {
        Alias = alias;
        Code = code;
        Constraint = constraint;
        Message = message;
        CoreCode = coreCode;
    }

    /// <summary>Gets the alias, <c>XML</c> plus four digits, for example <c>XML0005</c>.</summary>
    public string Alias { get; }

    /// <summary>Gets the diagnostic code in the Xml range, <c>NES1000</c> to <c>NES1999</c>.</summary>
    public DiagnosticCode Code { get; }

    /// <summary>Gets the name of the one constraint the code reports: a well-formedness or namespace constraint, a production, or an implementation limit.</summary>
    public string Constraint { get; }

    /// <summary>Gets the default message.</summary>
    public string Message { get; }

    /// <summary>Gets the core diagnostic code reported for the same condition, or <see langword="default"/> when there is none.</summary>
    public DiagnosticCode CoreCode { get; }

    /// <inheritdoc />
    public override string ToString() => Alias;
}
