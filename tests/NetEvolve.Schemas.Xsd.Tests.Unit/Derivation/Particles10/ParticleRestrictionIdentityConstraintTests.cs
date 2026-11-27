namespace NetEvolve.Schemas.Xsd.Tests.Unit.Derivation.Particles10;

using System.Linq;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Derivation.Particles10;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Elements;
using NetEvolve.Schemas.Xsd.IdentityConstraints.Definitions;
using NetEvolve.Schemas.Xsd.SchemaDocuments;
using NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;
using static NetEvolve.Schemas.Xsd.Tests.Unit.Derivation.Particles10.ParticleKit;

/// <summary>
/// Clause 3.2.3 of <c>rcase-NameAndTypeOK</c> (Structures 1.0, 3.9.6): the identity constraints of the restriction are a subset of
/// those of the base. Two constraints are the same when they are the same definition, or when kind, selector, fields in order,
/// and (for a keyref) the referred constraint are equal; the name plays no role because each local declaration owns its own.
/// </summary>
public class ParticleRestrictionIdentityConstraintTests
{
    private const string Clause = "rcase-NameAndTypeOK.3.2.3";

    private static readonly SchemaNamespaceScope Scope = new(Tns, false);

    private readonly ParticleRestriction _check = Checker();

    private static IdentityConstraintDefinition Constraint(
        string kind,
        string name,
        string selector,
        string[] fields,
        string refer = ""
    )
    {
        var xml =
            "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" xmlns:t=\""
            + Tns
            + "\"><xs:element name=\"x\"><xs:"
            + kind
            + " name=\""
            + name
            + "\""
            + (refer.Length == 0 ? string.Empty : " refer=\"t:" + refer + "\"")
            + "><xs:selector xpath=\""
            + selector
            + "\"/>"
            + string.Concat(fields.Select(f => "<xs:field xpath=\"" + f + "\"/>"))
            + "</xs:"
            + kind
            + "></xs:element></xs:schema>";
        var reporter = new XsdIssueReporter(new ValidationIssueCollector(ValidationLimits.Default));
        var node = SchemaDocumentParser
            .Parse(xml.AsSpan(), "file:///c.xsd", reporter)
            .Root!.GetChildNode(0)
            .GetChildNode(0);
        return IdentityConstraintDefinition.Create(node, Scope, new FakeComponentRegistry([]), reporter)!;
    }

    private static IdentityConstraintDefinition Unique(string name, string selector = "e", string field = "@a") =>
        Constraint("unique", name, selector, [field]);

    private static ElementDeclaration With(string name, params IdentityConstraintDefinition[] constraints)
    {
        var declaration = Declaration(name);
        foreach (var constraint in constraints)
        {
            declaration.AddIdentityConstraint(constraint);
        }

        return declaration;
    }

    private ParticleViolation Check(ElementDeclaration restriction, ElementDeclaration baseDeclaration) =>
        _check.Check(Of(restriction, line: 10), Of(baseDeclaration, line: 20));

    [Test]
    public async Task NoConstraints_OnEitherSide_IsValid() => await AssertValid(Check(With("a"), With("a")));

    [Test]
    public async Task RestrictionWithoutConstraints_OfABaseWithConstraints_IsValid() =>
        await AssertValid(Check(With("a"), With("a", Unique("u"))));

    [Test]
    public async Task SameDefinition_OnBothSides_IsValid()
    {
        var shared = Unique("u");

        await AssertValid(Check(With("a", shared), With("a", shared)));
    }

    [Test]
    public async Task EqualConstraint_UnderAnotherName_IsTheSameConstraint() =>
        await AssertValid(Check(With("a", Unique("r")), With("a", Unique("b"))));

    [Test]
    public async Task Subset_OfTheBaseConstraints_IsValid() =>
        await AssertValid(
            Check(With("a", Unique("r2", field: "@b")), With("a", Unique("b1"), Unique("b2", field: "@b")))
        );

    [Test]
    public async Task ConstraintOnlyTheRestrictionDeclares_IsReportedAtTheRestrictingParticle() =>
        await AssertViolation(Check(With("a", Unique("r")), With("a")), XsdRuleCodes.RcaseNameAndTypeOK, Clause, 10);

    [Test]
    public async Task ConstraintWithAnotherSelector_IsNotTheSameConstraint() =>
        await AssertViolation(
            Check(With("a", Unique("r", selector: "f")), With("a", Unique("b"))),
            XsdRuleCodes.RcaseNameAndTypeOK,
            Clause,
            10
        );

    [Test]
    public async Task ConstraintWithAnotherField_IsNotTheSameConstraint() =>
        await AssertViolation(
            Check(With("a", Unique("r", field: "@b")), With("a", Unique("b"))),
            XsdRuleCodes.RcaseNameAndTypeOK,
            Clause,
            10
        );

    [Test]
    public async Task ConstraintWithMoreFields_IsNotTheSameConstraint() =>
        await AssertViolation(
            Check(
                With("a", Constraint("unique", "r", "e", ["@a", "@b"])),
                With("a", Constraint("unique", "b", "e", ["@a"]))
            ),
            XsdRuleCodes.RcaseNameAndTypeOK,
            Clause,
            10
        );

    [Test]
    public async Task FieldsInAnotherOrder_AreNotTheSameConstraint() =>
        await AssertViolation(
            Check(
                With("a", Constraint("unique", "r", "e", ["@b", "@a"])),
                With("a", Constraint("unique", "b", "e", ["@a", "@b"]))
            ),
            XsdRuleCodes.RcaseNameAndTypeOK,
            Clause,
            10
        );

    [Test]
    public async Task ConstraintOfAnotherKind_IsNotTheSameConstraint() =>
        await AssertViolation(
            Check(With("a", Constraint("key", "r", "e", ["@a"])), With("a", Unique("b"))),
            XsdRuleCodes.RcaseNameAndTypeOK,
            Clause,
            10
        );

    [Test]
    public async Task Keyref_ThatRefersToTheSameConstraint_IsTheSameConstraint() =>
        await AssertValid(
            Check(
                With("a", Constraint("keyref", "r", "e", ["@a"], "k")),
                With("a", Constraint("keyref", "b", "e", ["@a"], "k"))
            )
        );

    [Test]
    public async Task Keyref_ThatRefersToAnotherConstraint_IsNotTheSameConstraint() =>
        await AssertViolation(
            Check(
                With("a", Constraint("keyref", "r", "e", ["@a"], "k1")),
                With("a", Constraint("keyref", "b", "e", ["@a"], "k2"))
            ),
            XsdRuleCodes.RcaseNameAndTypeOK,
            Clause,
            10
        );
}
