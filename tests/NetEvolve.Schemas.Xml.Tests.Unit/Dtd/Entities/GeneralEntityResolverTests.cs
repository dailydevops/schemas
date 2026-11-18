namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Entities;

using System;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Declarations;
using NetEvolve.Schemas.Xml.Dtd.Entities;
using NetEvolve.Schemas.Xml.Dtd.Model;
using NetEvolve.Schemas.Xml.Reader.EntityInput;

/// <summary>The lookup and the constraints of section 4.1 and 4.4 over a declaration model, without a reader.</summary>
public class GeneralEntityResolverTests
{
    private const string Subset =
        "<!ENTITY i \"text\"><!ENTITY x SYSTEM \"x.xml\"><!ENTITY u SYSTEM \"u.gif\" NDATA gif><!ENTITY % p \"v\">";

    private static GeneralEntityResolver Create(string subset, string systemId = "")
    {
        var info = new XmlDoctypeInfo("doc".AsSpan(), default, systemId.AsSpan(), subset.AsSpan());
        _ = DtdSubsetParser.Parse(in info, XmlReaderLimits.Default, out var declarations);

        return new GeneralEntityResolver(declarations);
    }

    [Test]
    public async Task Resolve_InternalEntity_ReturnsItsDeclaration()
    {
        var resolver = Create(Subset);

        var error = resolver.Resolve("i".AsSpan(), false, false, out var entity);

        await Assert.That(error).IsNull();
        await Assert.That(entity!.Value).IsEqualTo("text");
    }

    [Test]
    public async Task Resolve_NameIsASliceOfALongerSpan_FindsTheEntity()
    {
        var resolver = Create(Subset);
        const string reference = "&i;";

        var error = resolver.Resolve(reference.AsSpan(1, 1), true, false, out var entity);

        await Assert.That(error).IsNull();
        await Assert.That(entity!.Name).IsEqualTo("i");
    }

    [Test]
    [Arguments("x", false, "XML2002")]
    [Arguments("x", true, "XML0022")]
    [Arguments("u", false, "XML0021")]
    [Arguments("u", true, "XML0021")]
    [Arguments("missing", false, "XML0008")]
    [Arguments("p", false, "XML0008")]
    [Arguments("ii", true, "XML0008")]
    public async Task Resolve_ConstraintViolation_ReturnsItsCode(string name, bool inAttributeValue, string alias)
    {
        ArgumentNullException.ThrowIfNull(name);
        var resolver = Create(Subset);

        var error = resolver.Resolve(name.AsSpan(), inAttributeValue, false, out var entity);

        await Assert.That(error?.Alias).IsEqualTo(alias);
        await Assert.That(entity).IsNull();
    }

    [Test]
    public async Task Resolve_WithoutDeclarations_ReportsEntityDeclared()
    {
        var resolver = new GeneralEntityResolver(null);

        var error = resolver.Resolve("e".AsSpan(), false, false, out var entity);

        await Assert.That(error?.Alias).IsEqualTo("XML0008");
        await Assert.That(entity).IsNull();
    }

    [Test]
    [Arguments("", "", false, true)]
    [Arguments("", "a.dtd", false, false)]
    [Arguments("", "a.dtd", true, true)]
    [Arguments("<!ENTITY % p \"\">%p;", "", false, false)]
    [Arguments("<!ENTITY % p \"\">%p;", "", true, true)]
    [Arguments("<!ENTITY e \"v\">", "", false, true)]
    public async Task Resolve_UndeclaredEntity_IsFatalOnlyWhenNoOtherDeclarationCanExist(
        string subset,
        string systemId,
        bool standalone,
        bool fatal
    )
    {
        ArgumentNullException.ThrowIfNull(subset);
        ArgumentNullException.ThrowIfNull(systemId);
        var resolver = Create(subset, systemId);

        var error = resolver.Resolve("undeclared".AsSpan(), false, standalone, out var entity);

        await Assert.That(error?.Alias).IsEqualTo(fatal ? "XML0008" : null);
        await Assert.That(entity).IsNull();
    }

    [Test]
    public async Task Stack_NullArguments_AreRejected()
    {
        var handler = new DtdDoctypeHandler(XmlReaderLimits.Default);

        await Assert.That(() => new XmlEntityInputStack(null!, handler)).Throws<ArgumentNullException>();
        await Assert
            .That(() => new XmlEntityInputStack(XmlReaderLimits.Default, null!))
            .Throws<ArgumentNullException>();
    }
}
