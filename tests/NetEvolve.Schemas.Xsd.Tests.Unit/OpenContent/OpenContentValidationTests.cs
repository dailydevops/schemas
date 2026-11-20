namespace NetEvolve.Schemas.Xsd.Tests.Unit.OpenContent;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Elements;
using NetEvolve.Schemas.Xsd.OpenContent;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;
using NetEvolve.Schemas.Xsd.Wildcards.Core;

/// <summary>The streaming validator with open content: the instance documents of the acceptance criteria of the work package.</summary>
public class OpenContentValidationTests
{
    // Elements of the open content live in this namespace, which the wildcard ##other allows and the declared ones are not in.
    private const string Foreign = " xmlns:f=\"urn:foreign\"";

    private static SchemaFixture Schema(
        Particle content,
        OpenContentElement? own = null,
        OpenContentElement? defaultOpenContent = null,
        bool mixed = false
    )
    {
        var schema = new SchemaFixture();
        schema.Global(
            "r",
            ComplexTypeDefinition.Implicit(
                SchemaFixture.Header(),
                mixed,
                content,
                [],
                null,
                schema.Reporter,
                own,
                defaultOpenContent
            )
        );
        return schema;
    }

    private static Particle AThenB() =>
        SchemaFixture.Seq(
            SchemaFixture.Part("a", SchemaFixture.Simple("string")),
            SchemaFixture.Part("b", SchemaFixture.Simple("string"))
        );

    private static OpenContentElement Open(
        OpenContentMode mode,
        string namespaces = "##other",
        XsdProcessContents process = XsdProcessContents.Lax
    ) => OpenContentElement.CreateDefault(mode, SchemaFixture.Wild(namespaces, process), false, SchemaFixture.Where);

    private static OpenContentElement OwnOpen(
        OpenContentMode mode,
        string namespaces = "##other",
        XsdProcessContents process = XsdProcessContents.Lax
    ) =>
        OpenContentElement.Create(
            mode,
            mode == OpenContentMode.None ? null : SchemaFixture.Wild(namespaces, process),
            SchemaFixture.Where,
            new SchemaFixture().Reporter
        )!;

    private static IReadOnlyList<ValidationIssue> Run(SchemaFixture schema, string xml) =>
        ValidationHarness.Run(schema, xml);

    private static async Task AssertContentIssue(IReadOnlyList<ValidationIssue> issues, string clause, string namePart)
    {
        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(ValidationHarness.Code("cvc-complex-type"));
        await Assert.That(issues[0].Constraint).IsEqualTo(clause);
        await Assert.That(issues[0].Message).Contains(namePart);
    }

    // ---- interleave --------------------------------------------------------------------------------------------------

    [Test]
    [Arguments("<r" + Foreign + "><a/><b/></r>")]
    [Arguments("<r" + Foreign + "><f:x/><a/><b/></r>")]
    [Arguments("<r" + Foreign + "><a/><f:x/><b/></r>")]
    [Arguments("<r" + Foreign + "><a/><b/><f:x/></r>")]
    [Arguments("<r" + Foreign + "><f:x/><a/><f:y a=\"1\">t<f:z/></f:y><b/><f:x/></r>")]
    public async Task Interleave_AcceptsWildcardMatchedElementsBetweenTheDeclaredOnes(string xml)
    {
        var issues = Run(Schema(AThenB(), OwnOpen(OpenContentMode.Interleave)), xml);

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Interleave_StillRequiresTheDeclaredContent()
    {
        var issues = Run(Schema(AThenB(), OwnOpen(OpenContentMode.Interleave)), "<r" + Foreign + "><a/><f:x/></r>");

        await AssertContentIssue(issues, "cvc-complex-type.2.4", "'b'");
    }

    [Test]
    public async Task Interleave_ElementTheWildcardDoesNotAllow_IsReported()
    {
        // The wildcard is ##other: an element in no namespace that is not the next declared one matches nothing.
        var issues = Run(Schema(AThenB(), OwnOpen(OpenContentMode.Interleave)), "<r><a/><c/><b/></r>");

        await AssertContentIssue(issues, "cvc-complex-type.2.4", "'c'");
    }

    // ---- suffix ------------------------------------------------------------------------------------------------------

    [Test]
    [Arguments("<r" + Foreign + "><a/><b/></r>", 0)]
    [Arguments("<r" + Foreign + "><a/><b/><f:x/></r>", 0)]
    [Arguments("<r" + Foreign + "><a/><b/><f:x/><f:y/></r>", 0)]
    [Arguments("<r" + Foreign + "><f:x/><a/><b/></r>", 1)]
    [Arguments("<r" + Foreign + "><a/><f:x/><b/></r>", 1)]
    [Arguments("<r" + Foreign + "><a/><f:x/></r>", 1)]
    public async Task Suffix_AcceptsWildcardMatchedElementsOnlyAfterTheDeclaredContent(string xml, int expectedIssues)
    {
        var issues = Run(Schema(AThenB(), OwnOpen(OpenContentMode.Suffix)), xml);

        await Assert.That(issues.Count).IsEqualTo(expectedIssues);
    }

    [Test]
    public async Task Suffix_AfterTheFirstOpenElement_ADeclaredElementIsNotAcceptedAgain()
    {
        // (a, b?): the content is complete after a, so f:x starts the suffix, and b can no longer continue the content model.
        var content = SchemaFixture.Seq(
            SchemaFixture.Part("a", SchemaFixture.Simple("string")),
            SchemaFixture.Part("b", SchemaFixture.Simple("string"), 0, 1)
        );
        var schema = Schema(content, OwnOpen(OpenContentMode.Suffix));

        var ok = Run(schema, "<r" + Foreign + "><a/><b/><f:x/></r>");
        var bad = Run(schema, "<r" + Foreign + "><a/><f:x/><b/></r>");

        await Assert.That(ok.Count).IsEqualTo(0);
        await AssertContentIssue(bad, "cvc-complex-type.2.4", "'b'");
    }

    // ---- default open content and mode none --------------------------------------------------------------------------

    [Test]
    public async Task DefaultOpenContent_AppliesToATypeWithoutItsOwn()
    {
        var schema = Schema(AThenB(), defaultOpenContent: Open(OpenContentMode.Interleave));

        var issues = Run(schema, "<r" + Foreign + "><a/><f:x/><b/></r>");

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ModeNone_RemovesTheDefaultOpenContent()
    {
        var withDefault = Schema(AThenB(), defaultOpenContent: Open(OpenContentMode.Interleave));
        var removed = Schema(AThenB(), OwnOpen(OpenContentMode.None), Open(OpenContentMode.Interleave));
        var xml = "<r" + Foreign + "><a/><f:x/><b/></r>";

        await Assert.That(Run(withDefault, xml).Count).IsEqualTo(0);
        await AssertContentIssue(Run(removed, xml), "cvc-complex-type.2.4", "urn:foreign");
    }

    [Test]
    public async Task DefaultOpenContent_ForEmptyContent_AppliesOnlyWithAppliesToEmpty()
    {
        var wildcard = SchemaFixture.Wild("##other", XsdProcessContents.Lax);
        var xml = "<r" + Foreign + "><f:x/></r>";

        var without = new SchemaFixture();
        without.Global(
            "r",
            ComplexTypeDefinition.Implicit(
                SchemaFixture.Header(),
                false,
                null,
                [],
                null,
                without.Reporter,
                null,
                OpenContentElement.CreateDefault(OpenContentMode.Interleave, wildcard, false, SchemaFixture.Where)
            )
        );
        var with = new SchemaFixture();
        with.Global(
            "r",
            ComplexTypeDefinition.Implicit(
                SchemaFixture.Header(),
                false,
                null,
                [],
                null,
                with.Reporter,
                null,
                OpenContentElement.CreateDefault(OpenContentMode.Interleave, wildcard, true, SchemaFixture.Where)
            )
        );

        var rejected = Run(without, xml);
        var accepted = Run(with, xml);

        await Assert.That(rejected.Count).IsEqualTo(1);
        await Assert.That(rejected[0].Constraint).IsEqualTo("cvc-complex-type.2.1");
        await Assert.That(accepted.Count).IsEqualTo(0);
    }

    // ---- priority and assessment -------------------------------------------------------------------------------------

    [Test]
    public async Task DeclaredElement_TakesPriorityOverTheOpenContentWildcard()
    {
        // The wildcard allows every element, including a; a is nevertheless assessed against its declared type int.
        var content = SchemaFixture.Seq(SchemaFixture.Part("a", SchemaFixture.Simple("int")));
        var schema = Schema(content, OwnOpen(OpenContentMode.Interleave, "##any"));

        var issues = Run(schema, "<r><a>x</a></r>");

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(ValidationHarness.Code("cvc-datatype-valid"));
    }

    [Test]
    public async Task ElementOfTheContentModelThatTheModelRefuses_IsAssessedAsOpenContent()
    {
        // The second a is no longer allowed by (a); the wildcard takes it as open content, lax and without declaration.
        var content = SchemaFixture.Seq(SchemaFixture.Part("a", SchemaFixture.Simple("int")));
        var schema = Schema(content, OwnOpen(OpenContentMode.Interleave, "##any"));

        var issues = Run(schema, "<r><a>1</a><a>x</a></r>");

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task StrictOpenContent_RequiresADeclarationForTheElement()
    {
        var schema = Schema(AThenB(), OwnOpen(OpenContentMode.Interleave, "##other", XsdProcessContents.Strict));
        schema.Global("x", schema.Empty(), namespaceName: "urn:foreign");

        var known = Run(schema, "<r" + Foreign + "><a/><f:x/><b/></r>");
        var unknown = Run(schema, "<r" + Foreign + "><a/><f:y/><b/></r>");
        var invalid = Run(schema, "<r" + Foreign + "><a/><f:x>text</f:x><b/></r>");

        await Assert.That(known.Count).IsEqualTo(0);
        await Assert.That(unknown.Count).IsEqualTo(1);
        await Assert.That(unknown[0].Constraint).IsEqualTo("cvc-wildcard.2");
        await Assert.That(invalid.Count).IsEqualTo(1);
        await Assert.That(invalid[0].Constraint).IsEqualTo("cvc-complex-type.2.1");
    }

    [Test]
    public async Task SkipOpenContent_DoesNotAssessTheSubtree()
    {
        var schema = Schema(AThenB(), OwnOpen(OpenContentMode.Interleave, "##other", XsdProcessContents.Skip));

        var issues = Run(schema, "<r" + Foreign + "><a/><f:x><anything at=\"all\"/></f:x><b/></r>");

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    // ---- other content models ----------------------------------------------------------------------------------------

    [Test]
    public async Task AllGroup_AcceptsOpenContentBetweenTheMembers()
    {
        var content = SchemaFixture.All(
            SchemaFixture.Part("a", SchemaFixture.Simple("string")),
            SchemaFixture.Part("b", SchemaFixture.Simple("string"))
        );
        var schema = Schema(content, OwnOpen(OpenContentMode.Interleave));

        var ok = Run(schema, "<r" + Foreign + "><f:x/><b/><f:x/><a/><f:x/></r>");
        var missing = Run(schema, "<r" + Foreign + "><f:x/><b/></r>");

        await Assert.That(ok.Count).IsEqualTo(0);
        await Assert.That(missing.Count).IsEqualTo(1);
        await Assert.That(missing[0].Constraint).IsEqualTo("cvc-complex-type.2.4");
    }

    [Test]
    public async Task MixedContent_AcceptsOpenContentAndText()
    {
        var schema = Schema(AThenB(), OwnOpen(OpenContentMode.Interleave), mixed: true);

        var issues = Run(schema, "<r" + Foreign + ">t<a/>u<f:x/>v<b/>w</r>");

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task OpenContentOfAnElement_DoesNotLeakIntoItsChildren()
    {
        // The child c has the same kind of type without open content, and a run of its parent must not be reused for it.
        var schema = new SchemaFixture();
        var child = schema.ElementOnly(SchemaFixture.Seq(SchemaFixture.Part("a", SchemaFixture.Simple("string"))));
        var parent = ComplexTypeDefinition.Implicit(
            SchemaFixture.Header(),
            false,
            SchemaFixture.Seq(SchemaFixture.Part("c", child)),
            [],
            null,
            schema.Reporter,
            OwnOpen(OpenContentMode.Interleave)
        );
        schema.Global("r", parent);

        var issues = Run(schema, "<r" + Foreign + "><f:x/><c><a/></c><c><f:x/><a/></c></r>");

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Constraint).IsEqualTo("cvc-complex-type.2.4");
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task CountedNesting_ExceedingTheRunCeiling_IsNotAcceptedAsOpenContent(bool suffix)
    {
        var mode = suffix ? OpenContentMode.Suffix : OpenContentMode.Interleave;
        var content = SchemaFixture.Group(
            ModelGroupCompositor.Sequence,
            1,
            70,
            SchemaFixture.Part("a", SchemaFixture.Simple("string"), 1, 70)
        );
        var elements = new string('x', 60).Replace("x", "<a/>", StringComparison.Ordinal);

        // The wildcard allows every name, so it would take the a that the run refuses at its ceiling; the engine limit is
        // reported instead.
        var issues = Run(Schema(content, OwnOpen(mode, "##any")), "<r>" + elements + "</r>");

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(ValidationHarness.Code("engine-content-model-transitions"));
    }

    // ---- messages ----------------------------------------------------------------------------------------------------

    [Test]
    public async Task ExpectedNames_MentionTheWildcard_ForInterleave_AndForSuffixOnlyWhenTheContentMayEnd()
    {
        var interleave = Run(Schema(AThenB(), OwnOpen(OpenContentMode.Interleave)), "<r><c/></r>");
        var suffixIncomplete = Run(Schema(AThenB(), OwnOpen(OpenContentMode.Suffix)), "<r><c/></r>");
        var suffixComplete = Run(Schema(AThenB(), OwnOpen(OpenContentMode.Suffix)), "<r><a/><b/><c/></r>");

        await Assert.That(interleave[0].Message).Contains("any element the wildcard allows");
        await Assert.That(suffixIncomplete[0].Message).DoesNotContain("any element the wildcard allows");
        await Assert.That(suffixComplete[0].Message).Contains("any element the wildcard allows");
    }

    [Test]
    public async Task ExpectedNames_AfterTheSuffixBegan_NameOnlyTheWildcard()
    {
        // (a, b?): f:x begins the suffix, so b is refused and must not be offered as expected.
        var content = SchemaFixture.Seq(
            SchemaFixture.Part("a", SchemaFixture.Simple("string")),
            SchemaFixture.Part("b", SchemaFixture.Simple("string"), 0, 1)
        );

        var issues = Run(Schema(content, OwnOpen(OpenContentMode.Suffix)), "<r" + Foreign + "><a/><f:x/><b/></r>");

        await AssertContentIssue(
            issues,
            "cvc-complex-type.2.4",
            "One of {any element the wildcard allows} is expected."
        );
    }

    [Test]
    public async Task IncompleteContent_AtTheEndTag_DoesNotOfferTheWildcard()
    {
        // Open content never completes the content, so the message names only the missing declared element.
        var issues = Run(Schema(AThenB(), OwnOpen(OpenContentMode.Interleave)), "<r><a/></r>");

        await AssertContentIssue(issues, "cvc-complex-type.2.4", "'b'");
        await Assert.That(issues[0].Message).DoesNotContain("any element the wildcard allows");
    }
}
