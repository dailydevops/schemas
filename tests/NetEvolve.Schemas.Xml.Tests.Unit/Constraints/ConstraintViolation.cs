namespace NetEvolve.Schemas.Xml.Tests.Unit.Constraints;

/// <summary>One broken rule of the coverage matrix.</summary>
/// <param name="Kind">The rule.</param>
/// <param name="Subject">What the rule was checked for: a code alias, or the key of an entry.</param>
/// <param name="Message">What is wrong.</param>
internal readonly record struct ConstraintViolation(ConstraintViolationKind Kind, string Subject, string Message)
{
    /// <inheritdoc />
    public override string ToString() => Kind + " " + Subject + ": " + Message;
}
