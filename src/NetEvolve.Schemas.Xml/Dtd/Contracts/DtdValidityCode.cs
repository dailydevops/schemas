namespace NetEvolve.Schemas.Xml.Dtd.Contracts;

/// <summary>One row of the DTD code table: the alias, the <c>NES</c> code, the official name, the section, the owning step, and the message.</summary>
/// <remarks>
/// Validity constraints use <c>XML5001</c> to <c>XML5026</c> in specification order (<c>NES1501</c> to <c>NES1526</c>);
/// other reports use <c>XML59nn</c> (<c>NES16nn</c>).
/// The <c>XML59nn</c> reports cannot follow the formula of <see cref="NetEvolve.Schemas.Xml.Contracts.XmlErrorCodes"/> because
/// it would give them the numbers of the validity constraints.
/// </remarks>
public sealed class DtdValidityCode
{
    internal DtdValidityCode(
        string alias,
        int number,
        DtdValidityCodeKind kind,
        string name,
        string section,
        string owner,
        IssueSeverity defaultSeverity,
        string message
    )
    {
        ArgumentNullException.ThrowIfNullOrEmpty(alias);
        ArgumentNullException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(section);
        ArgumentNullException.ThrowIfNullOrEmpty(owner);
        ArgumentNullException.ThrowIfNullOrEmpty(message);
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)kind, (int)DtdValidityCodeKind.Report);
        ArgumentOutOfRangeException.ThrowIfLessThan((int)kind, (int)DtdValidityCodeKind.ValidityConstraint);
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)defaultSeverity, (int)IssueSeverity.Error);

        Alias = alias;
        Code = new DiagnosticCode(number);
        Kind = kind;
        Name = name;
        Section = section;
        Owner = owner;
        DefaultSeverity = defaultSeverity;
        Message = message;
    }

    /// <summary>Gets the alias, <c>XML</c> plus four digits, for example <c>XML5004</c>.</summary>
    public string Alias { get; }

    /// <summary>Gets the diagnostic code in the Xml range, <c>NES1000</c> to <c>NES1999</c>.</summary>
    public DiagnosticCode Code { get; }

    /// <summary>Gets the family of the code.</summary>
    public DtdValidityCodeKind Kind { get; }

    /// <summary>Gets the official name of the validity constraint, or the name of the report or limit.</summary>
    public string Name { get; }

    /// <summary>Gets the section of XML 1.0 (Fifth Edition) that defines the constraint, empty for a limit.</summary>
    public string Section { get; }

    /// <summary>Gets the work package that owns the step reporting or enforcing the code, for example <c>XML-63</c>.</summary>
    public string Owner { get; }

    /// <summary>Gets the severity of an issue reported with the code, unless the step overrides it.</summary>
    public IssueSeverity DefaultSeverity { get; }

    /// <summary>Gets the default message.</summary>
    public string Message { get; }

    /// <inheritdoc />
    public override string ToString() => Alias;
}
