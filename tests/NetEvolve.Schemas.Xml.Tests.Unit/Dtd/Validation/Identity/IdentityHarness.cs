namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Identity;

using System;
using System.Collections.Generic;
using System.Text;
using NetEvolve.Schemas;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Model;
using NetEvolve.Schemas.Xml.Dtd.Validation.Identity;
using NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.ContentModels;

/// <summary>
/// Hand-built attribute lists and a small event script for the identity step. A script is a string of tags with quoted
/// attributes; a <c>~</c> in front of an attribute name (or of a tag, for a defaulted attribute) marks a position where an
/// issue is expected and is removed before the script runs. The declared type of every attribute comes from the attribute
/// lists, and defaults are applied the way XML-16 applies them (an attribute that the tag does not carry arrives with
/// <c>IsSpecified</c> false), so the tests need neither a declaration parser nor a reader.
/// </summary>
internal static class IdentityHarness
{
    internal const string SystemId = "doc.xml";

    /// <summary>The location of the document type declaration; declaration-time issues report here.</summary>
    internal static readonly DocumentLocation DoctypeLocation = new(SystemId, 2, 3);

    internal static DtdAttributeDefinition Attribute(
        string name,
        DtdAttributeType type,
        DtdDefaultKind kind = DtdDefaultKind.Implied,
        string? value = null
    ) => new(name, type, null, kind, value);

    internal static DtdAttributeDefinition Id(
        string name,
        DtdDefaultKind kind = DtdDefaultKind.Implied,
        string? value = null
    ) => Attribute(name, DtdAttributeType.Id, kind, value);

    /// <summary>The attribute lists most tests use for the element types r, a, and b: an ID attribute id, an IDREF attribute named ref, an IDREFS attribute refs, and a plain attribute note.</summary>
    internal static Attlist[] Standard() => [Standard("r"), Standard("a"), Standard("b")];

    internal static Attlist Standard(string element) =>
        new(
            element,
            [
                Attribute("id", DtdAttributeType.Id),
                Attribute("ref", DtdAttributeType.IdRef),
                Attribute("refs", DtdAttributeType.IdRefs),
                Attribute("note", DtdAttributeType.Cdata),
            ]
        );

    internal static DtdDeclarations Declare(params Attlist[] lists)
    {
        var builder = new DtdDeclarationBuilder("r", publicId: null, systemId: null);
        foreach (var list in lists)
        {
            foreach (var definition in list.Definitions)
            {
                builder.AddAttribute(list.Element, definition);
            }
        }

        return builder.Build();
    }

    internal static DtdValidationContext Context(ValidationIssueCollector issues, DtdDeclarations declarations) =>
        new(declarations, DtdValidationOptions.Default, issues, isStandalone: false, SystemId, DoctypeLocation);

    internal static ValidationIssueCollector Collector(int maxIssues = 1_000) =>
        new(ValidationLimits.Default.WithMaxIssues(maxIssues));

    /// <summary>Runs the step over the script with the standard attribute lists.</summary>
    internal static IdentityRun Run(string markedScript) => RunWith(markedScript, null, Standard());

    /// <summary>Runs the step with the given reader limits over the script with the standard attribute lists.</summary>
    internal static IdentityRun Run(string markedScript, XmlReaderLimits limits) =>
        RunWith(markedScript, limits, Standard());

    /// <summary>Runs the step over the script with the given attribute lists.</summary>
    internal static IdentityRun Run(string markedScript, params Attlist[] lists) => RunWith(markedScript, null, lists);

    internal static IdentityRun RunWith(string markedScript, XmlReaderLimits? limits, params Attlist[] lists)
    {
        ArgumentNullException.ThrowIfNull(markedScript);

        var (script, markers) = Strip(markedScript);
        var declarations = Declare(lists);
        var issues = Collector();
        var step = new DtdIdentityStep(limits);

        step.BeginDocument(Context(issues, declarations));
        Play(step, script, declarations);
        var beforeEnd = issues.Count;
        step.EndDocument();

        var expected = new List<DocumentLocation>(markers.Count);
        foreach (var marker in markers)
        {
            var position = ContentModelHarness.Position(script, marker);
            expected.Add(new DocumentLocation(SystemId, position.Line, position.Column));
        }

        return new IdentityRun(issues, expected, beforeEnd);
    }

    private static (string Script, List<int> Markers) Strip(string marked)
    {
        var builder = new StringBuilder(marked.Length);
        var markers = new List<int>();
        foreach (var c in marked)
        {
            if (c == '~')
            {
                markers.Add(builder.Length);
            }
            else
            {
                _ = builder.Append(c);
            }
        }

        return (builder.ToString(), markers);
    }

    private static void Play(DtdIdentityStep step, string script, DtdDeclarations declarations)
    {
        var i = 0;
        while (i < script.Length)
        {
            if (script[i] != '<')
            {
                i++;
                continue;
            }

            var at = ContentModelHarness.Position(script, i);
            if (script[i + 1] == '/')
            {
                var end = i + script.AsSpan(i).IndexOf('>');
                step.EndElement(new ValidationElement(script.AsMemory(i + 2, end - i - 2), at));
                i = end + 1;
            }
            else
            {
                i = Start(step, script, i, at, declarations);
            }
        }
    }

    private static int Start(DtdIdentityStep step, string script, int i, XmlPosition at, DtdDeclarations declarations)
    {
        var j = i + 1;
        while (script[j] is not (' ' or '/' or '>' or '\n'))
        {
            j++;
        }

        var name = script.AsMemory(i + 1, j - i - 1);
        _ = declarations.AttributeLists.TryGetValue(name.ToString(), out var definitions);
        var attributes = new List<ValidationAttributeInfo>();
        while (true)
        {
            while (script[j] is ' ' or '\n')
            {
                j++;
            }

            if (script[j] is '/' or '>')
            {
                break;
            }

            j = ReadAttribute(script, j, definitions, attributes);
        }

        AddDefaults(definitions, attributes, at);

        var element = new ValidationElement(name, at, attributes.ToArray());
        step.StartElement(in element);
        if (script[j] == '/')
        {
            step.EndElement(new ValidationElement(name, at));
            j++;
        }

        return j + 1;
    }

    private static int ReadAttribute(
        string script,
        int nameStart,
        IReadOnlyList<DtdAttributeDefinition>? definitions,
        List<ValidationAttributeInfo> attributes
    )
    {
        var j = nameStart;
        while (script[j] != '=')
        {
            j++;
        }

        var name = script.AsMemory(nameStart, j - nameStart);
        var valueStart = j + 2;
        var valueEnd = valueStart + script.AsSpan(valueStart).IndexOf(script[j + 1]);
        DtdAttributeType? type = null;
        if (definitions is not null)
        {
            foreach (var definition in definitions)
            {
                if (string.Equals(definition.Name, name.ToString(), StringComparison.Ordinal))
                {
                    type = definition.Type;
                }
            }
        }

        attributes.Add(
            new ValidationAttributeInfo(
                name,
                script.AsMemory(valueStart, valueEnd - valueStart),
                type,
                isSpecified: true,
                ContentModelHarness.Position(script, nameStart)
            )
        );

        return valueEnd + 1;
    }

    // The defaults of the declarations for the attributes that the tag does not carry, as XML-16 applies them.
    private static void AddDefaults(
        IReadOnlyList<DtdAttributeDefinition>? definitions,
        List<ValidationAttributeInfo> attributes,
        XmlPosition at
    )
    {
        if (definitions is null)
        {
            return;
        }

        var specified = attributes.Count;
        foreach (var definition in definitions)
        {
            if (definition.DefaultKind is not (DtdDefaultKind.Default or DtdDefaultKind.Fixed))
            {
                continue;
            }

            var carried = false;
            for (var k = 0; k < specified; k++)
            {
                carried |= attributes[k].Name.Span.SequenceEqual(definition.Name.AsSpan());
            }

            if (!carried)
            {
                attributes.Add(
                    new ValidationAttributeInfo(
                        definition.Name.AsMemory(),
                        definition.DefaultValue.AsMemory(),
                        definition.Type,
                        isSpecified: false,
                        at
                    )
                );
            }
        }
    }
}
