#pragma warning disable NE0002 // Approved public namespace NetEvolve.Schemas.Xsd for the rule catalog (design #146 DP-1)

namespace NetEvolve.Schemas.Xsd;

using NetEvolve.Arguments;

/// <summary>One rule of the catalog: a stable diagnostic code for one constraint of the specification.</summary>
/// <remarks>
/// The <see cref="Id"/> of a specification rule is the short id of the constraint (for example <c>cvc-elt</c>); engine
/// and construct rules use an own id. <see cref="Section"/> names the text and section that define the rule, the XSD
/// 1.0 text when the rule exists there and the XSD 1.1 text otherwise; it is empty when no section applies.
/// </remarks>
public sealed class XsdRule
{
    internal XsdRule(
        DiagnosticCode code,
        string id,
        string title,
        XsdRuleKind kind,
        XsdVersionMask versions,
        string section,
        IssueSeverity defaultSeverity = IssueSeverity.Error
    )
    {
        ArgumentNullException.ThrowIfNullOrEmpty(id);
        ArgumentNullException.ThrowIfNullOrEmpty(title);
        ArgumentNullException.ThrowIfNull(section);
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)kind, (int)XsdRuleKind.Validation);
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)versions, (int)XsdVersionMask.All);
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)defaultSeverity, (int)IssueSeverity.Error);

        Code = code;
        Id = id;
        Title = title;
        Kind = kind;
        Versions = versions;
        Section = section;
        DefaultSeverity = defaultSeverity;
    }

    /// <summary>Gets the stable diagnostic code.</summary>
    public DiagnosticCode Code { get; }

    /// <summary>Gets the id of the constraint.</summary>
    public string Id { get; }

    /// <summary>Gets the title of the constraint.</summary>
    public string Title { get; }

    /// <summary>Gets the family of the rule.</summary>
    public XsdRuleKind Kind { get; }

    /// <summary>Gets the versions in which the rule applies.</summary>
    public XsdVersionMask Versions { get; }

    /// <summary>Gets the text and section that define the rule.</summary>
    public string Section { get; }

    /// <summary>Gets the severity of an issue reported for the rule, unless the reporter overrides it.</summary>
    public IssueSeverity DefaultSeverity { get; }

    /// <inheritdoc />
    public override string ToString() => $"{Code} {Id}";
}
