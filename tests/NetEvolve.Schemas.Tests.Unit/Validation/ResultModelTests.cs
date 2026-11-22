namespace NetEvolve.Schemas.Tests.Unit.Validation;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NetEvolve.Arguments;

public class ResultModelTests
{
    private static ValidationIssue Issue(IssueSeverity severity, string message = "m") =>
        new(new DiagnosticCode(1234), severity, message, new DocumentLocation("a.xml", 1, 5));

    [Test]
    [Arguments(new IssueSeverity[0], false, false, true)]
    [Arguments(new[] { IssueSeverity.Information }, false, false, true)]
    [Arguments(new[] { IssueSeverity.Warning }, false, false, true)]
    [Arguments(new[] { IssueSeverity.Information, IssueSeverity.Warning }, false, false, true)]
    [Arguments(new[] { IssueSeverity.Error }, false, false, false)]
    [Arguments(new[] { IssueSeverity.Warning, IssueSeverity.Error }, false, false, false)]
    [Arguments(new IssueSeverity[0], true, true, false)]
    [Arguments(new[] { IssueSeverity.Warning }, true, true, false)]
    [Arguments(new[] { IssueSeverity.Warning }, true, false, true)]
    public async Task IsValid_SeverityMix_FollowsTruthTable(
        IssueSeverity[] severities,
        bool isTruncated,
        bool hasDroppedErrors,
        bool expected
    )
    {
        ArgumentNullException.ThrowIfNull(severities);

        var issues = new List<ValidationIssue>();
        foreach (var severity in severities)
        {
            issues.Add(Issue(severity));
        }

        var result = new ValidationResult(issues, isTruncated, hasDroppedErrors);

        await Assert.That(result.IsValid).IsEqualTo(expected);
        await Assert.That(result.IsTruncated).IsEqualTo(isTruncated);
        await Assert.That(result.HasDroppedErrors).IsEqualTo(hasDroppedErrors);
    }

    [Test]
    public async Task Constructor_HasDroppedErrorsWithoutTruncated_ThrowsArgumentException() =>
        await Assert.That(() => new ValidationResult([], false, true)).Throws<ArgumentException>();

    [Test]
    public async Task Constructor_NullIssues_ThrowsArgumentNullException() =>
        await Assert.That(() => new ValidationResult(null!)).Throws<ArgumentNullException>();

    [Test]
    public async Task Constructor_NullElement_ThrowsArgumentNullException() =>
        await Assert.That(() => new ValidationResult([null!])).Throws<ArgumentNullException>();

    [Test]
    public async Task Valid_IsSharedAndEmpty()
    {
        await Assert.That(ValidationResult.Valid).IsSameReferenceAs(ValidationResult.Valid);
        await Assert.That(ValidationResult.Valid.IsValid).IsTrue();
        await Assert.That(ValidationResult.Valid.Issues.Count).IsEqualTo(0);
        await Assert.That(ValidationResult.Valid.IsTruncated).IsFalse();
    }

    [Test]
    public async Task Issues_IsSnapshotOfTheGivenList()
    {
        var source = new List<ValidationIssue> { Issue(IssueSeverity.Warning) };
        var result = new ValidationResult(source);

        source.Add(Issue(IssueSeverity.Error));

        await Assert.That(result.Issues.Count).IsEqualTo(1);
        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Location_EqualValues_AreEqualWithSameHashCode()
    {
        var left = new DocumentLocation("a.xml", 3, 4);
        var right = new DocumentLocation("a.xml", 3, 4);

        await Assert.That(left.Equals(right)).IsTrue();
        await Assert.That(left.Equals((object)right)).IsTrue();
        await Assert.That(left == right).IsTrue();
        await Assert.That(left != right).IsFalse();
        await Assert.That(left.GetHashCode()).IsEqualTo(right.GetHashCode());
    }

    [Test]
    [Arguments("b.xml", 3, 4)]
    [Arguments("a.xml", 2, 4)]
    [Arguments("a.xml", 3, 5)]
    [Arguments(null, 3, 4)]
    [Arguments("A.XML", 3, 4)]
    public async Task Location_DifferingPart_IsNotEqual(string? source, int line, int column)
    {
        var left = new DocumentLocation("a.xml", 3, 4);
        var right = new DocumentLocation(source, line, column);

        await Assert.That(left.Equals(right)).IsFalse();
        await Assert.That(left.Equals((object)right)).IsFalse();
        await Assert.That(left == right).IsFalse();
        await Assert.That(left != right).IsTrue();
    }

    [Test]
    public async Task Location_EqualsOtherType_IsFalse() =>
        await Assert.That(new DocumentLocation(null, 1, 1).Equals("x")).IsFalse();

    [Test]
    public async Task Location_Default_IsUnknown()
    {
        var location = default(DocumentLocation);

        await Assert.That(location.Source).IsNull();
        await Assert.That(location.Line).IsEqualTo(0);
        await Assert.That(location.Column).IsEqualTo(0);
        await Assert.That(location.ToString()).IsEqualTo("(0,0)");
    }

    [Test]
    public async Task Location_ToString_UsesSourceLineColumn()
    {
        await Assert.That(new DocumentLocation("a.xml", 3, 4).ToString()).IsEqualTo("a.xml(3,4)");
        await Assert.That(new DocumentLocation(null, 3, 4).ToString()).IsEqualTo("(3,4)");
    }

    [Test]
    public async Task Location_NegativeLine_ThrowsArgumentOutOfRangeException() =>
        await Assert.That(() => new DocumentLocation("a", -1, 0)).Throws<ArgumentOutOfRangeException>();

    [Test]
    public async Task Location_NegativeColumn_ThrowsArgumentOutOfRangeException() =>
        await Assert.That(() => new DocumentLocation("a", 0, -1)).Throws<ArgumentOutOfRangeException>();

    [Test]
    public async Task Issue_ExposesGivenValues()
    {
        var location = new DocumentLocation("a.xml", 1, 5);
        var issue = new ValidationIssue(new DiagnosticCode(1234), IssueSeverity.Warning, "msg", location, "c-1");

        await Assert.That(issue.Code).IsEqualTo(new DiagnosticCode(1234));
        await Assert.That(issue.Severity).IsEqualTo(IssueSeverity.Warning);
        await Assert.That(issue.Message).IsEqualTo("msg");
        await Assert.That(issue.Location).IsEqualTo(location);
        await Assert.That(issue.Constraint).IsEqualTo("c-1");
    }

    [Test]
    public async Task Issue_ToString_UsesCodeSeverityLocationMessage()
    {
        var issue = new ValidationIssue(
            new DiagnosticCode(1234),
            IssueSeverity.Error,
            "bad",
            new DocumentLocation("source", 1, 5)
        );

        await Assert.That(issue.ToString()).IsEqualTo("NES1234 Error source(1,5): bad");
    }

    [Test]
    public async Task Issue_NullMessage_ThrowsArgumentNullException() =>
        await Assert
            .That(() => new ValidationIssue(default, IssueSeverity.Error, null!, default))
            .Throws<ArgumentNullException>();

    [Test]
    public async Task Issue_UndefinedSeverity_ThrowsArgumentOutOfRangeException() =>
        await Assert
            .That(() => new ValidationIssue(default, (IssueSeverity)3, "m", default))
            .Throws<ArgumentOutOfRangeException>();

    [Test]
    public async Task Exception_CarriesIssuesAndInnerException()
    {
        var issue = Issue(IssueSeverity.Error, "bad");
        var inner = new InvalidOperationException("inner");

        var exception = new SchemaCompilationException([issue], inner);

        await Assert.That(exception.Issues.Count).IsEqualTo(1);
        await Assert.That(exception.Issues[0]).IsSameReferenceAs(issue);
        await Assert.That(exception.InnerException).IsSameReferenceAs(inner);
        await Assert.That(exception.Message).IsEqualTo($"Schema compilation failed with 1 issue(s). First: {issue}");
    }

    [Test]
    public async Task Exception_WithoutInner_HasNoInnerException()
    {
        var exception = new SchemaCompilationException([Issue(IssueSeverity.Error)]);

        await Assert.That(exception.InnerException).IsNull();
    }

    [Test]
    public async Task Exception_Issues_IsSnapshot()
    {
        var source = new List<ValidationIssue> { Issue(IssueSeverity.Error) };
        var exception = new SchemaCompilationException(source);

        source.Add(Issue(IssueSeverity.Warning));

        await Assert.That(exception.Issues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Exception_NullIssues_ThrowsArgumentNullException()
    {
        await Assert.That(() => new SchemaCompilationException(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => new SchemaCompilationException(null!, null)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Exception_NullElement_ThrowsArgumentNullException() =>
        await Assert.That(() => new SchemaCompilationException([null!])).Throws<ArgumentNullException>();

    [Test]
    public async Task Exception_EmptyIssues_ThrowsArgumentException() =>
        await Assert.That(() => new SchemaCompilationException([])).Throws<ArgumentException>();
}
