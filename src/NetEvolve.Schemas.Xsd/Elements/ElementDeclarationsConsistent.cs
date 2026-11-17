namespace NetEvolve.Schemas.Xsd.Elements;

using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Diagnostics;

/// <summary>
/// Element Declarations Consistent (Structures 1.0, section 3.8.6, <c>cos-element-consistent</c>): all element particles of one
/// content model that have the same expanded name must have the same type definition.
/// </summary>
/// <remarks>
/// The walk enters nested model groups and model group definitions, but not the content of an element. Two anonymous types
/// are always different types. A particle whose type is not resolved is skipped, so an unresolved reference causes no second
/// error. Limitations: the substitution group members of a particle (XSD-12 #239) and the XSD 1.1 clause about wildcards and
/// type tables are not compared.
/// </remarks>
internal static class ElementDeclarationsConsistent
{
    /// <summary>Checks a content model and reports each element that disagrees with an earlier one of the same name.</summary>
    /// <param name="modelGroup">The model group of the content model, after the references are resolved.</param>
    /// <param name="reporter">The reporter.</param>
    internal static void Check(ModelGroup modelGroup, XsdIssueReporter reporter)
    {
        Argument.ThrowIfNull(modelGroup);
        Argument.ThrowIfNull(reporter);

        Walk(modelGroup, new Dictionary<XPathQName, object>(), new HashSet<ModelGroup> { modelGroup }, reporter);
    }

    private static void Walk(
        ModelGroup group,
        Dictionary<XPathQName, object> types,
        HashSet<ModelGroup> visited,
        XsdIssueReporter reporter
    )
    {
        for (var i = 0; i < group.Particles.Count; i++)
        {
            var particle = group.Particles[i];
            switch (particle.Term)
            {
                case ElementDeclaration element:
                    Compare(element, particle, types, reporter);
                    break;
                case ModelGroup nested:
                    Walk(nested, types, visited, reporter);
                    break;
                case ModelGroupDefinition definition when visited.Add(definition.ModelGroup):
                    // Each definition is walked once, which also ends a circular definition.
                    Walk(definition.ModelGroup, types, visited, reporter);
                    break;
            }
        }
    }

    private static void Compare(
        ElementDeclaration element,
        Particle particle,
        Dictionary<XPathQName, object> types,
        XsdIssueReporter reporter
    )
    {
        var type = element.TypeIdentity;
        if (type is null)
        {
            return;
        }

        if (!types.TryGetValue(element.Name, out var first))
        {
            types.Add(element.Name, type);
        }
        else if (!ReferenceEquals(first, type))
        {
            reporter.Report(
                XsdRuleLookup.CosElementConsistent,
                particle.Location,
                $"The elements named '{element.Name}' in one content model have different types.",
                "cos-element-consistent"
            );
        }
    }
}
