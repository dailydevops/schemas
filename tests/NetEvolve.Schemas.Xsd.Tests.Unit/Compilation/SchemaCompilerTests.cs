namespace NetEvolve.Schemas.Xsd.Tests.Unit.Compilation;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Compilation;
using NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;
using NetEvolve.Schemas.Xsd.ContentModels.Determinism;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Elements;
using NetEvolve.Schemas.Xsd.SimpleTypes.Definitions;
using NetEvolve.Schemas.Xsd.Tests.Unit.Composition;
using NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;
using static NetEvolve.Schemas.Xsd.Tests.Unit.Compilation.CompilationFixture;

/// <summary>The pipeline turns schema documents into a checked schema set and collects every error with its location and rule code.</summary>
public class SchemaCompilerTests
{
    private const string Other = "urn:b";

    [Test]
    public async Task Compile_MultiDocumentSchema_ContainsTheComponentsOfEveryDocument()
    {
        var resolver = new MapResolver()
            .Add(
                "file:///inc.xsd",
                CompositionFixture.Schema(
                    "<xs:simpleType name=\"code\"><xs:restriction base=\"xs:string\"><xs:maxLength value=\"3\"/></xs:restriction></xs:simpleType>"
                )
            )
            .Add("file:///b.xsd", CompositionFixture.Schema("<xs:element name=\"item\" type=\"xs:int\"/>", Other));
        var root = Schema(
            "<xs:include schemaLocation=\"inc.xsd\"/><xs:import namespace=\"urn:b\" schemaLocation=\"b.xsd\"/>"
                + "<xs:element name=\"root\" type=\"t:rootType\"/>"
                + "<xs:complexType name=\"rootType\"><xs:sequence><xs:element name=\"code\" type=\"t:code\"/></xs:sequence></xs:complexType>"
        );

        var compilation = Compile(root, resolver: resolver);

        await Assert.That(Describe(compilation)).IsEmpty();
        await Assert.That(compilation.IsValid).IsTrue();
        await Assert.That(Get(compilation, XsdComponentKind.ElementDeclaration, "root")).IsTypeOf<ElementDeclaration>();
        await Assert.That(Get(compilation, XsdComponentKind.ComplexType, "rootType")).IsTypeOf<ComplexTypeDefinition>();
        await Assert.That(Get(compilation, XsdComponentKind.SimpleType, "code")).IsTypeOf<SimpleTypeDefinition>();
        await Assert
            .That(compilation.Set!.TryGet(XsdComponentKind.ElementDeclaration, new XPathQName(Other, "item"), out _))
            .IsTrue();
        await Assert
            .That(compilation.Set!.TryGet(XsdComponentKind.SimpleType, new XPathQName(Xs, "string"), out _))
            .IsTrue();
        await Assert.That(compilation.Set!.Version).IsEqualTo(XsdVersion.V10);
    }

    [Test]
    public async Task Compile_ThreeIndependentErrors_ReportsAllThreeWithLocationAndRuleCode()
    {
        var compilation = Compile(
            Schema(
                "<xs:element name=\"a\" type=\"xs:missingType\"/>\n"
                    + "<xs:simpleType name=\"s\"><xs:restriction base=\"xs:int\"><xs:minInclusive value=\"9\"/><xs:maxInclusive value=\"3\"/></xs:restriction></xs:simpleType>\n"
                    + "<xs:complexType name=\"c\"><xs:choice><xs:element name=\"x\" type=\"xs:string\"/><xs:element name=\"x\" type=\"xs:string\"/></xs:choice></xs:complexType>"
            )
        );

        await Assert.That(compilation.IsValid).IsFalse();
        await Assert.That(compilation.Set).IsNull();
        await Assert.That(compilation.Model).IsNull();
        await Assert.That(compilation.Result.Issues.Count).IsEqualTo(3);
        await Assert
            .That(Codes(compilation))
            .IsEquivalentTo([
                XsdRuleCodes.SrcResolve,
                XsdRuleCodes.MinInclusiveLessThanEqualToMaxInclusive,
                XsdRuleCodes.CosNonambig,
            ]);
        foreach (var issue in compilation.Result.Issues)
        {
            await Assert.That(issue.Location.Source).IsEqualTo(RootUri);
            await Assert.That(issue.Location.Line).IsGreaterThan(0);
            await Assert.That(issue.Location.Column).IsGreaterThan(0);
        }
    }

    [Test]
    public async Task Compile_ErrorsOfSeveralPhases_AreAllCollected()
    {
        var compilation = Compile(
            Schema(
                "<xs:element name=\"e\" type=\"xs:int\" default=\"abc\"/>"
                    + "<xs:simpleType name=\"loop\"><xs:restriction base=\"t:loop\"/></xs:simpleType>"
                    + "<xs:element name=\"dup\"/><xs:element name=\"dup\"/>"
            )
        );

        await Assert.That(compilation.IsValid).IsFalse();
        await Assert
            .That(Codes(compilation))
            .IsEquivalentTo([XsdRuleCodes.CosValidDefault, XsdRuleCodes.StPropsCorrect, XsdRuleCodes.SchPropsCorrect]);
    }

    [Test]
    public async Task Steps_AreTheRealImplementations_AndNoFake()
    {
        var steps = Compiler().Steps;
        var library = typeof(SchemaCompiler).Assembly;

        await Assert.That(steps).IsNotEmpty();
        foreach (var step in steps)
        {
            await Assert.That(step.GetType().Assembly).IsEqualTo(library);
            await Assert.That(step.GetType().Name.StartsWith("Fake", StringComparison.Ordinal)).IsFalse();
        }

        await Assert.That(typeof(FakeComponentResolver).Assembly).IsNotEqualTo(library);
        await Assert.That(steps.OfType<IComponentResolver>().Count()).IsEqualTo(1);
        await Assert.That(steps.OfType<IDerivationCheck>().Count()).IsEqualTo(1);
        await Assert.That(steps.OfType<IDeterminismCheck>().Count()).IsEqualTo(1);
        await Assert
            .That(steps.OfType<IComponentBuilder>().Select(b => b.Kind).OrderBy(kind => kind))
            .IsEquivalentTo([
                XsdComponentKind.SimpleType,
                XsdComponentKind.ComplexType,
                XsdComponentKind.AttributeDeclaration,
                XsdComponentKind.ElementDeclaration,
                XsdComponentKind.AttributeGroup,
                XsdComponentKind.ModelGroupDefinition,
            ]);
    }

    [Test]
    public async Task Compile_MalformedRoot_ReportsTheReaderErrorAndNoSet()
    {
        var compilation = Compile("<xs:schema xmlns:xs=\"" + Xs + "\"><xs:element");

        await Assert.That(compilation.IsValid).IsFalse();
        await Assert.That(compilation.Result.Issues.Count).IsEqualTo(1);
        await Assert.That(compilation.Result.Issues[0].Code).IsEqualTo(XsdRuleCodes.SchemaReference);
        await Assert.That(compilation.Result.Issues[0].Location.Source).IsEqualTo(RootUri);
        await Assert.That(compilation.Result.Issues[0].Location.Line).IsEqualTo(1);
    }

    [Test]
    public async Task Compile_Strict10_StopsAfterTheDocumentCheckBeforeComponentsAreBuilt()
    {
        var compilation = Compile(
            Schema(
                "<xs:element name=\"e\"><xs:complexType><xs:assert test=\"true()\"/></xs:complexType></xs:element>"
                    + "<xs:element name=\"f\" type=\"t:missing\"/>"
            ),
            XsdVersion.V10
        );

        await Assert.That(Codes(compilation)).IsEquivalentTo([XsdRuleCodes.Strict10Assert]);
    }

    [Test]
    public async Task Compile_CharsBytesAndMemoryInputs_GiveTheSameSet(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var text = Schema(
            "<xs:element name=\"e\" type=\"xs:int\"/><xs:simpleType name=\"s\"><xs:restriction base=\"xs:int\"/></xs:simpleType>"
        );
        var compiler = Compiler();

        var fromChars = compiler.Compile(text.AsSpan(), RootUri, cancellationToken);
        var fromBytes = compiler.Compile(Encoding.UTF8.GetBytes(text).AsSpan(), RootUri, cancellationToken);
        var fromMemory = compiler.Compile([SchemaInput.FromChars(text.AsMemory(), RootUri)], cancellationToken);
        var fromByteMemory = compiler.Compile(
            [SchemaInput.FromBytes(Encoding.UTF8.GetBytes(text), RootUri)],
            cancellationToken
        );

        foreach (var compilation in new[] { fromChars, fromBytes, fromMemory, fromByteMemory })
        {
            await Assert.That(compilation.IsValid).IsTrue();
            await Assert.That(compilation.Set!.Count).IsEqualTo(fromChars.Set!.Count);
            await Assert.That(compilation.Model).IsNotNull();
        }
    }

    [Test]
    public async Task Compile_SeveralRootDocuments_AreOneSchemaAndADocumentGivenTwiceCountsOnce(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var a = CompositionFixture.Schema("<xs:element name=\"a\" type=\"xs:int\"/>", "urn:a");
        var b = CompositionFixture
            .Schema(
                "<xs:element name=\"b\"><xs:complexType><xs:sequence><xs:element ref=\"a:a\"/></xs:sequence></xs:complexType></xs:element>",
                "urn:b"
            )
            .Replace("<xs:schema ", "<xs:schema xmlns:a=\"urn:a\" ", StringComparison.Ordinal);

        var compilation = Compiler()
            .Compile(
                [
                    SchemaInput.FromChars(a.AsMemory(), "file:///a.xsd"),
                    SchemaInput.FromChars(b.AsMemory(), "file:///b.xsd"),
                    SchemaInput.FromChars(a.AsMemory(), "file:///a.xsd"),
                ],
                cancellationToken
            );

        await Assert.That(Describe(compilation)).IsEmpty();
        await Assert
            .That(compilation.Set!.TryGet(XsdComponentKind.ElementDeclaration, new XPathQName("urn:a", "a"), out _))
            .IsTrue();
        await Assert
            .That(compilation.Set!.TryGet(XsdComponentKind.ElementDeclaration, new XPathQName("urn:b", "b"), out _))
            .IsTrue();
    }

    [Test]
    public async Task Compile_ACanceledToken_Throws()
    {
        using var source = new CancellationTokenSource();
#if NET10_0_OR_GREATER
        await source.CancelAsync();
#else
        source.Cancel();
#endif

        await Assert
            .That(() => Compiler().Compile(Schema(string.Empty).AsSpan(), RootUri, source.Token))
            .Throws<OperationCanceledException>();
    }

    [Test]
    public async Task Compile_NoInput_GivesAnEmptyValidSet(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var compilation = Compiler().Compile(new List<SchemaInput>(), cancellationToken);

        await Assert.That(compilation.IsValid).IsTrue();
        await Assert.That(compilation.Result.Issues).IsEmpty();
    }

    [Test]
    public async Task Constructor_AndCompile_GuardTheirArguments()
    {
        await Assert
            .That(() => new SchemaCompiler(XsdVersion.V10, null!, ValidationLimits.Default))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() =>
                new SchemaCompiler(XsdVersion.V10, NetEvolve.Schemas.Resources.DenyAllResourceResolver.Instance, null!)
            )
            .Throws<ArgumentNullException>();
        await Assert
            .That(() =>
                new SchemaCompiler(
                    (XsdVersion)7,
                    NetEvolve.Schemas.Resources.DenyAllResourceResolver.Instance,
                    ValidationLimits.Default
                )
            )
            .Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => Compiler().Compile((IReadOnlyList<SchemaInput>)null!)).Throws<ArgumentNullException>();
    }

    // An all group that may not occur is dropped from the content type, but cos-all-limited of XSD 1.0 still applies to it.
    [Test]
    [Arguments(XsdVersion.V10, 1)]
    [Arguments(XsdVersion.V11, 0)]
    public async Task Compile_AllGroupThatNeverOccurs_IsAnErrorInXsd10Only(XsdVersion version, int errors)
    {
        var root = Schema(
            "<xs:complexType name=\"foo\"><xs:all minOccurs=\"0\" maxOccurs=\"0\"><xs:element name=\"a\"/></xs:all></xs:complexType>"
        );

        var compilation = Compile(root, version);

        await Assert.That(Codes(compilation).Count(c => c == XsdRuleCodes.CosAllLimited)).IsEqualTo(errors);
    }

    [Test]
    public async Task Steps_ComposeTheParticleRestrictionOfXsd10ForThatVersionOnly()
    {
        await Assert
            .That(
                Compiler(XsdVersion.V10)
                    .Steps.OfType<NetEvolve.Schemas.Xsd.Derivation.Particles10.ParticleRestriction>()
                    .Count()
            )
            .IsEqualTo(1);
        await Assert
            .That(
                Compiler(XsdVersion.V11)
                    .Steps.OfType<NetEvolve.Schemas.Xsd.Derivation.Particles10.ParticleRestriction>()
            )
            .IsEmpty();
    }
}
