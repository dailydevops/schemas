namespace NetEvolve.Schemas.Xsd.Tests.Unit.IdentityConstraints.References;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.SchemaDocuments;

/// <summary>Schema documents with identity constraints on global elements, one constraint per line from line 3 on.</summary>
internal static class ReferenceFixture
{
    internal static readonly SchemaNamespaceScope Scope = new("urn:t", false);

    private const string Body = "<xs:selector xpath=\"a\"/><xs:field xpath=\"@x\"/>";

    internal static string Definition(string kind, string name) =>
        kind == "keyref"
            ? $"<xs:keyref name=\"{name}\" refer=\"t:target\">{Body}</xs:keyref>"
            : $"<xs:{kind} name=\"{name}\">{Body}</xs:{kind}>";

    internal static string Schema(string targetNamespace, params string[][] elements)
    {
        var text =
            "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" xmlns:t=\"urn:t\" xmlns:o=\"urn:o\" targetNamespace=\""
            + targetNamespace
            + "\">\n";
        for (var i = 0; i < elements.Length; i++)
        {
            text +=
                "<xs:element name=\"e"
                + i
                + "\" type=\"xs:string\">\n"
                + string.Join("\n", elements[i])
                + "\n</xs:element>\n";
        }

        return text + "</xs:schema>";
    }

    internal static List<SchemaSyntaxNode> Elements(string schema)
    {
        var document = SchemaDocumentParser.Parse(
            schema.AsSpan(),
            "file:///t.xsd",
            new XsdIssueReporter(new ValidationIssueCollector(ValidationLimits.Default))
        );
        var list = new List<SchemaSyntaxNode>();
        for (var i = 0; i < document.Root!.ChildCount; i++)
        {
            list.Add(document.Root.GetChildNode(i));
        }

        return list;
    }

    internal static SchemaSyntaxNode Constraint(string schema, int index = 0) =>
        Elements(schema)[0].GetChildNode(index);

    internal static DiagnosticCode CodeOf(string ruleId)
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
}
