namespace NetEvolve.Schemas.Xml.Tests.Unit.Constraints;

using static NetEvolve.Schemas.Xml.Tests.Unit.Constraints.ConstraintHarness;

/// <summary>
/// Pins the validity constraints that the validating reader mode does not report yet, one probe each. Every probe reads a
/// document that violates the constraint and asserts that the run does not report it. The probe turns red the day the
/// constraint is enforced; the matrix entry of the constraint then moves from a gap to a violation and a conforming test (see
/// <c>ConstraintMatrix</c>), and the probe is deleted.
/// </summary>
public class ConstraintGapTests
{
    // Issue #608: the declaration parser records none of the parse-time facts, so the document step has nothing to report.
    [Test]
    public async Task Gap_ProperDeclarationPeNesting_IsNotReported_Issue608() =>
        await DoesNotReport(ExternalSubsetDocument, "XML5002", ("ext.dtd", "<!ENTITY % p \"ANY>\"><!ELEMENT r %p;"));

    [Test]
    public async Task Gap_UniqueElementTypeDeclaration_IsNotReported_Issue608() =>
        await DoesNotReport(Doc("<!ELEMENT r ANY><!ELEMENT r EMPTY>", "<r/>"), "XML5006");

    [Test]
    public async Task Gap_ProperGroupPeNesting_IsNotReported_Issue608() =>
        await DoesNotReport(ExternalSubsetDocument, "XML5007", ("ext.dtd", "<!ENTITY % p \"(a|b\"><!ELEMENT r %p;) >"));

    [Test]
    public async Task Gap_ProperConditionalSectionPeNesting_IsNotReported_Issue608() =>
        await DoesNotReport(
            ExternalSubsetDocument,
            "XML5023",
            ("ext.dtd", "<!ENTITY % e \"[ <!ELEMENT r ANY> ]]>\"> <![INCLUDE %e;")
        );

    // Issue #647: the reader skips a reference to an undeclared entity without an event, so no step sees it.
    [Test]
    public async Task Gap_EntityDeclaredValidityForm_IsNotReported_Issue647() =>
        await DoesNotReport("<!DOCTYPE r SYSTEM \"ext.dtd\"><r>&e;</r>", "XML5024", ("ext.dtd", "<!ELEMENT r ANY>"));
}
