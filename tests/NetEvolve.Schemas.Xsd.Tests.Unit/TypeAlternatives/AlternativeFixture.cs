namespace NetEvolve.Schemas.Xsd.Tests.Unit.TypeAlternatives;

using System;
using System.Collections.Generic;
using System.Text;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.SchemaDocuments;
using NetEvolve.Schemas.Xsd.SimpleTypes.V11;
using NetEvolve.Schemas.Xsd.Tests.Unit.Declarations11;
using NetEvolve.Schemas.Xsd.Tests.Unit.Derivation.Types;
using NetEvolve.Schemas.Xsd.TypeAlternatives;

/// <summary>Builds type tables from schema syntax, binds their types to real components, and feeds elements to the evaluator.</summary>
internal sealed class AlternativeFixture
{
    public const string Tns = "urn:test";

    private readonly ValidationIssueCollector _collector = new(ValidationLimits.Default);
    private readonly Dictionary<string, IXsdComponent> _types = new(StringComparer.Ordinal);

    public AlternativeFixture()
    {
        var f = new TypeFixture();
        Declared = TypeFixture.Xs("string");
        _types["a"] = f.Restrict("a", Declared);
        _types["b"] = f.Restrict("b", Declared);
        _types["c"] = f.Restrict("c", Declared);
        _types["x"] = f.Root("x");
        _types["error"] = ErrorType.Instance;
        _types["string"] = Declared;
    }

    /// <summary>Gets the type of the element declaration in the tests: <c>xs:string</c>.</summary>
    public IXsdComponent Declared { get; }

    public XsdIssueReporter Reporter => new(_collector);

    public IReadOnlyList<ValidationIssue> Issues => _collector.ToResult().Issues;

    public IXsdComponent Type(string localName) => _types[localName];

    public static TypeAlternativeBuilder Builder() => new(new SchemaNamespaceScope(Tns, false));

    /// <summary>Parses an <c>element</c> that holds the given children.</summary>
    public static SchemaSyntaxNode Element(string children, string schemaAttributes = "", string namespaces = "")
    {
        var text =
            "<xs:schema "
            + SyntaxFixture.Namespaces
            + " "
            + namespaces
            + " targetNamespace=\"urn:test\" "
            + schemaAttributes
            + ">\n<xs:element name=\"e\" type=\"xs:string\">"
            + children
            + "</xs:element>\n</xs:schema>";
        return SyntaxFixture.ParseDocument(text).GetChildNode(0);
    }

    /// <summary>Builds the table of the alternatives and binds every named type to the component of the same local name.</summary>
    public TypeTable? Table(string alternatives, string schemaAttributes = "", string namespaces = "")
    {
        var table = TypeTable.FromSyntax(Element(alternatives, schemaAttributes, namespaces), Builder(), Reporter);
        if (table is null)
        {
            return null;
        }

        for (var i = 0; i < table.Count; i++)
        {
            Bind(table[i]);
        }

        if (table.Default is not null)
        {
            Bind(table.Default);
        }

        return table;
    }

    /// <summary>Opens the frame of an element on the stack and selects its type.</summary>
    public TypeSelection Enter(
        TypeAlternativeEvaluator evaluator,
        TypeTable table,
        InheritedAttributes stack,
        params Attr[] attributes
    ) => Enter(evaluator, table, stack, new XPathQName(Tns, "e"), attributes);

    /// <summary>Opens the frame of an element on the stack and selects its type.</summary>
    public TypeSelection Enter(
        TypeAlternativeEvaluator evaluator,
        TypeTable table,
        InheritedAttributes stack,
        XPathQName element,
        params Attr[] attributes
    )
    {
        var entries = new TypeAlternativeAttribute[attributes.Length];
        var values = new StringBuilder();
        for (var i = 0; i < attributes.Length; i++)
        {
            entries[i] = new TypeAlternativeAttribute(
                new XPathQName(attributes[i].Namespace, attributes[i].Name),
                values.Length,
                attributes[i].Value.Length,
                attributes[i].Inheritable
            );
            _ = values.Append(attributes[i].Value);
        }

        var buffer = values.ToString();
        stack.Enter(entries, buffer);
        return evaluator.Select(table, Declared, element, entries, buffer, stack);
    }

    /// <summary>Selects the type of an element without ancestors.</summary>
    public TypeSelection Select(TypeTable table, params Attr[] attributes)
    {
        using var evaluator = new TypeAlternativeEvaluator();
        return Enter(evaluator, table, new InheritedAttributes(), attributes);
    }

    public static string Name(TypeSelection selection) => selection.Type.Name.LocalName;

    private void Bind(TypeAlternative alternative)
    {
        if (alternative.ReferenceCount > 0)
        {
            alternative.Bind(0, _types[alternative.GetReference(0).Name.LocalName]);
        }
    }
}
