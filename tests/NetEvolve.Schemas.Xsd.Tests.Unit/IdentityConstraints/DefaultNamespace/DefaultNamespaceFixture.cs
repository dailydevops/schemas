namespace NetEvolve.Schemas.Xsd.Tests.Unit.IdentityConstraints.DefaultNamespace;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.IdentityConstraints.DefaultNamespace;
using NetEvolve.Schemas.Xsd.IdentityConstraints.Definitions;
using NetEvolve.Schemas.Xsd.IdentityConstraints.Evaluation;
using NetEvolve.Schemas.Xsd.SchemaDocuments;
using NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;
using NetEvolve.Schemas.Xsd.Tests.Unit.IdentityConstraints.Evaluation;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;

/// <summary>
/// Schema documents with one <c>unique</c> constraint on an element <c>x</c> and the evaluation of that constraint against
/// instances of the identity constraint fixture, whose root <c>r</c> carries the constraint.
/// </summary>
internal static class DefaultNamespaceFixture
{
    internal const string Tns = "urn:t";

    /// <summary>Two <c>e</c> elements with the attribute <c>a</c> equal.</summary>
    internal const string EqualAttributes = "<e a=\"1\"/>\n<e a=\"1\"/>";

    /// <summary>Two <c>e</c> elements with the child <c>k</c> equal.</summary>
    internal const string EqualChildren = "<e><k>1</k></e>\n<e><k>1</k></e>";

    internal static readonly SchemaNamespaceScope Scope = new(Tns, false);

    /// <summary>Gets the attribute text for a value, empty for <see langword="null"/>.</summary>
    internal static string Attr(string? value) =>
        value is null ? string.Empty : " xpathDefaultNamespace=\"" + value + "\"";

    /// <summary>A schema document with the given attributes on the schema element and the constraints on the element <c>x</c>.</summary>
    internal static string Schema(string schemaAttributes, string constraints) =>
        "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" targetNamespace=\""
        + Tns
        + "\""
        + schemaAttributes
        + ">\n<xs:element name=\"x\">\n"
        + constraints
        + "\n</xs:element>\n</xs:schema>";

    /// <summary>A <c>unique</c> constraint whose selector and field carry the given attribute values.</summary>
    internal static string Unique(string selector, string? selectorValue, string field, string? fieldValue) =>
        "<xs:unique name=\"u\"><xs:selector xpath=\""
        + selector
        + "\""
        + Attr(selectorValue)
        + "/><xs:field xpath=\""
        + field
        + "\""
        + Attr(fieldValue)
        + "/></xs:unique>";

    internal static SchemaSyntaxNode Parse(string schema) =>
        SchemaDocumentParser
            .Parse(
                schema.AsSpan(),
                "file:///t.xsd",
                new XsdIssueReporter(new ValidationIssueCollector(ValidationLimits.Default))
            )
            .Root!;

    /// <summary>The constraint element of a document made by <see cref="Schema"/>.</summary>
    internal static SchemaSyntaxNode Constraint(string schema) => Parse(schema).GetChildNode(0).GetChildNode(0);

    /// <summary>Runs the constraint of a document: qualifies it, creates the definition, and validates the instance.</summary>
    internal static IReadOnlyList<ValidationIssue> Run(
        string schema,
        string body,
        bool namespaced = true,
        XsdVersion version = XsdVersion.V11
    )
    {
        var collector = new ValidationIssueCollector(ValidationLimits.Default);
        var reporter = new XsdIssueReporter(collector);
        var registry = new FakeComponentRegistry([]);
        var declaration = SchemaDocumentParser.Parse(schema.AsSpan(), "file:///t.xsd", reporter).Root!.GetChildNode(0);
        var definitions = new List<IdentityConstraintDefinition>();
        for (var i = 0; i < declaration.ChildCount; i++)
        {
            var node = declaration.GetChildNode(i);
            XPathDefaultNamespace.Apply(node, Scope, version);
            definitions.Add(IdentityConstraintDefinition.Create(node, Scope, registry, reporter)!);
        }

        var listener = new IdentityConstraintListener(new ScriptedAssessor(), version, element =>
            string.Equals(element.Name.LocalName, "r", StringComparison.Ordinal) ? definitions : null
        );
        var xml = "<r" + (namespaced ? " xmlns=\"" + Tns + "\"" : string.Empty) + ">\n" + body + "\n</r>";
        return IdcFixture.Run(IdcFixture.Schema(ns: namespaced ? Tns : string.Empty), xml, listener);
    }
}
