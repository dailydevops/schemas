namespace NetEvolve.Schemas.Xsd.Tests.Unit.Derivation.Particles10;

using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Derivation.Particles10;
using NetEvolve.Schemas.Xsd.Derivation.Types;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Elements;
using NetEvolve.Schemas.Xsd.IdentityConstraints.Evaluation;
using NetEvolve.Schemas.Xsd.Validation.Core;
using NetEvolve.Schemas.Xsd.Wildcards.Core;

/// <summary>Builds particles over real element declarations, wildcards, and model groups for the particle restriction tests.</summary>
internal static class ParticleKit
{
    public const string Tns = "urn:test";
    public const long Unbounded = Particle.Unbounded;

    public static DocumentLocation At(int line) => new("file:///particles.xsd", line, 1);

    public static ValueSpace Values(XsdVersion version) => new(new DescriptorValueAssessor(version), version);

    public static ParticleRestriction Checker() => new(new TypeDerivationOk(XsdVersion.V10), Values(XsdVersion.V10));

    public static ElementDeclaration Declaration(
        string name,
        int line = 1,
        bool global = false,
        bool nillable = false,
        ElementValueConstraintKind constraint = ElementValueConstraintKind.None,
        string value = "",
        XsdDerivationMethod block = XsdDerivationMethod.None,
        IXsdComponent? type = null,
        string ns = Tns,
        bool unresolvedType = false,
        ISchemaSyntaxElement? inlineType = null
    )
    {
        var declaration = ElementDeclaration.Create(
            new XPathQName(ns, name),
            At(line),
            global,
            type is null && !unresolvedType ? default : new XPathQName(Tns, "type"),
            inlineType,
            nillable,
            new ElementValueConstraint(constraint, value),
            false,
            block,
            XsdDerivationMethod.None
        );
        if (type is not null)
        {
            declaration.Bind(0, type);
        }

        return declaration;
    }

    public static Particle Of(ElementDeclaration declaration, long min = 1, long max = 1, int line = 1) =>
        Particle.Create(At(line), min, max, declaration);

    public static Particle El(string name, long min = 1, long max = 1, int line = 1, string ns = Tns) =>
        Of(Declaration(name, line, ns: ns), min, max, line);

    public static Particle Any(
        NamespaceConstraint? constraint = null,
        XsdProcessContents process = XsdProcessContents.Lax,
        long min = 1,
        long max = 1,
        int line = 1
    ) => Particle.Create(At(line), min, max, Wildcard.Create(constraint ?? NamespaceConstraint.Any, process, At(line)));

    public static NamespaceConstraint Only(params string[] namespaces) => NamespaceConstraint.Set(namespaces);

    public static Particle Group(
        ModelGroupCompositor compositor,
        Particle[] members,
        long min = 1,
        long max = 1,
        int line = 1
    ) => Particle.Create(At(line), min, max, ModelGroup.Create(compositor, At(line), members));

    public static Particle Seq(Particle[] members, long min = 1, long max = 1, int line = 1) =>
        Group(ModelGroupCompositor.Sequence, members, min, max, line);

    public static Particle Cho(Particle[] members, long min = 1, long max = 1, int line = 1) =>
        Group(ModelGroupCompositor.Choice, members, min, max, line);

    public static Particle All(Particle[] members, long min = 1, long max = 1, int line = 1) =>
        Group(ModelGroupCompositor.All, members, min, max, line);

    /// <summary>A particle that refers to a model group definition whose group is built from the members.</summary>
    public static Particle Ref(ModelGroupCompositor compositor, Particle[] members, int line = 1)
    {
        var group = ModelGroup.Create(compositor, At(line), members);
        var definition = ModelGroupDefinition.Create(new XPathQName(Tns, "g"), At(line), group);
        return Particle.Create(At(line), 1, 1, definition);
    }

    public static async Task AssertValid(ParticleViolation violation)
    {
        await Assert.That(violation.IsViolation).IsFalse();
        await Assert.That(violation.Message).IsNull();
    }

    public static async Task AssertViolation(ParticleViolation violation, DiagnosticCode code, string clause, int line)
    {
        await Assert.That(violation.IsViolation).IsTrue();
        await Assert.That(violation.Code).IsEqualTo(code);
        await Assert.That(violation.Clause).IsEqualTo(clause);
        await Assert.That(violation.Location.Line).IsEqualTo(line);
        await Assert.That(violation.Message).IsNotEmpty();
    }
}
