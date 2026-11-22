namespace NetEvolve.Schemas.Tests.LegacyCompiler.Schematron;

using Microsoft.CodeAnalysis.CSharp;

/// <summary>
/// Compiled with C# 13 (no extension member syntax): the Schematron entry point is reached through the named static class.
/// A C# 14 compilation of the same call and a C# 13 compilation of the extension member call run in memory through Roslyn,
/// so one project proves both paths and the build failure of a renamed or non-public class.
/// </summary>
public class SchematronLegacyTests
{
    private const string Schema = """
        <sch:schema xmlns:sch="http://purl.oclc.org/dsdl/schematron">
          <sch:pattern><sch:rule context="order"><sch:assert test="@total">An order needs a total.</sch:assert></sch:rule></sch:pattern>
        </sch:schema>
        """;

    private const string ExtensionMemberSource = """
        using NetEvolve.Schemas;

        public static class Probe
        {
            public static SchematronValidatorBuilder Default(string schema) => SchemaValidator.ForSchematron(schema);

            public static SchematronValidatorBuilder Configured(string schema) =>
                SchemaValidator.ForSchematron(schema, "main.sch").WithPhase("p1").WithParameter("k", "v");
        }
        """;

    private const string NamedClassSource = """
        using NetEvolve.Schemas;

        public static class Probe
        {
            public static SchematronValidatorBuilder Default(string schema) =>
                SchematronSchemaValidatorExtensions.ForSchematron(schema);
        }
        """;

    private const string RenamedClassSource = """
        using NetEvolve.Schemas;

        public static class Probe
        {
            public static SchematronValidatorBuilder Default(string schema) =>
                SchematronSchemaValidatorExtensionsRenamed.ForSchematron(schema);
        }
        """;

    [Test]
    public async Task ForSchematron_NamedClass_ReturnsTheBuilderWithDefaults()
    {
        var builder = SchematronSchemaValidatorExtensions.ForSchematron(Schema);

        await Assert.That(builder).IsTypeOf<SchematronValidatorBuilder>();
        await Assert.That(builder.Phase).IsEqualTo("#DEFAULT");
        await Assert.That(builder.Parameters).IsEmpty();
        await Assert.That(builder.Limits).IsSameReferenceAs(ValidationLimits.Default);
        await Assert.That(builder.Resolver).IsSameReferenceAs(DenyAllResourceResolver.Instance);
    }

    [Test]
    public async Task ForSchematron_ExtensionMemberCompiledWithCSharp14_GivesTheSameBuilderTypeAndDefaults()
    {
        var compilation = ConsumerCompiler.Compile(ExtensionMemberSource, LanguageVersion.CSharp14);
        var viaMember = (SchematronValidatorBuilder)compilation.Invoke("Default", Schema);
        var viaClass = SchematronSchemaValidatorExtensions.ForSchematron(Schema);

        await Assert.That(compilation.Errors).IsEmpty();
        await Assert.That(viaMember.GetType()).IsEqualTo(viaClass.GetType());
        await Assert.That(viaMember.Phase).IsEqualTo(viaClass.Phase);
        await Assert.That(viaMember.Parameters).IsEquivalentTo(viaClass.Parameters);
        await Assert.That(viaMember.Limits).IsSameReferenceAs(viaClass.Limits);
        await Assert.That(viaMember.Resolver).IsSameReferenceAs(viaClass.Resolver);
    }

    [Test]
    public async Task ForSchematron_BothPaths_GiveEqualOptionsForTheSameInput()
    {
        var compilation = ConsumerCompiler.Compile(ExtensionMemberSource, LanguageVersion.CSharp14);
        var viaMember = (SchematronValidatorBuilder)compilation.Invoke("Configured", Schema);
        var viaClass = SchematronSchemaValidatorExtensions
            .ForSchematron(Schema, "main.sch")
            .WithPhase("p1")
            .WithParameter("k", "v");

        await Assert.That(viaMember.Phase).IsEqualTo("p1");
        await Assert.That(viaMember.Phase).IsEqualTo(viaClass.Phase);
        await Assert.That(viaMember.Parameters).IsEquivalentTo(viaClass.Parameters);
        await Assert.That(viaMember.Limits).IsSameReferenceAs(viaClass.Limits);
        await Assert.That(viaMember.Resolver).IsSameReferenceAs(viaClass.Resolver);
    }

    [Test]
    public async Task ForSchematron_ExtensionMemberCompiledWithCSharp13_DoesNotCompile()
    {
        var compilation = ConsumerCompiler.Compile(ExtensionMemberSource, LanguageVersion.CSharp13);

        await Assert.That(compilation.Errors).Contains("CS0117");
    }

    [Test]
    public async Task ForSchematron_NamedClassFromAnotherAssemblyCompiledWithCSharp13_Compiles()
    {
        var compilation = ConsumerCompiler.Compile(NamedClassSource, LanguageVersion.CSharp13);
        var builder = (SchematronValidatorBuilder)compilation.Invoke("Default", Schema);

        await Assert.That(compilation.Errors).IsEmpty();
        await Assert.That(builder).IsTypeOf<SchematronValidatorBuilder>();
    }

    [Test]
    public async Task ForSchematron_RenamedNamedClass_FailsTheBuild()
    {
        var compilation = ConsumerCompiler.Compile(RenamedClassSource, LanguageVersion.CSharp13);

        await Assert.That(compilation.Errors).Contains("CS0103");
    }

    [Test]
    public async Task NamedExtensionClass_IsPublicStaticInEntryPointNamespace()
    {
        var type = typeof(SchematronSchemaValidatorExtensions);

        await Assert.That(type.IsPublic).IsTrue();
        await Assert.That(type.IsAbstract && type.IsSealed).IsTrue();
        await Assert.That(type.Namespace).IsEqualTo("NetEvolve.Schemas");
        await Assert.That(type.Name).IsEqualTo("SchematronSchemaValidatorExtensions");
    }
}
