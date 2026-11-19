namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.Subset;

using NetEvolve.Schemas.Xml.Contracts;

/// <summary>Section 4.4.8 and 2.8: parameter-entity references inside declarations of the external subset, with the added spaces.</summary>
public class ParameterEntityInDeclarationTests
{
    [Test]
    [Arguments("<!ENTITY % c \"(a|b)\"><!ELEMENT x %c;>", "E:x:(a|b)|PE:c=\"(a|b)\"")]
    [Arguments("<!ENTITY % n \"x\"><!ELEMENT %n; EMPTY>", "E:x:EMPTY|PE:n=\"x\"")]
    [Arguments("<!ENTITY % k \"ANY\"><!ELEMENT x %k;>", "E:x:ANY|PE:k=\"ANY\"")]
    [Arguments("<!ENTITY % s \"|b\"><!ELEMENT x (a %s;)>", "E:x:(a|b)|PE:s=\"|b\"")]
    public async Task ReferenceInAnElementDeclaration_IncludesTheReplacementText(string subset, string expected)
    {
        ArgumentNullException.ThrowIfNull(subset);
        var (model, error) = SubsetHarness.ParseSubset(subset);

        await Assert.That(error).IsNull();
        await Assert.That(model).IsEqualTo(expected);
    }

    [Test]
    public async Task ReplacementText_GetsALeadingAndATrailingSpace()
    {
        var (model, error) = SubsetHarness.ParseSubset("<!ENTITY % n \"x\"><!ELEMENT%n;EMPTY>");

        await Assert.That(error).IsNull();
        await Assert.That(model).IsEqualTo("E:x:EMPTY|PE:n=\"x\"");
    }

    [Test]
    [Arguments("<!ENTITY % t \"CDATA\"><!ATTLIST e a %t; #IMPLIED>", "A:e/a:CDATA:#IMPLIED|PE:t=\"CDATA\"")]
    [Arguments(
        "<!ENTITY % d \"a CDATA #IMPLIED\"><!ATTLIST e %d; b ID #REQUIRED>",
        "A:e/a:CDATA:#IMPLIED|A:e/b:ID:#REQUIRED|PE:d=\"a CDATA #IMPLIED\""
    )]
    [Arguments(
        "<!ENTITY % m \"b ID #REQUIRED\"><!ATTLIST e a CDATA #IMPLIED %m;>",
        "A:e/a:CDATA:#IMPLIED|A:e/b:ID:#REQUIRED|PE:m=\"b ID #REQUIRED\""
    )]
    [Arguments("<!ENTITY % v \"(x|y)\"><!ATTLIST e a %v; 'x'>", "A:e/a:(x|y):\"x\"|PE:v=\"(x|y)\"")]
    public async Task ReferenceInAnAttributeListDeclaration_IncludesTheReplacementText(string subset, string expected)
    {
        ArgumentNullException.ThrowIfNull(subset);
        var (model, error) = SubsetHarness.ParseSubset(subset);

        await Assert.That(error).IsNull();
        await Assert.That(model).IsEqualTo(expected);
    }

    [Test]
    public async Task AttributeDefaultLiteral_DoesNotRecognizeAParameterEntityReference()
    {
        var (model, error) = SubsetHarness.ParseSubset("<!ENTITY % y \"no\"><!ATTLIST e a CDATA \"x%y;z\">");

        await Assert.That(error).IsNull();
        await Assert.That(model).IsEqualTo("A:e/a:CDATA:\"x%y;z\"|PE:y=\"no\"");
    }

    [Test]
    [Arguments("<!ENTITY % v \"value\"><!ENTITY g \"%v; more\">", "GE:g=\"value more\"|PE:v=\"value\"")]
    [Arguments("<!ENTITY % a \"x\"><!ENTITY % b \"%a;%a;\"><!ENTITY g \"%b;\">", "GE:g=\"xx\"|PE:a=\"x\"|PE:b=\"xx\"")]
    [Arguments("<!ENTITY % q '\"'><!ENTITY g \"a%q;b\">", "GE:g=\"a\"b\"|PE:q=\"\"\"")]
    [Arguments("<!ENTITY % a \"1\"><!ENTITY % b \"%a;2\">", "PE:a=\"1\"|PE:b=\"12\"")]
    [Arguments("<!ENTITY % id \"SYSTEM 's.ent'\"><!ENTITY g %id;>", "GE:g - s.ent|PE:id=\"SYSTEM 's.ent'\"")]
    public async Task ReferenceInAnEntityDeclaration_IncludesTheReplacementText(string subset, string expected)
    {
        ArgumentNullException.ThrowIfNull(subset);
        var (model, error) = SubsetHarness.ParseSubset(subset);

        await Assert.That(error).IsNull();
        await Assert.That(model).IsEqualTo(expected);
    }

    [Test]
    public async Task ReferenceInANotationDeclaration_IncludesTheReplacementText()
    {
        var (model, error) = SubsetHarness.ParseSubset("<!ENTITY % pid \"PUBLIC 'p' 's'\"><!NOTATION n %pid;>");

        await Assert.That(error).IsNull();
        await Assert.That(model).IsEqualTo("PE:pid=\"PUBLIC 'p' 's'\"|N:n p s");
    }

    [Test]
    public async Task ReferenceInsideAReplacementText_IsIncludedToo()
    {
        var (model, error) = SubsetHarness.ParseSubset(
            "<!ENTITY % inner \"EMPTY\"><!ENTITY % outer \"&#37;inner;\"><!ELEMENT a %outer;>"
        );

        await Assert.That(error).IsNull();
        await Assert.That(model).IsEqualTo("E:a:EMPTY|PE:inner=\"EMPTY\"|PE:outer=\"%inner;\"");
    }

    [Test]
    public async Task UndeclaredReferenceInsideADeclaration_IsSkipped()
    {
        var (model, error) = SubsetHarness.ParseSubset("<!ELEMENT a %missing; EMPTY>");

        await Assert.That(error).IsNull();
        await Assert.That(model).IsEqualTo("E:a:EMPTY");
    }

    [Test]
    public async Task ReferenceBetweenDeclarations_IncludesCompleteDeclarations()
    {
        var (model, error) = SubsetHarness.ParseSubset("<!ENTITY % d \"<!ELEMENT a EMPTY>\"> %d; <!ELEMENT b ANY>");

        await Assert.That(error).IsNull();
        await Assert.That(model).IsEqualTo("E:a:EMPTY|E:b:ANY|PE:d=\"<!ELEMENT a EMPTY>\"");
    }

    [Test]
    [Arguments("<!ENTITY % e \"<!ELEMENT \"> %e; doc (#PCDATA)>")]
    [Arguments("<!ENTITY % e \"<!--\"> %e; -->")]
    [Arguments("<!ENTITY % e1 \"<!ELEMENT \"><!ENTITY % e2 \">\"> %e1; doc (#PCDATA) %e2;")]
    public async Task ReferenceBetweenDeclarations_WithAPartialDeclaration_RaisesXml0011(string subset)
    {
        ArgumentNullException.ThrowIfNull(subset);
        var (_, error) = SubsetHarness.ParseSubset(subset);

        await Assert.That(error).IsSameReferenceAs(XmlErrorCodes.XML0011);
    }

    [Test]
    [Arguments("<!ELEMENT doc ANY><!ENTITY e \"100%\">")]
    [Arguments("<!ENTITY e \"100%x\">")]
    [Arguments("<!ENTITY e \"%\">")]
    [Arguments("<!ENTITY % p \"x\"><!ELEMENT a %p>")]
    public async Task MalformedReference_RaisesXml0011(string subset)
    {
        ArgumentNullException.ThrowIfNull(subset);
        var (_, error) = SubsetHarness.ParseSubset(subset);

        await Assert.That(error).IsSameReferenceAs(XmlErrorCodes.XML0011);
    }

    [Test]
    public async Task ReferenceInsideADeclarationOfTheInternalSubset_StaysXml0019_AndTheExternalSubsetIsNotRequested()
    {
        var resolver = SubsetHarness.Serving("<!ELEMENT b EMPTY>");

        var (_, error) = SubsetHarness.Parse("<!ENTITY % c \"EMPTY\"><!ELEMENT a %c;>", resolver);

        await Assert.That(error).IsSameReferenceAs(XmlErrorCodes.XML0019);
        await Assert.That(resolver.Requests).IsEmpty();
    }

    [Test]
    public async Task ReferenceInAnEntityValue_ToAnEntityOfTheInternalSubset_IsIncluded()
    {
        var (model, error) = SubsetHarness.Parse(
            "<!ENTITY % v \"internal\">",
            SubsetHarness.Serving("<!ENTITY g \"x %v; y\">")
        );

        await Assert.That(error).IsNull();
        await Assert.That(model).IsEqualTo("GE:g=\"x internal y\"|PE:v=\"internal\"");
    }

    [Test]
    public async Task SelfReferencingEntity_InAnEntityValue_RaisesXml0020()
    {
        var (_, error) = SubsetHarness.ParseSubset("<!ENTITY % a \"&#37;a;\"><!ENTITY % b \"%a;\">");

        await Assert.That(error).IsSameReferenceAs(XmlErrorCodes.XML0020);
    }

    [Test]
    [Arguments("<!ENTITY % s \"|b\"><!ELEMENT x (a %s; %s;)>")]
    [Arguments("<!ENTITY % n \"x\"><!ENTITY % k \"EMPTY\"><!ELEMENT %n; %k;><!ELEMENT y (%n;|%n;)>")]
    [Arguments("<!ENTITY % t \"CDATA\"><!ATTLIST e a %t; #IMPLIED b %t; #IMPLIED>")]
    public async Task RepeatedReferenceInsideADeclaration_IsNotRecursion(string subset)
    {
        ArgumentNullException.ThrowIfNull(subset);
        var (_, error) = SubsetHarness.ParseSubset(subset);

        await Assert.That(error).IsNull();
    }

    [Test]
    [Arguments("<!ENTITY % a \"x\"><!ENTITY g \"%a;%a;\">", "GE:g=\"xx\"|PE:a=\"x\"")]
    [Arguments(
        "<!ENTITY % a \"x\"><!ENTITY % b \"%a;\"><!ENTITY % c \"%a;%b;\"><!ENTITY g \"%c;%a;\">",
        "GE:g=\"xxx\"|PE:a=\"x\"|PE:b=\"x\"|PE:c=\"xx\""
    )]
    public async Task RepeatedReferenceInsideAnEntityValue_IsNotRecursion(string subset, string expected)
    {
        ArgumentNullException.ThrowIfNull(subset);
        var (model, error) = SubsetHarness.ParseSubset(subset);

        await Assert.That(error).IsNull();
        await Assert.That(model).IsEqualTo(expected);
    }
}
