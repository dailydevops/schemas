namespace NetEvolve.Schemas.Xsd.Components;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;

/// <summary>
/// Binds the by-name references of the registered components, reports missing components (<c>src-resolve</c>), and
/// reports circular type, model group, and attribute group definitions.
/// </summary>
/// <remarks>
/// Every component is registered before this runs, so forward references resolve. Circular attribute group references are
/// a schema error in XSD 1.0 only (<c>ag-props-correct.3</c>); XSD 1.1 allows them (Structures 1.1, appendix G.1.11, design #146 DP-15).
/// </remarks>
internal sealed class ComponentResolver : IComponentResolver
{
    private const int InProgress = 1;
    private const int Done = 2;

    private readonly XsdVersion _version;

    internal ComponentResolver(XsdVersion version)
    {
        Argument.ThrowIfLessThan((int)version, (int)XsdVersion.V10);
        Argument.ThrowIfGreaterThan((int)version, (int)XsdVersion.V11);

        _version = version;
    }

    /// <inheritdoc />
    /// <exception cref="NotSupportedException"><paramref name="registry"/> is not a <see cref="ComponentRegistry"/>, which is the only one that can be enumerated.</exception>
    public void Resolve(IComponentRegistry registry, XsdIssueReporter reporter)
    {
        Argument.ThrowIfNull(registry);
        Argument.ThrowIfNull(reporter);

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

    private static void Bind(ComponentRegistry registry, IReferencingComponent component, XsdIssueReporter reporter)
    {
        for (var i = 0; i < component.ReferenceCount; i++)
        {
            var reference = component.GetReference(i);
            if (registry.TryGet(reference.Space, reference.Name, out var target))
            {
                component.Bind(i, target!);
            }
            else
            {
                reporter.Report(
                    XsdRuleLookup.SrcResolve,
                    reference.Location,
                    $"No {reference.Space} component named '{reference.Name}' is declared."
                );
            }
        }
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
            XsdComponentKind.AttributeGroup => (XsdRuleLookup.AgPropsCorrect, "ag-props-correct.3"),
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
