namespace NetEvolve.Schemas.Xml.Tests.Unit.Constraints;

using static NetEvolve.Schemas.Xml.Tests.Unit.Constraints.ConstraintHarness;

/// <summary>
/// One violation and one conforming document for each namespace constraint of Namespaces in XML 1.0 (Third Edition), read
/// through the validating reader mode, which always processes namespaces. A violation is a fatal status.
/// </summary>
public class NamespaceConstraintTests
{
    private const string XmlNamespace = "http://www.w3.org/XML/1998/namespace";

    [Test]
    public async Task PrefixDeclared_ElementPrefixWithoutADeclaration_IsFatalXml3001() =>
        await ReportsFatal("<p:r/>", "XML3001");

    [Test]
    public async Task PrefixDeclared_AttributePrefixWithoutADeclaration_IsFatalXml3001() =>
        await ReportsFatal("<r p:a=\"1\"/>", "XML3001");

    [Test]
    public async Task PrefixDeclared_PrefixDeclaredOnTheElement_IsWellFormed() =>
        await IsWellFormed("<p:r xmlns:p=\"u\" p:a=\"1\"/>");

    [Test]
    public async Task ReservedPrefixesAndNamespaceNames_BindingThePrefixXmlToAnotherName_IsFatalXml3002() =>
        await ReportsFatal("<r xmlns:xml=\"u\"/>", "XML3002");

    [Test]
    public async Task ReservedPrefixesAndNamespaceNames_BindingTheXmlNamespaceToAnotherPrefix_IsFatalXml3002() =>
        await ReportsFatal("<r xmlns:p=\"" + XmlNamespace + "\"/>", "XML3002");

    [Test]
    public async Task ReservedPrefixesAndNamespaceNames_BindingThePrefixXmlToItsOwnName_IsWellFormed() =>
        await IsWellFormed("<r xmlns:xml=\"" + XmlNamespace + "\" xml:lang=\"en\"/>");

    [Test]
    public async Task AttributesUnique_TwoPrefixesForTheSameName_IsFatalXml3003() =>
        await ReportsFatal("<r xmlns:a=\"u\" xmlns:b=\"u\" a:x=\"1\" b:x=\"2\"/>", "XML3003");

    [Test]
    public async Task AttributesUnique_TwoPrefixesForDifferentNames_IsWellFormed() =>
        await IsWellFormed("<r xmlns:a=\"u\" xmlns:b=\"v\" a:x=\"1\" b:x=\"2\"/>");

    [Test]
    public async Task NoPrefixUndeclaring_EmptyNameForAPrefix_IsFatalXml3005() =>
        await ReportsFatal("<r xmlns:p=\"\"/>", "XML3005");

    [Test]
    public async Task NoPrefixUndeclaring_EmptyNameForTheDefaultNamespace_IsWellFormed() =>
        await IsWellFormed("<r xmlns=\"u\"><a xmlns=\"\"/></r>");
}
