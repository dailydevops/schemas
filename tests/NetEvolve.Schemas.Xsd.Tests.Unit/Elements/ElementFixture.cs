namespace NetEvolve.Schemas.Xsd.Tests.Unit.Elements;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Elements;
using NetEvolve.Schemas.Xsd.SchemaDocuments;

/// <summary>Parses a schema, builds its top-level elements and groups, and resolves the references.</summary>
internal sealed class ElementFixture
{
    public const string TargetNamespace = "urn:test";

    private ElementFixture(ComponentRegistry registry, ValidationIssueCollector collector, XsdIssueReporter reporter)
    {
        Registry = registry;
        Collector = collector;
        Reporter = reporter;
    }

    public ComponentRegistry Registry { get; }

    public ValidationIssueCollector Collector { get; }

    public XsdIssueReporter Reporter { get; }

    public IReadOnlyList<ValidationIssue> Issues => Collector.ToResult().Issues;

    /// <summary>Builds the schema around <paramref name="body"/>; every top-level element and group becomes a component.</summary>
    public static ElementFixture Build(string body, string schemaAttributes = "")
    {
        var collector = new ValidationIssueCollector(ValidationLimits.Default);
        var reporter = new XsdIssueReporter(collector);
        var registry = new ComponentRegistry(reporter);
        XsdBuiltInTypes.Register(registry, XsdVersion.V11);

        var text =
            "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" xmlns:t=\"urn:test\" targetNamespace=\"urn:test\" "
            + schemaAttributes
            + ">\n"
            + body
            + "\n</xs:schema>";
        var document = SchemaDocumentParser.Parse(text.AsSpan(), "file:///t.xsd", reporter);
        var root = document.Root!;
        var scope = new SchemaNamespaceScope(TargetNamespace, false);
        var elements = new ElementDeclarationBuilder(scope);
        var groups = new ModelGroupDefinitionBuilder(scope);
        for (var i = 0; i < root.ChildCount; i++)
        {
            var child = root.GetChildNode(i);
            if (child.Name.LocalName == "element")
            {
                _ = elements.Build(child, registry, reporter);
            }
            else if (child.Name.LocalName == "group")
            {
                _ = groups.Build(child, registry, reporter);
            }
        }

        return new ElementFixture(registry, collector, reporter);
    }

    public ElementFixture Resolve(XsdVersion version = XsdVersion.V11)
    {
        new ComponentResolver(version).Resolve(Registry, Reporter);
        return this;
    }

    public ElementDeclaration Element(string local) => Get<ElementDeclaration>(XsdComponentKind.ElementDeclaration, local);

    public ModelGroupDefinition Group(string local) =>
        Get<ModelGroupDefinition>(XsdComponentKind.ModelGroupDefinition, local);

    public DiagnosticCode CodeOf(string ruleId)
    {
        foreach (var rule in XsdRuleCatalog.All)
        {
            if (string.Equals(rule.Id, ruleId, StringComparison.Ordinal))
            {
                return rule.Code;
            }
        }

        throw new InvalidOperationException("No rule " + ruleId);
    }

    private T Get<T>(XsdComponentKind kind, string local)
        where T : class, IXsdComponent
    {
        _ = Registry.TryGet(kind, new(TargetNamespace, local), out var component);
        return (T)component!;
    }
}
