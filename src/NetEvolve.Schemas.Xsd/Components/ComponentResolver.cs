namespace NetEvolve.Schemas.Xsd.Components;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;

/// <summary>
/// Binds the by-name references of the registered components, reports missing components (<c>src-resolve</c>), and
/// reports circular type, model group, and attribute group definitions.
/// </summary>
/// <remarks>
/// Every component is registered before this runs, so forward references resolve. Circular attribute group references are
/// a schema error in XSD 1.0 only (<c>src-attribute_group.3</c>); XSD 1.1 allows them (Structures 1.1, appendix G.1.11, design #146 DP-15).
/// </remarks>
internal sealed class ComponentResolver : IComponentResolver
{
    private const int InProgress = 1;
    private const int Done = 2;

    private readonly XsdVersion _version;

    /// <summary>Gets or sets the namespaces each document may refer to, <see langword="null"/> for no restriction.</summary>
    internal ImportScope? Imports { get; set; }

    internal ComponentResolver(XsdVersion version)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan((int)version, (int)XsdVersion.V10);
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)version, (int)XsdVersion.V11);

        _version = version;
    }

    /// <inheritdoc />
    /// <exception cref="NotSupportedException"><paramref name="registry"/> is not a <see cref="ComponentRegistry"/>, which is the only one that can be enumerated.</exception>
    public void Resolve(IComponentRegistry registry, XsdIssueReporter reporter)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(reporter);

        var concrete =
            registry as ComponentRegistry
            ?? throw new NotSupportedException("Only a ComponentRegistry can be resolved.");
        var components = concrete.Components;
        for (var i = 0; i < components.Count; i++)
        {
            if (components[i] is IReferencingComponent referencing)
            {
                Bind(concrete, referencing, reporter);
            }
        }

        var states = new Dictionary<IXsdComponent, int>();
        for (var i = 0; i < components.Count; i++)
        {
            if (components[i] is IReferencingComponent root && !states.ContainsKey(root))
            {
                FindCycles(concrete, root, states, reporter);
            }
        }
    }

    private void Bind(ComponentRegistry registry, IReferencingComponent component, XsdIssueReporter reporter)
    {
        for (var i = 0; i < component.ReferenceCount; i++)
        {
            var reference = component.GetReference(i);
            if (TryResolve(registry, reference, reporter, out var target))
            {
                component.Bind(i, target!);
            }
        }
    }

    /// <summary>Looks a reference up and reports it when the component is missing or its namespace is not imported by the referencing document.</summary>
    /// <param name="registry">The registry.</param>
    /// <param name="reference">The reference.</param>
    /// <param name="reporter">The reporter.</param>
    /// <param name="target">The component when resolved.</param>
    /// <returns><see langword="true"/> when the reference resolved.</returns>
    internal bool TryResolve(
        ComponentRegistry registry,
        in XsdReference reference,
        XsdIssueReporter reporter,
        out IXsdComponent? target
    )
    {
        if (
            Imports?.IsVisible(reference.Location, reference.Name) != false
            && registry.TryGet(reference.Space, reference.Name, out target)
        )
        {
            return true;
        }

        target = null;
        reporter.Report(
            XsdRuleLookup.SrcResolve,
            reference.Location,
            $"No {reference.Space} component named '{reference.Name}' is declared or its namespace is not imported."
        );
        return false;
    }

    // Iterative depth-first search with an explicit stack: a long derivation chain never overflows the call stack.
    private void FindCycles(
        ComponentRegistry registry,
        IReferencingComponent root,
        Dictionary<IXsdComponent, int> states,
        XsdIssueReporter reporter
    )
    {
        var path = new List<IReferencingComponent> { root };
        var next = new List<int> { 0 };
        states[root] = InProgress;
        while (path.Count > 0)
        {
            var top = path.Count - 1;
            var component = path[top];
            if (next[top] >= component.ReferenceCount)
            {
                states[component] = Done;
                path.RemoveAt(top);
                next.RemoveAt(top);
                continue;
            }

            var reference = component.GetReference(next[top]++);
            if (
                !reference.IsStructural
                || (reference.Space == XsdSymbolSpace.AttributeGroups && _version == XsdVersion.V11)
                || !registry.TryGet(reference.Space, reference.Name, out var found)
                || found is not IReferencingComponent target
            )
            {
                continue;
            }

            if (!states.TryGetValue(target, out var state))
            {
                states[target] = InProgress;
                path.Add(target);
                next.Add(0);
            }
            else if (state == InProgress)
            {
                ReportCycle(target, reporter);
            }
        }
    }

    private static void ReportCycle(IReferencingComponent component, XsdIssueReporter reporter)
    {
        var (rule, clause) = component.Kind switch
        {
            XsdComponentKind.SimpleType => (XsdRuleLookup.StPropsCorrect, "st-props-correct.2"),
            XsdComponentKind.ComplexType => (XsdRuleLookup.CtPropsCorrect, "ct-props-correct.3"),
            XsdComponentKind.ModelGroupDefinition => (XsdRuleLookup.MgPropsCorrect, "mg-props-correct.2"),
            XsdComponentKind.AttributeGroup => (XsdRuleLookup.SrcAttributeGroup, "src-attribute_group.3"),
            _ => (XsdRuleLookup.SchPropsCorrect, "sch-props-correct"),
        };
        reporter.Report(
            rule,
            component.Location,
            $"The definition of '{component.Name}' is circular: it refers to itself directly or through other definitions.",
            clause
        );
    }
}
