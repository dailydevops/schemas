namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Assertions.Structural;

using NetEvolve.Schemas.Conformance.Results;
using static NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Assertions.Structural.StructuralHarness;

/// <summary>assert-xml: the result is serialized through the model-to-sink pump and the XML writer and compared with the expected XML.</summary>
public class AssertXmlTests
{
    private const string Prefixed = "<p:r xmlns:p=\"urn:p\"><p:a q=\"1\"/></p:r>";

    [Test]
    public async Task SequenceOfTwoElementsAndATextNode_Matches()
    {
        var pass = Run("/doc/node()", Xml("<a>1</a>x<b/>"));
        var wrongText = Run("/doc/node()", Xml("<a>1</a>y<b/>"));
        var wrongOrder = Run("/doc/node()", Xml("<b/><a>1</a>x"));
        var missing = Run("/doc/node()", Xml("<a>1</a>x"));

        await Assert.That(pass.Outcome).IsEqualTo(CaseOutcome.Pass).Because(pass.Reason ?? string.Empty);
        await Assert.That(wrongText.Outcome).IsEqualTo(CaseOutcome.Fail);
        await Assert.That(wrongOrder.Outcome).IsEqualTo(CaseOutcome.Fail);
        await Assert.That(missing.Outcome).IsEqualTo(CaseOutcome.Fail);
    }

    [Test]
    public async Task IgnorePrefixes_PrefixDiffersOnlyInTheName_PassesOnlyWithTheFlag()
    {
        const string expected = "<q:a xmlns:q=\"urn:p\" q=\"1\"/>";

        var strict = Run("/p:r/p:a", Xml(expected), Prefixed);
        var relaxed = Run("/p:r/p:a", Xml(expected, ignorePrefixes: true), Prefixed);
        var samePrefix = Run("/p:r/p:a", Xml("<p:a xmlns:p=\"urn:p\" q=\"1\"/>"), Prefixed);

        await Assert.That(strict.Outcome).IsEqualTo(CaseOutcome.Fail);
        await Assert.That(relaxed.Outcome).IsEqualTo(CaseOutcome.Pass).Because(relaxed.Reason ?? string.Empty);
        await Assert.That(samePrefix.Outcome).IsEqualTo(CaseOutcome.Pass).Because(samePrefix.Reason ?? string.Empty);
    }

    [Test]
    public async Task IgnorePrefixes_NamespaceOrNameDiffers_StillFails()
    {
        var otherUri = Run("/p:r/p:a", Xml("<p:a xmlns:p=\"urn:other\" q=\"1\"/>", ignorePrefixes: true), Prefixed);
        var otherName = Run("/p:r/p:a", Xml("<p:b xmlns:p=\"urn:p\" q=\"1\"/>", ignorePrefixes: true), Prefixed);
        var otherValue = Run("/p:r/p:a", Xml("<p:a xmlns:p=\"urn:p\" q=\"2\"/>", ignorePrefixes: true), Prefixed);

        await Assert.That(otherUri.Outcome).IsEqualTo(CaseOutcome.Fail);
        await Assert.That(otherName.Outcome).IsEqualTo(CaseOutcome.Fail);
        await Assert.That(otherValue.Outcome).IsEqualTo(CaseOutcome.Fail);
    }

    [Test]
    public async Task AdjacentAtomicValues_AreSeparatedBySingleSpaces()
    {
        var atomics = Run("(1, 2, 3)", Xml("1 2 3"));
        var mixed = Run("(1, /doc/a, 2, 3)", Xml("1<a>1</a>2 3"));
        var glued = Run("(1, 2, 3)", Xml("123"));

        await Assert.That(atomics.Outcome).IsEqualTo(CaseOutcome.Pass).Because(atomics.Reason ?? string.Empty);
        await Assert.That(mixed.Outcome).IsEqualTo(CaseOutcome.Pass).Because(mixed.Reason ?? string.Empty);
        await Assert.That(glued.Outcome).IsEqualTo(CaseOutcome.Fail);
    }

    [Test]
    public async Task DocumentNode_IsSerializedAsItsChildren()
    {
        var pass = Run("/", Xml("<doc id=\"7\"><a>1</a>x<b/></doc>"));
        var fail = Run("/", Xml("<doc id=\"8\"><a>1</a>x<b/></doc>"));

        await Assert.That(pass.Outcome).IsEqualTo(CaseOutcome.Pass).Because(pass.Reason ?? string.Empty);
        await Assert.That(fail.Outcome).IsEqualTo(CaseOutcome.Fail);
    }

    [Test]
    public async Task EmptySequence_MatchesAnEmptyExpectation()
    {
        var pass = Run("()", Xml(string.Empty));
        var fail = Run("()", Xml("<a/>"));

        await Assert.That(pass.Outcome).IsEqualTo(CaseOutcome.Pass).Because(pass.Reason ?? string.Empty);
        await Assert.That(fail.Outcome).IsEqualTo(CaseOutcome.Fail);
    }

    [Test]
    public async Task ExpectedXmlFromAFile_IsComparedAsADocument()
    {
        var file = ("expected.xml", "<?xml version=\"1.0\"?>\n<a>1</a>");

        var pass = Run("/doc/a", "<assert-xml file=\"expected.xml\"/>", Document, file);
        var fail = Run("/doc/b", "<assert-xml file=\"expected.xml\"/>", Document, file);

        await Assert.That(pass.Outcome).IsEqualTo(CaseOutcome.Pass).Because(pass.Reason ?? string.Empty);
        await Assert.That(fail.Outcome).IsEqualTo(CaseOutcome.Fail);
    }

    [Test]
    [Arguments("/doc/@id")]
    [Arguments("(/doc/a, /doc/@id)")]
    public async Task AttributeItem_CannotBeSerialized_Fails(string query)
    {
        var result = Run(query, Xml("7"));

        await Assert.That(result.Outcome).IsEqualTo(CaseOutcome.Fail);
        await Assert.That(result.Reason).IsNotNull();
    }

    [Test]
    public async Task ExpectedXmlThatIsNotWellFormed_Fails()
    {
        var result = Run("/doc/a", Xml("<a>"));

        await Assert.That(result.Outcome).IsEqualTo(CaseOutcome.Fail);
    }

    [Test]
    public async Task QueryRaisesAnError_Fails()
    {
        var result = Run("1 div 0", Xml("1"));

        await Assert.That(result.Outcome).IsEqualTo(CaseOutcome.Fail);
    }
}
