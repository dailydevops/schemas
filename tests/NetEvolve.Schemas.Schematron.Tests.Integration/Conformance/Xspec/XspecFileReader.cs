namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Xspec;

using System.Globalization;
using System.Xml;
using System.Xml.Linq;

/// <summary>Reads one XSpec file, and the files it imports, into expectations and exclusions.</summary>
/// <remarks>
/// Only the declarative subset is read: the scenarios with their <c>x:context</c> (a file or one inline element), the phase parameter, and the
/// expectations <c>x:expect-assert</c>, <c>x:expect-not-assert</c>, <c>x:expect-report</c>, <c>x:expect-not-report</c>, and
/// <c>x:expect-valid</c>. Everything that needs an XSLT or XQuery processor (variables, calls, helpers, selects, other parameters) and
/// everything that XSpec skips itself (<c>pending</c>) is left out with its reason.
/// </remarks>
internal sealed class XspecFileReader(string testSet, List<XspecCase> selected, List<XspecExclusion> excluded)
{
    private static readonly XNamespace X = "http://www.jenitennison.com/xslt/xspec";

    private readonly HashSet<string> _importing = new(StringComparer.Ordinal);
    private int _ordinal;

    /// <summary>Reads a file as the root of a test set.</summary>
    /// <param name="file">The full path of the file.</param>
    public void ReadFile(string file)
    {
        var root = Load(file, out var problem);
        if (root is null)
        {
            ExcludeFile(problem!);
            return;
        }

        if (root.Attribute("schematron") is not { } attribute)
        {
            ExcludeFile(
                "The description has no schematron attribute: the file tests XSLT or XQuery, or is only imported by another file."
            );
            return;
        }

        var baseDirectory = BaseDirectory(root, file);
        var schema = Path.GetFullPath(Path.Combine(baseDirectory, attribute.Value));
        if (!File.Exists(schema))
        {
            ExcludeFile($"The schema '{attribute.Value}' was not found.");
            return;
        }

        _ = _importing.Add(file);
        Walk(root, new XspecScope(baseDirectory, schema, null, null, null, ""));
    }

    /// <summary>Excludes the whole file.</summary>
    /// <param name="reason">Why the file is left out.</param>
    public void ExcludeFile(string reason) => excluded.Add(new XspecExclusion($"{testSet}/0000 file", testSet, reason));

    private static XElement? Load(string file, out string? problem)
    {
        try
        {
            var root = XDocument.Load(file, LoadOptions.PreserveWhitespace).Root;
            problem = root?.Name == X + "description" ? null : "The file is not an XSpec description.";
            return problem is null ? root : null;
        }
        catch (XmlException exception)
        {
            problem = $"The file is not well formed XML 1.0: {exception.Message}";
            return null;
        }
    }

    private static string BaseDirectory(XElement description, string file)
    {
        var directory = Path.GetDirectoryName(file)!;
        return description.Attribute(XNamespace.Xml + "base") is { } xmlBase
            ? Path.GetFullPath(Path.Combine(directory, xmlBase.Value))
            : directory;
    }

    private void Walk(XElement parent, XspecScope scope)
    {
        scope = Declare(parent, scope);
        foreach (var child in parent.Elements())
        {
            if (child.Name.Namespace != X)
            {
                continue;
            }

            switch (child.Name.LocalName)
            {
                case "scenario":
                    Walk(child, Scenario(child, scope));
                    break;
                case "pending":
                    Walk(child, scope with { Skip = scope.Skip ?? "x:pending" });
                    break;
                case "import":
                    Import(child, scope);
                    break;
                case "like":
                    Exclude(scope, "x:like", "x:like reuses a scenario by label and is not resolved.");
                    break;
                case "expect":
                    Exclude(
                        scope,
                        "x:expect",
                        "x:expect is the generic XSLT expectation, not a declarative Schematron expectation."
                    );
                    break;
                case "expect-rule":
                    Exclude(
                        scope,
                        "x:expect-rule",
                        "x:expect-rule checks the fired rules, which are not part of the declarative subset."
                    );
                    break;
                case "expect-assert":
                    Expect(child, scope, XspecExpectKind.Assert);
                    break;
                case "expect-not-assert":
                    Expect(child, scope, XspecExpectKind.NotAssert);
                    break;
                case "expect-report":
                    Expect(child, scope, XspecExpectKind.Report);
                    break;
                case "expect-not-report":
                    Expect(child, scope, XspecExpectKind.NotReport);
                    break;
                case "expect-valid":
                    Expect(child, scope, XspecExpectKind.Valid);
                    break;
                default:
                    break;
            }
        }
    }

    // Parameters, variables, calls, helpers, and the context apply to the whole level, wherever they stand.
    private static XspecScope Declare(XElement parent, XspecScope scope)
    {
        foreach (var child in parent.Elements())
        {
            if (child.Name.Namespace != X)
            {
                continue;
            }

            switch (child.Name.LocalName)
            {
                case "param":
                    scope = Parameter(child, scope);
                    break;
                case "variable" or "call" or "helper":
                    scope = scope with { Skip = scope.Skip ?? $"x:{child.Name.LocalName}" };
                    break;
                case "context":
                    scope = Context(child, scope);
                    break;
                default:
                    break;
            }
        }

        return scope;
    }

    private static XspecScope Parameter(XElement parameter, XspecScope scope)
    {
        var name = (string?)parameter.Attribute("name") ?? "";
        if (!name.Equals("phase", StringComparison.Ordinal) && !name.EndsWith(":phase", StringComparison.Ordinal))
        {
            return scope with { Skip = scope.Skip ?? $"The parameter {name} is not read." };
        }

        var value = ((string?)parameter.Attribute("select") ?? parameter.Value).Trim();
        return scope with { Phase = value.Length >= 2 && value[0] == '\'' && value[^1] == '\'' ? value[1..^1] : value };
    }

    private static XspecScope Context(XElement context, XspecScope scope)
    {
        if (context.Attribute("select") is not null)
        {
            return scope with { Skip = scope.Skip ?? "x:context/@select is an XSLT expression and is not read." };
        }

        if (context.Attribute("href") is { } href)
        {
            if (href.Value.Contains('%', StringComparison.Ordinal))
            {
                return scope with { Skip = scope.Skip ?? $"The context '{href.Value}' holds a placeholder." };
            }

            var path = Path.GetFullPath(Path.Combine(scope.BaseDirectory, href.Value));
            return File.Exists(path)
                ? scope with
                {
                    Context = new XspecContext(path, null),
                }
                : scope with
                {
                    Skip = scope.Skip ?? $"The context file '{href.Value}' was not found.",
                };
        }

        var elements = context.Elements().ToList();
        var inline = elements.Where(e => e.Name.Namespace != X).ToList();
        if (UsesValueTemplates(context))
        {
            return scope with
            {
                Skip = scope.Skip
                    ?? "XSpec expands value templates in the inline context, which are not expanded here.",
            };
        }

        return
            elements.Count != inline.Count
            || inline.Count != 1
            || context.Nodes().OfType<XText>().Any(t => !string.IsNullOrWhiteSpace(t.Value))
            ? scope with
            {
                Skip = scope.Skip ?? "The inline context must hold exactly one element and nothing else.",
            }
            : scope with
            {
                Context = new XspecContext(null, inline[0].ToString(SaveOptions.DisableFormatting)),
            };
    }

    // XSpec expands attribute value templates in the content of x:context, and text value templates when expand-text is on.
    private static bool UsesValueTemplates(XElement context) =>
        context.Attribute("expand-text") is { } expand
            && (
                string.Equals(expand.Value, "yes", StringComparison.Ordinal)
                || string.Equals(expand.Value, "true", StringComparison.Ordinal)
                || string.Equals(expand.Value, "1", StringComparison.Ordinal)
            )
        || context
            .Descendants()
            .Any(e => e.Attributes().Any(a => !a.IsNamespaceDeclaration && a.Value.AsSpan().IndexOfAny('{', '}') >= 0));

    private static XspecScope Scenario(XElement scenario, XspecScope scope)
    {
        var label = Normalize((string?)scenario.Attribute("label") ?? scenario.Element(X + "label")?.Value ?? "");
        var path = scope.Path.Length == 0 ? label : scope.Path + " " + label;
        var skip = scope.Skip
            ?? (
                scenario.Attribute("pending") is { } pending ? $"The scenario is pending: {pending.Value}"
                : string.Equals((string?)scenario.Attribute("shared"), "yes", StringComparison.Ordinal)
                    ? "A shared scenario runs only through x:like."
                : null
            );
        return scope with { Path = path, Skip = skip };
    }

    private void Import(XElement import, XspecScope scope)
    {
        var href = (string?)import.Attribute("href") ?? "";
        var file = Path.GetFullPath(Path.Combine(scope.BaseDirectory, href));
        if (!File.Exists(file))
        {
            Exclude(scope, "x:import", $"The imported file '{href}' was not found.");
        }
        else if (!_importing.Add(file))
        {
            Exclude(scope, "x:import", $"The file '{href}' is imported twice or circularly.");
        }
        else if (Load(file, out var problem) is { } root)
        {
            Walk(root, scope with { BaseDirectory = BaseDirectory(root, file) });
        }
        else
        {
            Exclude(scope, "x:import", $"The imported file '{href}': {problem}");
        }
    }

    private void Expect(XElement element, XspecScope scope, XspecExpectKind kind)
    {
        var name = "x:" + element.Name.LocalName;
        var reason = scope.Skip
            ?? (
                element.Attribute("pending") is { } pending ? $"The expectation is pending: {pending.Value}"
                : scope.Context is null ? "The scenario has no context."
                : null
            );
        if (reason is not null)
        {
            Exclude(scope, name, reason);
            return;
        }

        int? count = null;
        if (element.Attribute("count") is { } countAttribute)
        {
            if (!int.TryParse(countAttribute.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var number))
            {
                Exclude(scope, name, $"The count '{countAttribute.Value}' is no number.");
                return;
            }

            count = number;
        }

        var text = Normalize(element.Value);
        var expectation = new XspecExpectation(
            kind,
            (string?)element.Attribute("id"),
            (string?)element.Attribute("role"),
            (string?)element.Attribute("location"),
            count,
            text.Length == 0 ? null : text,
            Namespaces(element)
        );
        selected.Add(
            new XspecCase(
                CaseId(scope, name + (expectation.Id is null ? "" : " " + expectation.Id)),
                testSet,
                scope.SchemaPath,
                scope.Phase,
                scope.Context!,
                expectation
            )
        );
    }

    private void Exclude(XspecScope scope, string name, string reason) =>
        excluded.Add(new XspecExclusion(CaseId(scope, name), testSet, scope.Skip ?? reason));

    private string CaseId(XspecScope scope, string name) =>
        $"{testSet}/{(++_ordinal).ToString("D4", CultureInfo.InvariantCulture)} {scope.Path} :: {name}";

    // The bindings in scope of an element, the nearest declaration of a prefix winning; the default namespace does not apply to a location.
    private static Dictionary<string, string> Namespaces(XElement element)
    {
        var bindings = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var ancestor in element.AncestorsAndSelf())
        {
            foreach (var attribute in ancestor.Attributes().Where(a => a.IsNamespaceDeclaration))
            {
                if (attribute.Name.Namespace == XNamespace.Xmlns)
                {
                    _ = bindings.TryAdd(attribute.Name.LocalName, attribute.Value);
                }
            }
        }

        return bindings;
    }

    private static string Normalize(string value) => XspecJudge.Normalize(value);
}
