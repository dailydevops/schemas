namespace NetEvolve.Schemas.Xsd.Tests.Unit.Assertions.ComplexTypes;

using System.Collections.Generic;

public class AssertionEvaluationTests
{
    private static DiagnosticCode[] Codes(IReadOnlyList<ValidationIssue> issues) =>
        [.. issues.Select(static issue => issue.Code)];

    [Test]
    public async Task Assertion_OverChildValuesAndAttributes_Passes()
    {
        var issues = AssertionFixture.Run("\n<r k=\"x\"><a>1</a><b>2</b></r>", "@k = 'x' and a = 1 and b = 2");

        await Assert.That(issues).IsEmpty();
    }

    [Test]
    public async Task Assertion_OverAttribute_Fails_IssueIsLocatedAtTheElement()
    {
        var issues = AssertionFixture.Run("\n<r k=\"y\"><a>1</a></r>", "@k = 'x'");

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(XsdRuleCodes.CvcAssertion);
        await Assert.That(issues[0].Constraint).IsEqualTo("cvc-assertion");
        await Assert.That(issues[0].Location.Line).IsEqualTo(2);
        await Assert.That(issues[0].Message).Contains("@k = 'x'");
    }

    [Test]
    public async Task Assertion_OnANestedElement_IssueIsLocatedAtThatElement()
    {
        var instance = AssertionFixture.Instance([], ["n = 2"]);

        var issues = AssertionFixture.Run(instance, "\n<r>\n<c><n>1</n></c>\n</r>");

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(XsdRuleCodes.CvcAssertion);
        await Assert.That(issues[0].Location.Line).IsEqualTo(3);
    }

    [Test]
    public async Task Assertion_IntegerChild_ComparesNumerically()
    {
        // The text 10 sorts before 9 as a string, so a pass shows the numeric comparison.
        var issues = AssertionFixture.Run("\n<r><a>10</a></r>", "a > 9", "a >= 10", "a < 11");

        await Assert.That(issues).IsEmpty();
    }

    [Test]
    public async Task Assertion_ChildValues_AreUntypedUntilTheEvaluatorTakesAnnotations()
    {
        // Limitation pinned for #791: the XPath evaluator atomizes every node as xs:untypedAtomic, so two integer children compare as
        // strings and 9 does not come before 10. With typed values both assertions pass, and this test then states the typed result.
        var issues = AssertionFixture.Run("\n<r><a>9</a><b>10</b></r>", "a lt b");

        await Assert.That(Codes(issues)).IsEquivalentTo([XsdRuleCodes.CvcAssertion]);
    }

    [Test]
    public async Task Assertion_DynamicError_ReportsTheAssertionRuleAtTheElement()
    {
        var issues = AssertionFixture.Run("\n<r><a>1</a></r>", "1 idiv 0 = 1");

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(XsdRuleCodes.CvcAssertion);
        await Assert.That(issues[0].Constraint).IsEqualTo("cvc-assertion");
        await Assert.That(issues[0].Location.Line).IsEqualTo(2);
        await Assert.That(issues[0].Message).Contains("FOAR0001");
    }

    [Test]
    public async Task Assertion_ResultWithoutEffectiveBooleanValue_IsADynamicError()
    {
        var issues = AssertionFixture.Run("\n<r/>", "(1, 2)");

        await Assert.That(Codes(issues)).IsEquivalentTo([XsdRuleCodes.CvcAssertion]);
        await Assert.That(issues[0].Message).Contains("FORG0006");
    }

    [Test]
    public async Task Assertion_ErrorOfOneAssertion_DoesNotStopTheNext()
    {
        var issues = AssertionFixture.Run("\n<r/>", "1 idiv 0 = 1", "false()", "true()");

        await Assert.That(Codes(issues)).IsEquivalentTo([XsdRuleCodes.CvcAssertion, XsdRuleCodes.CvcAssertion]);
        await Assert.That(issues[0].Message).Contains("dynamic error");
        await Assert.That(issues[1].Message).Contains("does not satisfy");
    }

    [Test]
    public async Task Assertion_ParentAndAncestorAccess_ReturnEmpty()
    {
        var instance = AssertionFixture.Instance(
            [],
            ["count(..) = 0 and count(ancestor::*) = 0 and count(../*) = 0 and empty(../b) and empty(parent::node())"]
        );

        var issues = AssertionFixture.Run(instance, "\n<r><b>1</b><c><n>1</n></c></r>");

        await Assert.That(issues).IsEmpty();
    }

    [Test]
    public async Task Assertion_SiblingsOfTheElement_AreNotReachable()
    {
        var instance = AssertionFixture.Instance([], ["empty(preceding-sibling::*) and empty(following-sibling::*)"]);

        var issues = AssertionFixture.Run(instance, "\n<r><a>1</a><c><n>1</n></c><b>2</b></r>");

        await Assert.That(issues).IsEmpty();
    }

    [Test]
    public async Task Assertion_RootOfTheTree_IsTheElementNotADocument()
    {
        var instance = AssertionFixture.Instance([], ["boolean(/)"]);

        var issues = AssertionFixture.Run(instance, "\n<r><c><n>1</n></c></r>");

        // The tree has no document node, so the root step raises XPDY0050 as it does for any parentless element tree.
        await Assert.That(Codes(issues)).IsEquivalentTo([XsdRuleCodes.CvcAssertion]);
        await Assert.That(issues[0].Message).Contains("XPDY0050");
    }

    [Test]
    public async Task Assertion_ContextItem_IsTheElementAndItsDescendantsAreVisible()
    {
        var issues = AssertionFixture.Run(
            "\n<r><a>1</a><c><n>3</n></c></r>",
            "name(.) = 'r' and count(.//*) = 3 and c/n = 3 and count(root(.)) = 1 and root(.) is ."
        );

        await Assert.That(issues).IsEmpty();
    }

    [Test]
    public async Task Assertion_NestedAssertions_UseSeparateSubtrees()
    {
        var instance = AssertionFixture.Instance(["count(c) = 1 and count(c/*) = 1"], ["count(*) = 1 and empty(..)"]);

        var issues = AssertionFixture.Run(instance, "\n<r><a>1</a><c><n>1</n></c></r>");

        await Assert.That(issues).IsEmpty();
    }

    [Test]
    public async Task Assertion_FailingNestedAssertion_LeavesTheOuterOneAlone()
    {
        var instance = AssertionFixture.Instance(["count(c) = 1"], ["false()"]);

        var issues = AssertionFixture.Run(instance, "\n<r>\n<c><n>1</n></c></r>");

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Location.Line).IsEqualTo(3);
    }

    [Test]
    public async Task Assertion_SeesDefaultedAttribute_InAPlainRun()
    {
        var passes = AssertionFixture.Run("\n<r/>", "@d = 9");
        var fails = AssertionFixture.Run("\n<r d=\"3\"/>", "@d = 9");

        await Assert.That(passes).IsEmpty();
        await Assert.That(Codes(fails)).IsEquivalentTo([XsdRuleCodes.CvcAssertion]);
    }

    [Test]
    public async Task Assertion_SeesDefaultedElementValue_InAPlainRun()
    {
        var issues = AssertionFixture.Run("\n<r><e/></r>", "e = 'x'");

        await Assert.That(issues).IsEmpty();
    }

    [Test]
    public async Task Assertion_EveryFailedAssertion_ReportsItsOwnIssueInDocumentOrder()
    {
        var issues = AssertionFixture.Run("\n<r/>", "false()", "true()", "1 = 2");

        await Assert.That(issues.Count).IsEqualTo(2);
        await Assert.That(issues[0].Message).Contains("false()");
        await Assert.That(issues[1].Message).Contains("1 = 2");
    }

    [Test]
    public async Task Assertion_ElementOfATypeWithoutAssertions_IsNotChecked()
    {
        var instance = AssertionFixture.Instance(["false()"], []);

        var issues = AssertionFixture.Run(instance, "\n<r><c><n>1</n></c><sc>1</sc></r>");

        await Assert.That(Codes(issues)).IsEquivalentTo([XsdRuleCodes.CvcAssertion]);
        await Assert.That(issues[0].Location.Line).IsEqualTo(2);
    }

    [Test]
    public async Task Assertion_ValueOfSimpleContent_IsTheTypedValue()
    {
        var instance = AssertionFixture.Instance([], [], simpleTests: ["$value eq 5"]);

        var passes = AssertionFixture.Run(instance, "\n<r><sc>5</sc></r>");
        var fails = AssertionFixture.Run(instance, "\n<r><sc>6</sc></r>");

        await Assert.That(passes).IsEmpty();
        await Assert.That(Codes(fails)).IsEquivalentTo([XsdRuleCodes.CvcAssertion]);
    }

    [Test]
    public async Task Assertion_ValueOfAnInvalidElement_IsTheEmptySequence()
    {
        var instance = AssertionFixture.Instance([], [], simpleTests: ["empty($value)"]);

        var issues = AssertionFixture.Run(instance, "\n<r><sc>x</sc></r>");

        await Assert.That(issues.Where(static issue => issue.Code == XsdRuleCodes.CvcAssertion)).IsEmpty();
    }

    [Test]
    public async Task Assertion_ValueOfElementOnlyContent_IsTheEmptySequence()
    {
        var issues = AssertionFixture.Run("\n<r><a>1</a></r>", "empty($value)");

        await Assert.That(issues).IsEmpty();
    }

    [Test]
    public async Task Assertion_SubtreeAboveTheLimit_YieldsTheLimitCodeAndNoVerdict()
    {
        var instance = AssertionFixture.Instance(["false()"], []);
        var limits = ValidationLimits.Default.WithMaxInputSize(8);

        var issues = AssertionFixture.Run(instance, "\n<r><a>12345</a><b>12345</b></r>", limits);

        await Assert.That(Codes(issues)).IsEquivalentTo([CoreDiagnosticCodes.InputSizeLimitExceeded]);
        await Assert.That(issues[0].Location.Line).IsEqualTo(2);
    }

    [Test]
    public async Task Assertion_SubtreeOfEmptyElementsAboveTheLimit_YieldsTheLimitCodeAndNoVerdict()
    {
        var instance = AssertionFixture.Instance(["false()"], []);
        var limits = ValidationLimits.Default.WithMaxInputSize(2);

        var issues = AssertionFixture.Run(instance, "\n<r><s/><s/><s/></r>", limits);

        await Assert.That(Codes(issues)).IsEquivalentTo([CoreDiagnosticCodes.InputSizeLimitExceeded]);
    }

    [Test]
    public async Task Assertion_SubtreeAtTheLimit_IsEvaluated()
    {
        // Ten characters of text, the defaulted attribute d of one character, and the start tags of the two children, one each.
        var instance = AssertionFixture.Instance(["false()"], []);
        var limits = ValidationLimits.Default.WithMaxInputSize(13);

        var issues = AssertionFixture.Run(instance, "\n<r><a>12345</a><b>12345</b></r>", limits);

        await Assert.That(Codes(issues)).IsEquivalentTo([XsdRuleCodes.CvcAssertion]);
    }

    [Test]
    public async Task Assertion_AfterAnAbandonedSubtree_NextElementIsEvaluated()
    {
        var instance = AssertionFixture.Instance([], ["false()"]);
        var limits = ValidationLimits.Default.WithMaxInputSize(3);

        var issues = AssertionFixture.Run(instance, "\n<r><c><n>12345</n></c>\n<c><n>1</n></c></r>", limits);

        await Assert
            .That(Codes(issues))
            .IsEquivalentTo([CoreDiagnosticCodes.InputSizeLimitExceeded, XsdRuleCodes.CvcAssertion]);
    }

    [Test]
    [Arguments("\n<r><zz>1</zz><a>1</a></r>")]
    [Arguments("\n<r q=\"1\"><a>1</a></r>")]
    [Arguments("\n<r>text<a>1</a></r>")]
    [Arguments("\n<r><a>x</a><b>1</b></r>")]
    [Arguments("\n<r><c><zz/></c><a>1</a></r>")]
    [Arguments("\n<r><c><n>1</n><zz><y/></zz></c><a>1</a></r>")]
    public async Task Assertion_ContentThatTheValidatorRejects_DoesNotBreakTheSubtree(string xml)
    {
        var instance = AssertionFixture.Instance(["count(a) = 1"], ["count(n) = 1"]);

        var issues = AssertionFixture.Run(instance, xml);

        // The run ends and the assertions see what the validator handed over; the rejected content is the validator's to report.
        await Assert.That(issues).IsNotEmpty();
    }
}
