namespace NetEvolve.Schemas.Xsd.Elements;

using System;
using System.Collections.Generic;
using System.Globalization;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.SchemaDocuments;
using NetEvolve.Schemas.Xsd.Wildcards.Core;
using NetEvolve.Schemas.Xsd.Wildcards.Negation;

/// <summary>
/// Builds the model groups and particles of the XML representation (Structures sections 3.8.2 and 3.9.2): <c>sequence</c>,
/// <c>choice</c>, <c>all</c>, local and referenced <c>element</c>, <see langword="group"/> references, and <c>any</c>.
/// </summary>
/// <remarks>
/// An <c>any</c> with <c>notNamespace</c> or <c>notQName</c> (XSD 1.1) is a <c>NegatedWildcard</c>; the validator binds
/// <c>##defined</c> and <c>##definedSibling</c>.
/// Limitations: the restrictions of the <c>all</c> group (XSD-60 #279) are not checked.
/// </remarks>
internal sealed class ParticleBuilder
{
    private readonly SchemaNamespaceScope _scope;
    private readonly ElementDeclarationBuilder _elements;

    /// <summary>Initializes a new instance of the <see cref="ParticleBuilder"/> class.</summary>
    /// <param name="scope">The namespace the schema document contributes to.</param>
    internal ParticleBuilder(SchemaNamespaceScope scope)
    {
        _scope = scope;
        _elements = new ElementDeclarationBuilder(scope);
    }

    /// <summary>Builds the model group of a <c>sequence</c>, <c>choice</c>, or <c>all</c> node.</summary>
    /// <param name="node">The compositor syntax.</param>
    /// <param name="reporter">The reporter.</param>
    /// <returns>The model group.</returns>
    internal ModelGroup BuildGroup(SchemaSyntaxNode node, XsdIssueReporter reporter)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(reporter);

        var particles = new List<Particle>(node.ChildCount);
        for (var i = 0; i < node.ChildCount; i++)
        {
            var particle = BuildParticle(node.GetChildNode(i), reporter);
            if (particle is not null)
            {
                particles.Add(particle);
            }
        }

        return ModelGroup.Create(CompositorOf(node), node.Location, particles);
    }

    /// <summary>Tells whether a local name is a compositor.</summary>
    /// <param name="localName">The local name.</param>
    /// <returns><see langword="true"/> for <c>sequence</c>, <c>choice</c>, and <c>all</c>.</returns>
    internal static bool IsCompositor(string localName) => localName is "sequence" or "choice" or "all";

    private static ModelGroupCompositor CompositorOf(SchemaSyntaxNode node) =>
        node.Name.LocalName switch
        {
            "choice" => ModelGroupCompositor.Choice,
            "all" => ModelGroupCompositor.All,
            _ => ModelGroupCompositor.Sequence,
        };

    // Returns null for annotations and after an issue that leaves no particle to build.
    internal Particle? BuildParticle(SchemaSyntaxNode node, XsdIssueReporter reporter)
    {
        var local = node.Name.LocalName;
        if (local is not ("element" or "group" or "any") && !IsCompositor(local))
        {
            return null;
        }

        ReadOccurrence(node, reporter, out var min, out var max);
        switch (local)
        {
            case "element":
                return BuildElement(node, reporter, min, max);
            case "group":
                return BuildGroupReference(node, reporter, min, max);
            case "any":
                if (node.FindAttribute("notNamespace") is not null || node.FindAttribute("notQName") is not null)
                {
                    return NegatedWildcard.TryCreate(node, _scope, false, reporter, out var negated)
                        ? Particle.Create(node.Location, min, max, negated!)
                        : null;
                }

                if (
                    !Wildcard.TryCreate(
                        node.FindAttribute("namespace"),
                        node.FindAttribute("processContents"),
                        _scope.TargetNamespace,
                        node.Location,
                        out var wildcard
                    )
                )
                {
                    reporter.Report(XsdRuleLookup.SrcWildcard, node.Location, "The wildcard has an invalid value.");
                    return null;
                }

                return Particle.Create(node.Location, min, max, wildcard!);
            default:
                return Particle.Create(node.Location, min, max, BuildGroup(node, reporter));
        }
    }

    private Particle? BuildElement(SchemaSyntaxNode node, XsdIssueReporter reporter, long min, long max)
    {
        var reference = node.FindAttribute("ref");
        if (reference is null)
        {
            if (node.FindAttribute("name") is null)
            {
                ElementDeclarationBuilder.ReportElement(
                    reporter,
                    node,
                    "src-element.2.1",
                    "A local element declaration must have a name or a ref."
                );
                return null;
            }

            return Particle.Create(node.Location, min, max, _elements.BuildDeclaration(node, false, reporter));
        }

        if (node.FindAttribute("name") is not null)
        {
            ElementDeclarationBuilder.ReportElement(
                reporter,
                node,
                "src-element.2.1",
                "An element must not have both a name and a ref."
            );
        }

        if (HasDeclarationContent(node))
        {
            ElementDeclarationBuilder.ReportElement(
                reporter,
                node,
                "src-element.2.2",
                "An element with a ref must not have nillable, default, fixed, form, block, type, or an anonymous type."
            );
        }

        return CreateReference(node, reporter, min, max, XsdSymbolSpace.Elements, reference);
    }

    private Particle? BuildGroupReference(SchemaSyntaxNode node, XsdIssueReporter reporter, long min, long max)
    {
        var reference = node.FindAttribute("ref");
        if (reference is null)
        {
            reporter.Report(
                XsdRuleLookup.SrcModelGroupDefn,
                node.Location,
                "A group reference must have a ref attribute.",
                "src-model_group_defn.1"
            );
            return null;
        }

        return CreateReference(node, reporter, min, max, XsdSymbolSpace.Groups, reference);
    }

    private Particle? CreateReference(
        SchemaSyntaxNode node,
        XsdIssueReporter reporter,
        long min,
        long max,
        XsdSymbolSpace space,
        string lexical
    )
    {
        if (_scope.TryResolve(node, lexical, out var name))
        {
            return Particle.CreateReference(node.Location, min, max, space, name);
        }

        reporter.Report(
            XsdRuleLookup.SrcResolve,
            node.Location,
            $"The reference '{lexical}' is not a QName with a declared prefix."
        );
        return null;
    }

    private static bool HasDeclarationContent(SchemaSyntaxNode node)
    {
        foreach (var attribute in new[] { "nillable", "default", "fixed", "form", "block", "type" })
        {
            if (node.FindAttribute(attribute) is not null)
            {
                return true;
            }
        }

        for (var i = 0; i < node.ChildCount; i++)
        {
            if (node.GetChildNode(i).Name.LocalName is not "annotation")
            {
                return true;
            }
        }

        return false;
    }

    private static void ReadOccurrence(SchemaSyntaxNode node, XsdIssueReporter reporter, out long min, out long max)
    {
        min = Parse(node.FindAttribute("minOccurs"), 1);
        max = Parse(node.FindAttribute("maxOccurs"), 1);
        if (min > max)
        {
            reporter.Report(
                XsdRuleLookup.PPropsCorrect,
                node.Location,
                "The minOccurs of a particle must not exceed its maxOccurs.",
                "p-props-correct.2"
            );
        }
    }

    // The grammar guarantees a non-negative integer or "unbounded"; a value above long.MaxValue is read as unbounded.
    private static long Parse(string? value, long fallback) =>
        value is null ? fallback
        : long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number) ? number
        : Particle.Unbounded;
}
