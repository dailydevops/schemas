namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.Subset;

/// <summary>Section 4.2 and 3.3: the internal subset is read first, so its declarations bind before the external ones.</summary>
public class DeclarationPrecedenceTests
{
    [Test]
    public async Task InternalEntity_WinsOverTheExternalOneOfTheSameName()
    {
        var (model, error) = SubsetHarness.Parse(
            "<!ENTITY e 'internal'>",
            SubsetHarness.Serving("<!ENTITY e 'external'><!ENTITY f 'only external'>")
        );

        await Assert.That(error).IsNull();
        await Assert.That(model).IsEqualTo("GE:e=\"internal\"|GE:f=\"only external\"");
    }

    [Test]
    public async Task InternalParameterEntity_WinsOverTheExternalOneOfTheSameName_AndIsUsedByTheExternalSubset()
    {
        var (model, error) = SubsetHarness.Parse(
            "<!ENTITY % c 'EMPTY'>",
            SubsetHarness.Serving("<!ENTITY % c 'ANY'><!ELEMENT a %c;>")
        );

        await Assert.That(error).IsNull();
        await Assert.That(model).IsEqualTo("E:a:EMPTY|PE:c=\"EMPTY\"");
    }

    [Test]
    public async Task InternalAttributeDefault_WinsOverTheExternalDefault_AndTheExternalAttributesAreAdded()
    {
        var (model, error) = SubsetHarness.Parse(
            "<!ATTLIST e a CDATA 'internal'>",
            SubsetHarness.Serving("<!ATTLIST e a CDATA 'external' b CDATA 'added'>")
        );

        await Assert.That(error).IsNull();
        await Assert.That(model).IsEqualTo("A:e/a:CDATA:\"internal\"|A:e/b:CDATA:\"added\"");
    }

    [Test]
    public async Task InternalElementAndNotation_WinOverTheExternalOnes()
    {
        var (model, error) = SubsetHarness.Parse(
            "<!ELEMENT a EMPTY><!NOTATION n SYSTEM 'internal'>",
            SubsetHarness.Serving("<!ELEMENT a ANY><!ELEMENT b ANY><!NOTATION n SYSTEM 'external'>")
        );

        await Assert.That(error).IsNull();
        await Assert.That(model).IsEqualTo("E:a:EMPTY|E:b:ANY|N:n - internal");
    }

    [Test]
    public async Task ExternalDeclarations_BindWhenTheInternalSubsetHasNoneOfThatName()
    {
        var (model, error) = SubsetHarness.Parse(
            "<!ENTITY other 'x'>",
            SubsetHarness.Serving("<!ENTITY e 'external'><!ATTLIST p a CDATA 'ext'>")
        );

        await Assert.That(error).IsNull();
        await Assert.That(model).IsEqualTo("A:p/a:CDATA:\"ext\"|GE:e=\"external\"|GE:other=\"x\"");
    }

    [Test]
    public async Task ExternalEntityValue_UsesTheEntitiesDeclaredEarlierInTheExternalSubsetFirst()
    {
        var (model, error) = SubsetHarness.ParseSubset("<!ENTITY % v 'first'><!ENTITY % v 'second'><!ENTITY g '%v;'>");

        await Assert.That(error).IsNull();
        await Assert.That(model).IsEqualTo("GE:g=\"first\"|PE:v=\"first\"");
    }
}
