namespace NetEvolve.Schemas.Schematron.Patterns;

using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>The default priority of one pattern alternative (XSLT 2.0, section 6.4 Conflict Resolution for Template Rules).</summary>
/// <remarks>
/// Limitation: the forms with a type name (<c>element(E, T)</c>) or a schema reference (<c>schema-element()</c>), which have
/// priority 0.25, are outside the supported pattern subset and never reach this class.
/// </remarks>
internal static class PatternPriority
{
    /// <summary>Computes the default priority.</summary>
    /// <param name="root">The anchor of the alternative.</param>
    /// <param name="steps">The steps without the <c>//</c> separators.</param>
    /// <returns>The priority.</returns>
    internal static decimal Of(PathRoot root, IReadOnlyList<AxisStep> steps)
    {
        if (steps.Count == 0)
        {
            return -0.5m;
        }

        // Only a single step without predicates and without an anchor has one of the lower priorities.
        if (root != PathRoot.Relative || steps.Count != 1 || steps[0].Predicates.Count > 0)
        {
            return 0.5m;
        }

        return steps[0].Test switch
        {
            NameTest { Kind: NameTestKind.QualifiedName } => 0m,
            NameTest { Kind: NameTestKind.AnyLocalName or NameTestKind.AnyNamespace } => -0.25m,
            ProcessingInstructionTest { Target: not null } => 0m,
            ElementTest { Name: not null } => 0m,
            AttributeTest { Name: not null } => 0m,
            DocumentTest { Element: ElementTest { Name: not null } } => 0m,
            _ => -0.5m,
        };
    }
}
