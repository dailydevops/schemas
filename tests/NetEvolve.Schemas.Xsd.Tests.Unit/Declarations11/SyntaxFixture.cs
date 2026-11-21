namespace NetEvolve.Schemas.Xsd.Tests.Unit.Declarations11;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.SchemaDocuments;
using static NetEvolve.Schemas.Xsd.Tests.Unit.SchemaDocuments.SchemaFixture;

/// <summary>Parses schema documents for the syntax checks of the declaration changes.</summary>
internal static class SyntaxFixture
{
    public const string Namespaces =
        "xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" xmlns:t=\"urn:test\" xmlns:b=\"urn:b\"";

    /// <summary>A schema element with the target namespace <c>urn:s</c>.</summary>
    public const string WithNamespace = "<xs:schema " + Namespaces + " targetNamespace=\"urn:s\">\n";

    /// <summary>A schema element without a target namespace.</summary>
    public const string WithoutNamespace = "<xs:schema " + Namespaces + ">\n";

    public const string EndOfSchema = "\n</xs:schema>";

    /// <summary>Parses a document in which the caret marks the start tag whose position the test expects.</summary>
    public static (SchemaSyntaxNode Root, int Line, int Column) ParseMarked(string marked)
    {
        var (text, line, column) = Unmark(marked);
        return (ParseDocument(text), line, column);
    }

    public static SchemaSyntaxNode ParseDocument(string text)
    {
        var (document, issues) = Parse(text);
        if (issues.Count != 0)
        {
            throw new InvalidOperationException("The test document is not a schema document: " + issues[0].Message);
        }

        return document.Root!;
    }

    /// <summary>Finds a descendant of the root by the local names of the path, taking the first child with each name.</summary>
    public static SchemaSyntaxNode FindNode(SchemaSyntaxNode root, string path)
    {
        var node = root;
        foreach (var step in path.Split('/'))
        {
            SchemaSyntaxNode? next = null;
            for (var i = 0; i < node.ChildCount && next is null; i++)
            {
                if (string.Equals(node.GetChildNode(i).Name.LocalName, step, StringComparison.Ordinal))
                {
                    next = node.GetChildNode(i);
                }
            }

            node = next
                ?? throw new InvalidOperationException("No '" + step + "' below '" + node.Name.LocalName + "'.");
        }

        return node;
    }

    public static XsdIssueReporter Reporter(out ValidationIssueCollector collector) => NewReporter(out collector);

    public static IReadOnlyList<ValidationIssue> Issues(ValidationIssueCollector collector) =>
        collector.ToResult().Issues;
}
