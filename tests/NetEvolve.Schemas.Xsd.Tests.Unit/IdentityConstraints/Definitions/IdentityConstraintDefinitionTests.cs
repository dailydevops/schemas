namespace NetEvolve.Schemas.Xsd.Tests.Unit.IdentityConstraints.Definitions;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.IdentityConstraints.Definitions;
using NetEvolve.Schemas.Xsd.SchemaDocuments;
using NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;

public class IdentityConstraintDefinitionTests
{
    private const string Header =
        "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" xmlns:p=\"urn:p\" xmlns:t=\"urn:t\" targetNamespace=\"urn:t\">\n";

    private static readonly SchemaNamespaceScope Scope = new("urn:t", false);

    private const string Footer = "\n</xs:schema>";

    private readonly ValidationIssueCollector _collector = new(ValidationLimits.Default);
    private readonly FakeComponentRegistry _registry = new([]);

    private XsdIssueReporter Reporter => new(_collector);

    private static string Schema(string constraints) =>
        Header + "<xs:element name=\"e\" type=\"xs:string\">\n" + constraints + "\n</xs:element>" + Footer;

    private static List<SchemaSyntaxNode> Constraints(string schema)
    {
        var document = SchemaDocumentParser.Parse(
            schema.AsSpan(),
            "file:///t.xsd",
            new XsdIssueReporter(new ValidationIssueCollector(ValidationLimits.Default))
        );
        var element = document.Root!.GetChildNode(0);
        var list = new List<SchemaSyntaxNode>();
        for (var i = 0; i < element.ChildCount; i++)
        {
            list.Add(element.GetChildNode(i));
        }

        return list;
    }

    private IdentityConstraintDefinition? Create(string constraints, int index = 0) =>
        IdentityConstraintDefinition.Create(Constraints(Schema(constraints))[index], Scope, _registry, Reporter);

    private static DiagnosticCode CodeOf(string ruleId)
    {
        foreach (var rule in XsdRuleCatalog.All)
        {
            if (string.Equals(rule.Id, ruleId, StringComparison.Ordinal))
            {
                return rule.Code;
            }
        }

        throw new InvalidOperationException(ruleId);
    }

    private async Task AssertSingleIssue(string ruleId, int line, int column)
    {
        var issues = _collector.ToResult().Issues;
        await Assert.That(issues).Count().IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(CodeOf(ruleId));
        await Assert.That(issues[0].Location.Line).IsEqualTo(line);
        await Assert.That(issues[0].Location.Column).IsEqualTo(column);
    }

    [Test]
    [Arguments("unique", "Unique")]
    [Arguments("key", "Key")]
    public async Task Create_UniqueAndKey_RecordNameSelectorAndOrderedFields(string element, string kind)
    {
        var definition = Create(
            $"<xs:{element} name=\"k\"><xs:selector xpath=\".//p:item|a/b\"/><xs:field xpath=\"@id\"/><xs:field xpath=\"p:code\"/></xs:{element}>"
        );

        await Assert.That(definition).IsNotNull();
        await Assert.That(definition!.Kind).IsEqualTo(XsdComponentKind.IdentityConstraint);
        await Assert.That(definition.ConstraintKind.ToString()).IsEqualTo(kind);
        await Assert.That(definition.Name).IsEqualTo(new XPathQName("urn:t", "k"));
        await Assert.That(definition.Selector).IsEqualTo(".//p:item|a/b");
        await Assert.That(definition.Fields).Count().IsEqualTo(2);
        await Assert.That(definition.Fields[0]).IsEqualTo("@id");
        await Assert.That(definition.Fields[1]).IsEqualTo("p:code");
        await Assert.That(definition.Refer).IsEqualTo(default(XPathQName));
        await Assert.That(_collector.Count).IsEqualTo(0);
        await Assert.That(_registry.TryGet(XsdComponentKind.IdentityConstraint, definition.Name, out _)).IsTrue();
    }

    [Test]
    public async Task Create_Keyref_RecordsTheReferNameInTheSchemaNamespace()
    {
        var definition = Create(
            "<xs:keyref name=\"r\" refer=\"p:k\"><xs:selector xpath=\"a\"/><xs:field xpath=\"@x\"/></xs:keyref>"
        );

        await Assert.That(definition!.ConstraintKind).IsEqualTo(IdentityConstraintKind.Keyref);
        await Assert.That(definition.Refer).IsEqualTo(new XPathQName("urn:p", "k"));
    }

    [Test]
    [Arguments("key")]
    [Arguments("unique")]
    public async Task ResolveRefer_FindsKeyAndUnique(string target)
    {
        var schema = Schema(
            $"<xs:{target} name=\"k\"><xs:selector xpath=\"a\"/><xs:field xpath=\"@x\"/></xs:{target}>"
                + "<xs:keyref name=\"r\" refer=\"t:k\"><xs:selector xpath=\"b\"/><xs:field xpath=\"@y\"/></xs:keyref>"
        );
        var nodes = Constraints(schema);
        var key = IdentityConstraintDefinition.Create(nodes[0], Scope, _registry, Reporter)!;
        var keyref = IdentityConstraintDefinition.Create(nodes[1], Scope, _registry, Reporter)!;

        keyref.ResolveRefer(_registry, Reporter);

        await Assert.That(_collector.Count).IsEqualTo(0);
        await Assert.That(keyref.ReferencedConstraint).IsSameReferenceAs(key);
    }

    [Test]
    public async Task ResolveRefer_Unresolved_IsSchemaErrorWithRuleCode()
    {
        var definition = Create(
            "<xs:keyref name=\"r\" refer=\"missing\"><xs:selector xpath=\"a\"/><xs:field xpath=\"@x\"/></xs:keyref>"
        )!;

        definition.ResolveRefer(_registry, Reporter);

        await Assert.That(definition.ReferencedConstraint).IsNull();
        await AssertSingleIssue("src-resolve", 3, 1);
    }

    [Test]
    public async Task ResolveRefer_ReferToAKeyref_IsSchemaError()
    {
        var schema = Schema(
            "<xs:keyref name=\"a\" refer=\"t:b\"><xs:selector xpath=\"a\"/><xs:field xpath=\"@x\"/></xs:keyref>"
                + "<xs:keyref name=\"b\" refer=\"t:a\"><xs:selector xpath=\"a\"/><xs:field xpath=\"@x\"/></xs:keyref>"
        );
        var nodes = Constraints(schema);
        var first = IdentityConstraintDefinition.Create(nodes[0], Scope, _registry, Reporter)!;
        _ = IdentityConstraintDefinition.Create(nodes[1], Scope, _registry, Reporter);

        first.ResolveRefer(_registry, Reporter);

        await Assert.That(first.ReferencedConstraint).IsNull();
        await AssertSingleIssue("c-props-correct", 3, 1);
    }

    [Test]
    public async Task ResolveRefer_DifferentFieldCount_IsSchemaError()
    {
        var schema = Schema(
            "<xs:key name=\"k\"><xs:selector xpath=\"a\"/><xs:field xpath=\"@x\"/><xs:field xpath=\"@y\"/></xs:key>"
                + "<xs:keyref name=\"r\" refer=\"t:k\"><xs:selector xpath=\"b\"/><xs:field xpath=\"@z\"/></xs:keyref>"
        );
        var nodes = Constraints(schema);
        _ = IdentityConstraintDefinition.Create(nodes[0], Scope, _registry, Reporter);
        var keyref = IdentityConstraintDefinition.Create(nodes[1], Scope, _registry, Reporter)!;

        keyref.ResolveRefer(_registry, Reporter);

        await Assert.That(keyref.ReferencedConstraint).IsNull();
        await AssertSingleIssue("c-props-correct", 3, 95);
    }

    [Test]
    public async Task Create_SelectorWithPredicate_IsSchemaError()
    {
        var definition = Create("<xs:key name=\"k\"><xs:selector xpath=\"a[1]\"/><xs:field xpath=\"@x\"/></xs:key>");

        await Assert.That(definition).IsNull();
        await AssertSingleIssue("c-selector-xpath", 3, 18);
    }

    [Test]
    public async Task Create_FieldWithFunctionCall_IsSchemaError()
    {
        var definition = Create(
            "<xs:key name=\"k\"><xs:selector xpath=\"a\"/><xs:field xpath=\"string(@x)\"/></xs:key>"
        );

        await Assert.That(definition).IsNull();
        await AssertSingleIssue("c-fields-xpaths", 3, 42);
    }

    [Test]
    public async Task Create_FieldWithAxisOutsideTheSubset_IsSchemaError()
    {
        var definition = Create(
            "<xs:key name=\"k\"><xs:selector xpath=\"a\"/><xs:field xpath=\"ancestor::b\"/></xs:key>"
        );

        await Assert.That(definition).IsNull();
        await AssertSingleIssue("c-fields-xpaths", 3, 42);
    }

    [Test]
    public async Task Create_SelectorWithUndeclaredPrefix_IsSchemaError()
    {
        var definition = Create("<xs:key name=\"k\"><xs:selector xpath=\"q:a\"/><xs:field xpath=\"@x\"/></xs:key>");

        await Assert.That(definition).IsNull();
        await AssertSingleIssue("c-selector-xpath", 3, 18);
    }

    [Test]
    public async Task Create_SelectorIsNotAnExpression_IsSchemaError()
    {
        var definition = Create("<xs:key name=\"k\"><xs:selector xpath=\"a/\"/><xs:field xpath=\"@x\"/></xs:key>");

        await Assert.That(definition).IsNull();
        await AssertSingleIssue("c-selector-xpath", 3, 18);
    }

    [Test]
    public async Task Create_SameExpandedNameTwice_IsSchemaError()
    {
        var schema = Schema(
            "<xs:key name=\"k\"><xs:selector xpath=\"a\"/><xs:field xpath=\"@x\"/></xs:key>"
                + "<xs:unique name=\"k\"><xs:selector xpath=\"b\"/><xs:field xpath=\"@y\"/></xs:unique>"
        );
        var nodes = Constraints(schema);

        var first = IdentityConstraintDefinition.Create(nodes[0], Scope, _registry, Reporter);
        var second = IdentityConstraintDefinition.Create(nodes[1], Scope, _registry, Reporter);

        await Assert.That(first).IsNotNull();
        await Assert.That(second).IsNull();
        await AssertSingleIssue("sch-props-correct", 3, 73);
    }

    [Test]
    [Arguments("<xs:key name=\"k\"><xs:field xpath=\"@x\"/></xs:key>")]
    [Arguments("<xs:key name=\"k\"><xs:selector xpath=\"a\"/></xs:key>")]
    [Arguments(
        "<xs:key name=\"k\"><xs:selector xpath=\"a\"/><xs:selector xpath=\"b\"/><xs:field xpath=\"@x\"/></xs:key>"
    )]
    [Arguments("<xs:key><xs:selector xpath=\"a\"/><xs:field xpath=\"@x\"/></xs:key>")]
    public async Task Create_MissingPartsOrName_IsRepresentationError(string constraint)
    {
        var definition = Create(constraint);

        await Assert.That(definition).IsNull();
        await Assert.That(_collector.ToResult().Issues[0].Code).IsEqualTo(CodeOf("src-identity-constraint"));
    }
}
