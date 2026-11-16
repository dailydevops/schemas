namespace NetEvolve.Schemas.Tests.Unit.Contract;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.Formats;

public class ResultModelShapeTests
{
    [Test]
    public async Task IssueSeverity_HasThreeValuesWithoutFatal() =>
        await Assert
            .That(Shape.Of(typeof(IssueSeverity)))
            .IsEquivalentTo(Shape.Sorted("Information=0", "Warning=1", "Error=2"));

    [Test]
    public async Task DocumentLocation_IsEquatableValueType()
    {
        await Assert.That(typeof(DocumentLocation).IsValueType).IsTrue();
        await Assert.That(typeof(DocumentLocation)).IsAssignableTo<IEquatable<DocumentLocation>>();
        await Assert
            .That(Shape.Of(typeof(DocumentLocation)))
            .IsEquivalentTo(
                Shape.Sorted(
                    ".ctor(String,Int32,Int32)",
                    "String Source { get }",
                    "Int32 Line { get }",
                    "Int32 Column { get }",
                    "Boolean Equals(DocumentLocation)",
                    "Boolean Equals(Object)",
                    "Int32 GetHashCode()",
                    "String ToString()",
                    "static Boolean op_Equality(DocumentLocation,DocumentLocation)",
                    "static Boolean op_Inequality(DocumentLocation,DocumentLocation)"
                )
            );
    }

    [Test]
    public async Task ValidationIssue_IsSealedWithApprovedMembers()
    {
        await Assert.That(typeof(ValidationIssue).IsSealed).IsTrue();
        await Assert
            .That(Shape.Of(typeof(ValidationIssue)))
            .IsEquivalentTo(
                Shape.Sorted(
                    ".ctor(DiagnosticCode,IssueSeverity,String,DocumentLocation,String)",
                    "DiagnosticCode Code { get }",
                    "IssueSeverity Severity { get }",
                    "String Message { get }",
                    "DocumentLocation Location { get }",
                    "String Constraint { get }",
                    "String ToString()"
                )
            );
    }

    [Test]
    public async Task ValidationResult_IsSealedWithApprovedMembers()
    {
        await Assert.That(typeof(ValidationResult).IsSealed).IsTrue();
        await Assert
            .That(Shape.Of(typeof(ValidationResult)))
            .IsEquivalentTo(
                Shape.Sorted(
                    ".ctor(IReadOnlyList<ValidationIssue>,Boolean,Boolean)",
                    "static ValidationResult Valid { get }",
                    "Boolean IsValid { get }",
                    "IReadOnlyList<ValidationIssue> Issues { get }",
                    "Boolean IsTruncated { get }",
                    "Boolean HasDroppedErrors { get }"
                )
            );
    }

    [Test]
    public async Task SchemaCompilationException_IsSealedExceptionWithIssues()
    {
        await Assert.That(typeof(SchemaCompilationException).IsSealed).IsTrue();
        await Assert.That(typeof(SchemaCompilationException)).IsAssignableTo<Exception>();
        await Assert
            .That(Shape.Of(typeof(SchemaCompilationException)))
            .IsEquivalentTo(
                Shape.Sorted(
                    ".ctor(IReadOnlyList<ValidationIssue>)",
                    ".ctor(IReadOnlyList<ValidationIssue>,Exception)",
                    "IReadOnlyList<ValidationIssue> Issues { get }"
                )
            );
    }

    [Test]
    public async Task ValidationIssueCollector_IsSealedWithApprovedMembers()
    {
        await Assert.That(typeof(ValidationIssueCollector).IsSealed).IsTrue();
        await Assert
            .That(Shape.Of(typeof(ValidationIssueCollector)))
            .IsEquivalentTo(
                Shape.Sorted(
                    ".ctor(ValidationLimits)",
                    "Int32 Count { get }",
                    "Boolean IsStopped { get }",
                    "Void Add(ValidationIssue)",
                    "Void Stop(ValidationIssue)",
                    "ValidationResult ToResult()"
                )
            );
    }
}
