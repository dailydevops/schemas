namespace NetEvolve.Schemas.Xsd.Tests.Unit.SchemaDocuments;

using System;
using System.Collections.Generic;
using System.Text;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.SchemaDocuments;

/// <summary>Parses schema documents and describes the result as text.</summary>
internal static class SchemaFixture
{
    public const string SystemId = "file:///t.xsd";

    public const string Open = "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\">\n";

    public const string Close = "\n</xs:schema>";

    /// <summary>The caret marks the start tag whose position the test expects.</summary>
    public const char Marker = '^';

    public static XsdIssueReporter NewReporter(out ValidationIssueCollector collector)
    {
        collector = new ValidationIssueCollector(ValidationLimits.Default);
        return new XsdIssueReporter(collector);
    }

    public static (SchemaDocument Document, IReadOnlyList<ValidationIssue> Issues) Parse(string text)
    {
        var reporter = NewReporter(out var collector);
        var document = SchemaDocumentParser.Parse(text.AsSpan(), SystemId, reporter);
        return (document, collector.ToResult().Issues);
    }

    /// <summary>Removes the marker and returns the document with the one-based line and column of the marker.</summary>
    public static (string Text, int Line, int Column) Unmark(string marked)
    {
        if (marked.StartsWith(Open, StringComparison.Ordinal))
        {
            marked += Close;
        }

        var index = marked.IndexOf(Marker.ToString(), StringComparison.Ordinal);
        var line = 1;
        var lineStart = 0;
        for (var i = 0; i < index; i++)
        {
            if (marked[i] == '\n')
            {
                line++;
                lineStart = i + 1;
            }
        }

        return (marked.Remove(index, 1), line, index - lineStart + 1);
    }

    /// <summary>Finds a descendant of the root by the local names of the path, empty for the root.</summary>
    public static SchemaSyntaxNode Find(SchemaDocument document, string path)
    {
        var node = document.Root!;
        if (path.Length == 0)
        {
            return node;
        }

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

    public static string Attributes(SchemaSyntaxNode node)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < node.AttributeCount; i++)
        {
            var attribute = node.GetAttribute(i);
            builder
                .Append(i == 0 ? string.Empty : ";")
                .Append(attribute.Name.LocalName)
                .Append('=')
                .Append(attribute.Value);
        }

        return builder.ToString();
    }

    /// <summary>Describes the whole model: name, location, attributes, and children of every node.</summary>
    public static string Dump(SchemaSyntaxNode node)
    {
        var builder = new StringBuilder();
        Append(builder, node);
        return builder.ToString();
    }

    private static void Append(StringBuilder builder, SchemaSyntaxNode node)
    {
        builder
            .Append(node.Name.LocalName)
            .Append('@')
            .Append(node.Location.Source)
            .Append(':')
            .Append(node.Location.Line)
            .Append(':')
            .Append(node.Location.Column)
            .Append('[');
        for (var i = 0; i < node.AttributeCount; i++)
        {
            var attribute = node.GetAttribute(i);
            builder
                .Append(i == 0 ? string.Empty : ",")
                .Append(attribute.Name.NamespaceName)
                .Append('|')
                .Append(attribute.Name.LocalName)
                .Append('=')
                .Append(attribute.Value)
                .Append('@')
                .Append(attribute.Location.Line)
                .Append(':')
                .Append(attribute.Location.Column);
        }

        builder.Append("]{");
        for (var i = 0; i < node.ChildCount; i++)
        {
            Append(builder, node.GetChildNode(i));
        }

        builder.Append('}');
    }
}
