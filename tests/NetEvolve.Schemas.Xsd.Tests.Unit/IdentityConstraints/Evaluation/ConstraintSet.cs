namespace NetEvolve.Schemas.Xsd.Tests.Unit.IdentityConstraints.Evaluation;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Attributes.Declarations;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Elements;
using NetEvolve.Schemas.Xsd.IdentityConstraints.Definitions;
using NetEvolve.Schemas.Xsd.IdentityConstraints.Evaluation;
using NetEvolve.Schemas.Xsd.SchemaDocuments;
using NetEvolve.Schemas.Xsd.SimpleTypes.Definitions;
using NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;
using NetEvolve.Schemas.Xsd.Validation.Core;

/// <summary>Collects the identity constraints of a test, parsed from schema text, by the name of the declaring element.</summary>
internal sealed class ConstraintSet
{
    private const string Header =
        "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" xmlns:p=\"urn:t\" xmlns:t=\"urn:t\" targetNamespace=\"urn:t\">\n";

    private static readonly SchemaNamespaceScope Scope = new("urn:t", false);

    private readonly FakeComponentRegistry _registry = new([]);
    private readonly ValidationIssueCollector _collector = new(ValidationLimits.Default);
    private readonly List<IdentityConstraintDefinition> _all = [];
    private readonly Dictionary<string, List<IdentityConstraintDefinition>> _byElement = new(StringComparer.Ordinal);

    public int SchemaIssues => _collector.Count;

    /// <summary>Declares the constraints, given as schema text, on the element of that local name.</summary>
    public ConstraintSet On(string element, params string[] constraints)
    {
        var text = Header + "<xs:element name=\"x\">\n" + string.Concat(constraints) + "\n</xs:element>\n</xs:schema>";
        var reporter = new XsdIssueReporter(_collector);
        var document = SchemaDocumentParser.Parse(text.AsSpan(), "file:///t.xsd", reporter);
        var declaration = document.Root!.GetChildNode(0);
        if (!_byElement.TryGetValue(element, out var list))
        {
            list = [];
            _byElement.Add(element, list);
        }

        for (var i = 0; i < declaration.ChildCount; i++)
        {
            var definition = IdentityConstraintDefinition.Create(
                declaration.GetChildNode(i),
                Scope,
                _registry,
                reporter
            );
            if (definition is not null)
            {
                list.Add(definition);
                _all.Add(definition);
            }
        }

        return this;
    }

    public IdentityConstraintListener Listener(
        XsdVersion version = XsdVersion.V11,
        ISimpleValueAssessor? assessor = null
    )
    {
        var reporter = new XsdIssueReporter(_collector);
        foreach (var definition in _all)
        {
            definition.ResolveRefer(_registry, reporter);
        }

        return new IdentityConstraintListener(assessor ?? new ScriptedAssessor(), version, declaration =>
            _byElement.TryGetValue(declaration.Name.LocalName, out var list) ? list : null
        );
    }
}
