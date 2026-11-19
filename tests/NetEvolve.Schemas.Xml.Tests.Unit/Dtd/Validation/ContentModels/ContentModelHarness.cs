namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.ContentModels;

using System;
using System.Collections.Generic;
using System.Globalization;
using NetEvolve.Schemas;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Model;
using NetEvolve.Schemas.Xml.Dtd.Validation.ContentModels;

/// <summary>
/// Hand-built declaration models and a small event script for the content model step. A model is written in the syntax of
/// production 47 and turned into particles here, so the tests need no declaration parser. A script is a string of tags,
/// text, comments, processing instructions, CDATA sections, and references; a <c>~</c> marks the position where the first
/// issue is expected and is removed before the script runs.
/// </summary>
internal static class ContentModelHarness
{
    internal const string SystemId = "doc.xml";

    internal static DtdElementDeclaration Empty(string name) => new(name, DtdContentKind.Empty, null);

    internal static DtdElementDeclaration Any(string name) => new(name, DtdContentKind.Any, null);

    /// <summary>Mixed content, built as the parser builds it: a choice of the names, repeated when there are names.</summary>
    internal static DtdElementDeclaration Mixed(string name, params string[] names)
    {
        var items = new List<DtdContentParticle>(names.Length);
        foreach (var n in names)
        {
            items.Add(Name(n, DtdOccurrence.Once));
        }

        var content = new DtdContentParticle(
            DtdParticleKind.Choice,
            null,
            names.Length > 0 ? DtdOccurrence.ZeroOrMore : DtdOccurrence.Once,
            items
        );

        return new DtdElementDeclaration(name, DtdContentKind.Mixed, content);
    }

    internal static DtdElementDeclaration Children(string name, string model) =>
        new(name, DtdContentKind.Children, Parse(model));

    internal static DtdElementDeclaration Children(string name, DtdContentParticle model) =>
        new(name, DtdContentKind.Children, model);

    internal static DtdContentParticle Name(string name, DtdOccurrence occurrence) =>
        new(DtdParticleKind.Name, name, occurrence, null);

    internal static DtdContentParticle Group(
        DtdParticleKind kind,
        DtdOccurrence occurrence,
        params DtdContentParticle[] items
    ) => new(kind, null, occurrence, items);

    /// <summary>Parses a content model such as <c>(a,(b|c)*,d?)</c> into particles.</summary>
    internal static DtdContentParticle Parse(string model)
    {
        var i = 0;

        return ParseGroup(model, ref i);
    }

    internal static DtdDeclarations Declarations(string root, IEnumerable<DtdElementDeclaration> elements)
    {
        var builder = new DtdDeclarationBuilder(root, publicId: null, systemId: null);
        foreach (var element in elements)
        {
            builder.AddElement(element);
        }

        return builder.Build();
    }

    /// <summary>The declarations a script needs when the content model of <c>r</c> is the only one under test: <c>a</c> to <c>d</c> are EMPTY.</summary>
    internal static DtdElementDeclaration[] WithLeaves(DtdElementDeclaration root) =>
        [root, Empty("a"), Empty("b"), Empty("c"), Empty("d")];

    /// <summary>Runs the step over the script and returns what it reported.</summary>
    internal static ContentRun Run(
        string markedScript,
        DtdValidationOptions? options = null,
        params DtdElementDeclaration[] elements
    )
    {
        ArgumentNullException.ThrowIfNull(markedScript);

        var marker = markedScript.IndexOf('~', StringComparison.Ordinal);
        var script = marker < 0 ? markedScript : markedScript.Remove(marker, 1);
        var issues = new ValidationIssueCollector(ValidationLimits.Default);
        var context = new DtdValidationContext(
            Declarations("r", elements),
            options ?? DtdValidationOptions.Default,
            issues,
            isStandalone: false,
            SystemId,
            default
        );
        var step = new DtdContentModelStep();

        step.BeginDocument(context);
        Play(step, script);
        step.EndDocument();

        var expected = marker < 0 ? default : Locate(script, marker);

        return new ContentRun(issues, expected);
    }

    /// <summary>Runs only <c>BeginDocument</c>: the compile-time reports and limits.</summary>
    internal static ValidationIssueCollector Begin(
        DtdValidationOptions? options,
        IEnumerable<DtdElementDeclaration> elements
    )
    {
        var issues = new ValidationIssueCollector(ValidationLimits.Default);
        var context = new DtdValidationContext(
            Declarations("r", elements),
            options ?? DtdValidationOptions.Default,
            issues,
            isStandalone: false,
            SystemId,
            default
        );

        new DtdContentModelStep().BeginDocument(context);

        return issues;
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

    private static DocumentLocation Locate(string script, int index)
    {
        var position = Position(script, index);

        return new DocumentLocation(SystemId, position.Line, position.Column);
    }

    private static void Play(DtdContentModelStep step, string script)
    {
        var i = 0;
        while (i < script.Length)
        {
            var at = Position(script, i);
            if (Has(script, i, "<!--"))
            {
                step.Comment(in at);
                i = script.IndexOf("-->", i, StringComparison.Ordinal) + 3;
            }
            else if (Has(script, i, "<?"))
            {
                step.ProcessingInstruction(in at);
                i = script.IndexOf("?>", i, StringComparison.Ordinal) + 2;
            }
            else if (Has(script, i, "<![CDATA["))
            {
                var end = script.IndexOf("]]>", i, StringComparison.Ordinal);
                var from = i + 9;
                var inside = Position(script, from);
                step.Text(script.AsSpan(from, end - from), XmlTextKind.CData, in inside);
                i = end + 3;
            }
            else if (Has(script, i, "</"))
            {
                var end = i + script.AsSpan(i).IndexOf('>');
                step.EndElement(new ValidationElement(script.AsMemory(i + 2, end - i - 2), at));
                i = end + 1;
            }
            else if (script[i] == '<')
            {
                i = Start(step, script, i, at);
            }
            else if (script[i] == '&')
            {
                i = Reference(step, script, i, at);
            }
            else if (script[i] == '\n')
            {
                // The reader reports every line break as one reference with the text "\n".
                step.Text("\n".AsSpan(), XmlTextKind.Reference, in at);
                i++;
            }
            else
            {
                var end = i;
                while (end < script.Length && script[end] is not ('<' or '&' or '\n'))
                {
                    end++;
                }

                step.Text(script.AsSpan(i, end - i), XmlTextKind.Text, in at);
                i = end;
            }
        }
    }

    private static int Start(DtdContentModelStep step, string script, int i, XmlPosition at)
    {
        var end = i + script.AsSpan(i).IndexOf('>');
        var selfClosing = script[end - 1] == '/';
        var name = script.AsMemory(i + 1, end - i - 1 - (selfClosing ? 1 : 0));
        step.StartElement(new ValidationElement(name, at));
        if (selfClosing)
        {
            step.EndElement(new ValidationElement(name, at));
        }

        return end + 1;
    }

    private static int Reference(DtdContentModelStep step, string script, int i, XmlPosition at)
    {
        var end = i + script.AsSpan(i).IndexOf(';');
        var body = script.Substring(i + 1, end - i - 1);
        if (body[0] == '#')
        {
            var value = int.Parse(body.AsSpan(1), NumberStyles.None, CultureInfo.InvariantCulture);
            step.Text(((char)value).ToString().AsSpan(), XmlTextKind.Reference, in at);
        }
        else if (body is "amp" or "lt" or "gt" or "apos" or "quot")
        {
            var text = body switch
            {
                "amp" => "&",
                "lt" => "<",
                "gt" => ">",
                "apos" => "'",
                _ => "\"",
            };
            step.Text(text.AsSpan(), XmlTextKind.Reference, in at);
        }
        else
        {
            step.EntityReference(new ValidationEntityReference(body.AsMemory(), at, DtdEntityOutcome.DeclaredInternal));
        }

        return end + 1;
    }

    private static bool Has(string script, int index, string prefix) =>
        string.CompareOrdinal(script, index, prefix, 0, prefix.Length) == 0;

    private static DtdContentParticle ParseGroup(string s, ref int i)
    {
        i++; // (
        var items = new List<DtdContentParticle>();
        var separator = '\0';
        while (true)
        {
            SkipSpaces(s, ref i);
            items.Add(s[i] == '(' ? ParseGroup(s, ref i) : ParseName(s, ref i));
            SkipSpaces(s, ref i);
            if (s[i] == ')')
            {
                i++;
                break;
            }

            separator = s[i++];
        }

        return new DtdContentParticle(
            separator == '|' ? DtdParticleKind.Choice : DtdParticleKind.Sequence,
            null,
            ParseOccurrence(s, ref i),
            items
        );
    }

    private static DtdContentParticle ParseName(string s, ref int i)
    {
        var start = i;
        while (i < s.Length && (char.IsLetterOrDigit(s[i]) || s[i] == '_'))
        {
            i++;
        }

        return Name(s.Substring(start, i - start), ParseOccurrence(s, ref i));
    }

    private static DtdOccurrence ParseOccurrence(string s, ref int i)
    {
        var occurrence =
            i < s.Length
                ? s[i] switch
                {
                    '?' => DtdOccurrence.Optional,
                    '*' => DtdOccurrence.ZeroOrMore,
                    '+' => DtdOccurrence.OneOrMore,
                    _ => DtdOccurrence.Once,
                }
                : DtdOccurrence.Once;
        if (occurrence != DtdOccurrence.Once)
        {
            i++;
        }

        return occurrence;
    }

    private static void SkipSpaces(string s, ref int i)
    {
        while (s[i] == ' ')
        {
            i++;
        }
    }
}
