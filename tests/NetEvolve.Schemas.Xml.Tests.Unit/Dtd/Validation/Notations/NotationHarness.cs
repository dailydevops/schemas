namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Notations;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Model;
using NetEvolve.Schemas.Xml.Dtd.Validation.Notations;

/// <summary>
/// Runs the notation step over a hand-built declaration model and a script of tags. A start tag is written as
/// <c>&lt;e a="v" b='w'/&gt;</c>; the declared type of each attribute comes from the model, and a <c>~</c> marks the position
/// where the first issue is expected and is removed before the script runs.
/// </summary>
internal static class NotationHarness
{
    internal const string SystemId = "doc.xml";

    /// <summary>The location of the document type declaration, where declaration-time checks report.</summary>
    internal static readonly DocumentLocation DoctypeLocation = new(SystemId, 1, 10);

    internal static DtdValidationContext Context(
        ValidationIssueCollector issues,
        IDtdDeclarations declarations,
        DocumentLocation? doctype = null
    ) =>
        new(
            declarations,
            DtdValidationOptions.Default,
            issues,
            isStandalone: false,
            SystemId,
            doctype ?? DoctypeLocation
        );

    /// <summary>Runs <c>BeginDocument</c> only: the declaration-time checks.</summary>
    internal static ValidationIssueCollector Begin(IDtdDeclarations declarations)
    {
        var issues = new ValidationIssueCollector(ValidationLimits.Default);
        new DtdNotationStep().BeginDocument(Context(issues, declarations));

        return issues;
    }

    /// <summary>Runs the step over the script and returns what it reported.</summary>
    internal static NotationRun Run(string markedScript, DtdDeclarations declarations)
    {
        ArgumentNullException.ThrowIfNull(markedScript);

        var marker = markedScript.IndexOf('~', StringComparison.Ordinal);
        var script = marker < 0 ? markedScript : markedScript.Remove(marker, 1);
        var issues = new ValidationIssueCollector(ValidationLimits.Default);
        var step = new DtdNotationStep();

        step.BeginDocument(Context(issues, declarations));
        Play(step, script, declarations);
        step.EndDocument();

        var expected = marker < 0 ? default : Locate(script, marker);

        return new NotationRun(issues, expected);
    }

    internal static XmlPosition Position(string script, int index)
    {
        var line = 1;
        var start = 0;
        for (var i = 0; i < index; i++)
        {
            if (script[i] == '\n')
            {
                line++;
                start = i + 1;
            }
        }

        return new XmlPosition(line, index - start + 1, index);
    }

    /// <summary>Builds the start tag of one element with one attribute, for tests that call the step directly.</summary>
    internal static ValidationElement Element(
        string name,
        string attribute,
        string value,
        DtdAttributeType type,
        bool isSpecified = true
    )
    {
        var position = new XmlPosition(1, 1, 0);
        var info = new ValidationAttributeInfo(attribute.AsMemory(), value.AsMemory(), type, isSpecified, position);

        return new ValidationElement(name.AsMemory(), position, new[] { info });
    }

    private static DocumentLocation Locate(string script, int index)
    {
        var position = Position(script, index);

        return new DocumentLocation(SystemId, position.Line, position.Column);
    }

    private static void Play(DtdNotationStep step, string script, DtdDeclarations declarations)
    {
        var i = 0;
        while (i < script.Length)
        {
            if (script[i] != '<')
            {
                i++;

                continue;
            }

            var at = Position(script, i);
            if (script[i + 1] == '/')
            {
                var close = i + script.AsSpan(i).IndexOf('>');
                step.EndElement(new ValidationElement(script.AsMemory(i + 2, close - i - 2), at));
                i = close + 1;
            }
            else
            {
                i = Start(step, script, i, at, declarations);
            }
        }
    }

    private static int Start(DtdNotationStep step, string script, int i, XmlPosition at, DtdDeclarations declarations)
    {
        var j = i + 1;
        while (script[j] is not (' ' or '/' or '>'))
        {
            j++;
        }

        var name = script.AsMemory(i + 1, j - i - 1);
        var attributes = new List<ValidationAttributeInfo>();
        while (true)
        {
            while (script[j] == ' ')
            {
                j++;
            }

            if (script[j] is '/' or '>')
            {
                break;
            }

            var nameStart = j;
            while (script[j] != '=')
            {
                j++;
            }

            var attributeName = script.AsMemory(nameStart, j - nameStart);
            var quote = script[j + 1];
            var valueStart = j + 2;
            var valueEnd = valueStart + script.AsSpan(valueStart).IndexOf(quote);
            attributes.Add(
                new ValidationAttributeInfo(
                    attributeName,
                    script.AsMemory(valueStart, valueEnd - valueStart),
                    DeclaredType(declarations, name.ToString(), attributeName.ToString()),
                    isSpecified: true,
                    Position(script, nameStart)
                )
            );
            j = valueEnd + 1;
        }

        var selfClosing = script[j] == '/';
        step.StartElement(new ValidationElement(name, at, attributes.ToArray()));
        if (selfClosing)
        {
            step.EndElement(new ValidationElement(name, at));
        }

        return j + script.AsSpan(j).IndexOf('>') + 1;
    }

    private static DtdAttributeType? DeclaredType(DtdDeclarations declarations, string element, string attribute)
    {
        if (declarations.AttributeLists.TryGetValue(element, out var definitions))
        {
            foreach (var definition in definitions)
            {
                if (string.Equals(definition.Name, attribute, StringComparison.Ordinal))
                {
                    return definition.Type;
                }
            }
        }

        return null;
    }
}
