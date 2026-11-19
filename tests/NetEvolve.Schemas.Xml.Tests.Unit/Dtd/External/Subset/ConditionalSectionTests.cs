namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.Subset;

using NetEvolve.Schemas.Xml.Contracts;

/// <summary>Section 3.4: INCLUDE and IGNORE sections in the external subset.</summary>
public class ConditionalSectionTests
{
    [Test]
    [Arguments("<![ INCLUDE [ <!ELEMENT a EMPTY> ]]>", "E:a:EMPTY")]
    [Arguments("<![INCLUDE[<!ELEMENT a EMPTY>]]>", "E:a:EMPTY")]
    [Arguments("<![ IGNORE [ <!ELEMENT a EMPTY> ]]><!ELEMENT b ANY>", "E:b:ANY")]
    [Arguments("<![IGNORE[]]><!ELEMENT b ANY>", "E:b:ANY")]
    [Arguments(
        "<!ELEMENT a EMPTY>\n<![ INCLUDE [\n<!ELEMENT b ANY>\n]]>\n<!ELEMENT c ANY>",
        "E:a:EMPTY|E:b:ANY|E:c:ANY"
    )]
    public async Task Section_IncludesOrIgnoresItsDeclarations(string subset, string expected)
    {
        ArgumentNullException.ThrowIfNull(subset);
        var (model, error) = SubsetHarness.ParseSubset(subset);

        await Assert.That(error).IsNull();
        await Assert.That(model).IsEqualTo(expected);
    }

    [Test]
    [Arguments(
        "<![ INCLUDE [ <![ INCLUDE [ <!ELEMENT a EMPTY> ]]> <![ IGNORE [ <!ELEMENT b EMPTY> ]]> ]]>",
        "E:a:EMPTY"
    )]
    [Arguments("<![ IGNORE [ <![ INCLUDE [ <!ELEMENT a EMPTY> ]]> <!ELEMENT b EMPTY> ]]><!ELEMENT c ANY>", "E:c:ANY")]
    [Arguments("<![ IGNORE [ <![ IGNORE [ <![ INCLUDE [ ]]> ]]> <!ELEMENT b EMPTY> ]]><!ELEMENT c ANY>", "E:c:ANY")]
    [Arguments("<![ INCLUDE [ <![ IGNORE [ ]]> <!ELEMENT a EMPTY> ]]>", "E:a:EMPTY")]
    public async Task NestedSections_AreBalanced(string subset, string expected)
    {
        ArgumentNullException.ThrowIfNull(subset);
        var (model, error) = SubsetHarness.ParseSubset(subset);

        await Assert.That(error).IsNull();
        await Assert.That(model).IsEqualTo(expected);
    }

    [Test]
    public async Task IgnoredSection_IsNotParsed_SoItsContentMayBeAnything()
    {
        var (model, error) = SubsetHarness.ParseSubset(
            "<![ IGNORE [ <!ELEMENT> %undeclared; <!bogus ]]><!ELEMENT a ANY>"
        );

        await Assert.That(error).IsNull();
        await Assert.That(model).IsEqualTo("E:a:ANY");
    }

    [Test]
    [Arguments("INCLUDE", "E:a:EMPTY|PE:draft=\"INCLUDE\"")]
    [Arguments("IGNORE", "PE:draft=\"IGNORE\"")]
    [Arguments(" INCLUDE ", "E:a:EMPTY|PE:draft=\" INCLUDE \"")]
    public async Task Keyword_SuppliedByAParameterEntity_SelectsTheSection(string keyword, string expected)
    {
        ArgumentNullException.ThrowIfNull(keyword);
        var (model, error) = SubsetHarness.ParseSubset(
            $"<!ENTITY % draft \"{keyword}\"><![ %draft; [ <!ELEMENT a EMPTY> ]]>"
        );

        await Assert.That(error).IsNull();
        await Assert.That(model).IsEqualTo(expected);
    }

    [Test]
    public async Task Keyword_SuppliedByAParameterEntityOfTheInternalSubset_SelectsTheSectionOfTheExternalSubset()
    {
        var (model, error) = SubsetHarness.Parse(
            "<!ENTITY % draft \"IGNORE\"><!ENTITY % final \"INCLUDE\">",
            SubsetHarness.Serving("<![%draft;[<!ELEMENT a EMPTY>]]><![%final;[<!ELEMENT b EMPTY>]]>")
        );

        await Assert.That(error).IsNull();
        await Assert.That(model).IsEqualTo("E:b:EMPTY|PE:draft=\"IGNORE\"|PE:final=\"INCLUDE\"");
    }

    [Test]
    public async Task Section_CompleteInsideAParameterEntityBetweenDeclarations_IsAccepted()
    {
        var (model, error) = SubsetHarness.ParseSubset("<!ENTITY % d \"<![ INCLUDE [ <!ELEMENT a EMPTY> ]]>\"> %d;");

        await Assert.That(error).IsNull();
        await Assert.That(model).IsEqualTo("E:a:EMPTY|PE:d=\"<![ INCLUDE [ <!ELEMENT a EMPTY> ]]>\"");
    }

    [Test]
    [Arguments("<![ INCLUDE [ <!ELEMENT a EMPTY>")]
    [Arguments("<![ IGNORE [ <!ELEMENT a EMPTY>")]
    [Arguments("<![ INCLUDE [ <!ELEMENT a EMPTY> ]>")]
    [Arguments("<![ INCLUDE [ <![ INCLUDE [ <!ELEMENT a EMPTY> ]]>")]
    [Arguments("<![ IGNORE [ <![ INCLUDE [ ]]>")]
    [Arguments("<![INCLUDE <!ELEMENT a EMPTY> ]]>")]
    [Arguments("<![ MAYBE [ ]]>")]
    [Arguments("<![ INCLUDE ]]>")]
    [Arguments("<![ INCLUDE [ <!ELEMENT a EMPTY> ]] >")]
    [Arguments("]]>")]
    [Arguments("<!ELEMENT a EMPTY> ]]>")]
    public async Task MalformedOrUnterminatedSection_RaisesXml0011(string subset)
    {
        ArgumentNullException.ThrowIfNull(subset);
        var (_, error) = SubsetHarness.ParseSubset(subset);

        await Assert.That(error).IsSameReferenceAs(XmlErrorCodes.XML0011);
    }

    [Test]
    public async Task Section_OpenedInAParameterEntityAndClosedOutsideIt_RaisesXml0011()
    {
        var (_, error) = SubsetHarness.ParseSubset("<!ENTITY % open \"<![ INCLUDE [ <!ELEMENT a EMPTY>\"> %open; ]]>");

        await Assert.That(error).IsSameReferenceAs(XmlErrorCodes.XML0011);
    }

    [Test]
    public async Task Section_InTheInternalSubset_RaisesXml0011()
    {
        var (_, error) = SubsetHarness.Parse(
            "<![ INCLUDE [ <!ELEMENT a EMPTY> ]]>",
            SubsetHarness.Serving(string.Empty)
        );

        await Assert.That(error).IsSameReferenceAs(XmlErrorCodes.XML0011);
    }

    [Test]
    public async Task NestedIncludeSections_BeyondTheNestingLimit_RaiseXml1102()
    {
        var limits = XmlReaderLimits.Default.WithMaxElementDepth(2);
        var nested = "<![ INCLUDE [ <![ INCLUDE [ <![ INCLUDE [ <!ELEMENT a EMPTY> ]]> ]]> ]]>";

        var (_, error) = SubsetHarness.ParseSubset(nested, limits);
        var (_, atTheLimit) = SubsetHarness.ParseSubset(
            "<![ INCLUDE [ <![ INCLUDE [ <!ELEMENT a EMPTY> ]]> ]]>",
            limits
        );

        await Assert.That(error).IsSameReferenceAs(XmlErrorCodes.XML1102);
        await Assert.That(atTheLimit).IsNull();
    }
}
