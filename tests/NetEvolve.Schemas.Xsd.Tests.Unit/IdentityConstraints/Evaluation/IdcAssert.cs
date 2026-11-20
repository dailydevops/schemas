namespace NetEvolve.Schemas.Xsd.Tests.Unit.IdentityConstraints.Evaluation;

using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;

internal static class IdcAssert
{
    public static async Task Issue(ValidationIssue issue, string clause, int line)
    {
        await Assert.That(issue.Code).IsEqualTo(IdcFixture.Code("cvc-identity-constraint"));
        await Assert.That(issue.Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(issue.Constraint).IsEqualTo(clause);
        await Assert.That(issue.Location.Line).IsEqualTo(line);
        await Assert.That(issue.Location.Column).IsGreaterThan(0);
        await Assert.That(issue.Location.Source).IsEqualTo(ValidationHarness.Source);
    }
}
