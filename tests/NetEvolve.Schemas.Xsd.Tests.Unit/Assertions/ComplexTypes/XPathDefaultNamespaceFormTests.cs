namespace NetEvolve.Schemas.Xsd.Tests.Unit.Assertions.ComplexTypes;

/// <summary>
/// The <c>xpathDefaultNamespace</c> attribute of <c>assert</c> (XSD 1.1, Structures section 3.13.2): the namespace of the unprefixed
/// element names of the test. The instance of the fixture has its elements in <c>urn:t</c> or in no namespace.
/// </summary>
public class XPathDefaultNamespaceFormTests
{
    private const string Namespaced = "\n<r xmlns=\"urn:t\"><a>1</a></r>";
    private const string Plain = "\n<r><a>1</a></r>";

    private static int Failures(string xml, string ns, string schemaAttributes = "", string assertAttributes = "") =>
        AssertionFixture
            .Run(AssertionFixture.Instance(["a = 1"], [], ns, schemaAttributes, assertAttributes), xml)
            .Count(static issue => issue.Code == XsdRuleCodes.CvcAssertion);

    [Test]
    public async Task Default_WithoutTheAttribute_NamesHaveNoNamespace()
    {
        await Assert.That(Failures(Plain, string.Empty)).IsEqualTo(0);
        await Assert.That(Failures(Namespaced, AssertionFixture.Tns)).IsEqualTo(1);
    }

    [Test]
    public async Task TargetNamespace_OnTheAssertElement_NamesTakeTheTargetNamespace()
    {
        var attribute = AssertionFixture.Default("##targetNamespace");

        await Assert.That(Failures(Namespaced, AssertionFixture.Tns, assertAttributes: attribute)).IsEqualTo(0);
        await Assert.That(Failures(Plain, string.Empty, assertAttributes: attribute)).IsEqualTo(1);
    }

    [Test]
    public async Task Local_OnTheAssertElement_NamesHaveNoNamespace()
    {
        var attribute = AssertionFixture.Default("##local");

        await Assert.That(Failures(Plain, string.Empty, assertAttributes: attribute)).IsEqualTo(0);
        await Assert.That(Failures(Namespaced, AssertionFixture.Tns, assertAttributes: attribute)).IsEqualTo(1);
    }

    [Test]
    public async Task DefaultNamespace_OnTheAssertElement_NamesTakeTheDefaultNamespaceInScope()
    {
        var attribute = AssertionFixture.Default("##defaultNamespace");

        await Assert.That(Failures(Namespaced, AssertionFixture.Tns, " xmlns=\"urn:t\"", attribute)).IsEqualTo(0);
        await Assert.That(Failures(Namespaced, AssertionFixture.Tns, " xmlns=\"urn:other\"", attribute)).IsEqualTo(1);
    }

    [Test]
    public async Task DefaultNamespace_WithoutADefaultNamespaceInScope_NamesHaveNoNamespace()
    {
        var attribute = AssertionFixture.Default("##defaultNamespace");

        await Assert.That(Failures(Plain, string.Empty, assertAttributes: attribute)).IsEqualTo(0);
    }

    [Test]
    public async Task NamespaceName_OnTheAssertElement_NamesTakeThatNamespace()
    {
        await Assert
            .That(Failures(Namespaced, AssertionFixture.Tns, assertAttributes: AssertionFixture.Default("urn:t")))
            .IsEqualTo(0);
        await Assert
            .That(Failures(Namespaced, AssertionFixture.Tns, assertAttributes: AssertionFixture.Default("urn:other")))
            .IsEqualTo(1);
    }

    [Test]
    public async Task OnTheSchemaElement_ApplyToEveryAssert()
    {
        var schemaAttribute = AssertionFixture.Default("##targetNamespace");

        await Assert.That(Failures(Namespaced, AssertionFixture.Tns, schemaAttribute)).IsEqualTo(0);
        await Assert.That(Failures(Plain, string.Empty, schemaAttribute)).IsEqualTo(1);
    }

    [Test]
    public async Task OnTheAssertElement_WinsOverTheSchemaElement()
    {
        var schemaAttribute = AssertionFixture.Default("##targetNamespace");
        var assertAttribute = AssertionFixture.Default("##local");

        await Assert.That(Failures(Plain, string.Empty, schemaAttribute, assertAttribute)).IsEqualTo(0);
        await Assert.That(Failures(Namespaced, AssertionFixture.Tns, schemaAttribute, assertAttribute)).IsEqualTo(1);
    }

    [Test]
    public async Task PrefixedAndAttributeNames_KeepTheirMeaning()
    {
        // The default namespace applies to element names only: @k has no namespace, and p:a is in urn:t whatever the default is.
        var instance = AssertionFixture.Instance(
            ["@k = 'x' and p:a = 1 and count(p:*) = 1"],
            [],
            AssertionFixture.Tns,
            " xmlns:p=\"urn:t\"",
            AssertionFixture.Default("urn:other")
        );

        var issues = AssertionFixture.Run(instance, "\n<r xmlns=\"urn:t\" k=\"x\"><a>1</a></r>");

        await Assert.That(issues).IsEmpty();
    }
}
