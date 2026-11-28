namespace NetEvolve.Schemas.Schematron.Evaluation.Assertions;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Evaluation.Annotations;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Scopes;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>
/// Compiles an <c>assert</c> or <c>report</c> with its message, diagnostics, and properties in the scope of its rule, so they can
/// read the variables of the rule (design #150 EP-5). The <see cref="CompiledAssertion"/> it returns is immutable and shared by
/// every run.
/// </summary>
/// <remarks>
/// An id in <c>diagnostics</c> or <c>properties</c> that no declaration has is <see cref="SchematronDiagnosticCodes.IdrefUnresolved"/>
/// (design #137 AP-9). The <c>role</c>, <c>flag</c>, and <c>severity</c> attributes are compiled by <see cref="AnnotationCompiler"/>, in the same scope.
/// </remarks>
internal static class AssertionCompiler
{
    /// <summary>Compiles an assertion; every problem is reported and compiling continues, so one call finds them all.</summary>
    /// <param name="assertion">The <c>assert</c> or <c>report</c>.</param>
    /// <param name="rule">The rule that holds it, with its scope.</param>
    /// <param name="schema">The effective schema, which declares the diagnostics and properties.</param>
    /// <param name="binding">The binding that compiles the expressions.</param>
    /// <param name="position">The ordinals of the assertion in the effective model.</param>
    /// <param name="problems">Receives every problem.</param>
    /// <param name="compiled">The compiled assertion; valid only when the method returns <see langword="true"/>.</param>
    /// <returns><see langword="true"/> when no problem was reported.</returns>
    internal static bool TryCompile(
        SchematronAssertion assertion,
        RuleScope rule,
        SchematronSchema schema,
        IQueryBinding binding,
        in AssertionPosition position,
        IProblemSink problems,
        out CompiledAssertion compiled
    )
    {
        ArgumentNullException.ThrowIfNull(assertion);
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(binding);
        ArgumentNullException.ThrowIfNull(problems);

        var scope = rule.Scope;
        var ok = Compile(binding, assertion.Test, QueryExpressionKind.Boolean, scope, problems, out var test);
        ok &= TryCompileSubject(binding, assertion, rule, problems, out var subject);
        ok &= TryCompileMessage(binding, string.Empty, assertion.Message, scope, problems, out var message);
        ok &= TryCompileReferences(binding, assertion, schema, false, scope, problems, out var diagnostics);
        ok &= TryCompileReferences(binding, assertion, schema, true, scope, problems, out var properties);
        ok &= AnnotationCompiler.TryCompile(
            assertion.Role,
            assertion.Flags,
            assertion.Severity,
            assertion.Location,
            scope,
            binding,
            problems,
            out var annotations
        );

        compiled = ok
            ? Create(assertion, rule, in position, test!, subject, message!, diagnostics!, properties!, annotations)
            : null!;
        return ok;
    }

    private static CompiledAssertion Create(
        SchematronAssertion assertion,
        RuleScope rule,
        in AssertionPosition position,
        QueryExpression test,
        QueryExpression? subject,
        CompiledMessage message,
        CompiledMessage[] diagnostics,
        CompiledMessage[] properties,
        CompiledAnnotations annotations
    )
    {
        var constraint = AssertionIdentifier.Create(
            assertion.Id,
            rule.Rule.Id,
            position.Pattern,
            position.Rule,
            position.Assertion,
            position.Expansion
        );
        return new CompiledAssertion(
            assertion is SchematronReport,
            assertion.Id is { Length: > 0 } ? constraint : string.Empty,
            rule.Rule.Id ?? string.Empty,
            constraint,
            assertion.Test,
            assertion.Location,
            test,
            subject,
            message,
            diagnostics,
            properties,
            annotations
        );
    }

    // The subject of the assertion wins over the subject of the rule; neither is the context node.
    private static bool TryCompileSubject(
        IQueryBinding binding,
        SchematronAssertion assertion,
        RuleScope rule,
        IProblemSink problems,
        out QueryExpression? subject
    )
    {
        var text = assertion.Subject is { Length: > 0 } own ? own : rule.Rule.Subject;
        subject = null;
        return text is not { Length: > 0 }
            || Compile(binding, text, QueryExpressionKind.Nodes, rule.Scope, problems, out subject);
    }

    private static bool Compile(
        IQueryBinding binding,
        string text,
        QueryExpressionKind kind,
        QueryStaticScope scope,
        IProblemSink problems,
        out QueryExpression? expression
    )
    {
        if (binding.TryCompile(text.AsSpan(), kind, scope, out var compiled, out var problem))
        {
            expression = compiled;
            return true;
        }

        problems.Add(in problem);
        expression = null;
        return false;
    }

    private static bool TryCompileMessage(
        IQueryBinding binding,
        string id,
        SchematronMessage message,
        QueryStaticScope scope,
        IProblemSink problems,
        out CompiledMessage? compiled,
        string? role = null,
        string? scheme = null,
        string? fpi = null,
        string? icon = null,
        string? see = null
    )
    {
        var ok = TryCompileParts(binding, message.Parts, scope, problems, out var parts);
        compiled = ok
            ? new CompiledMessage(id, message.Location, parts!, message.Language, role, scheme, fpi, icon, see)
            : null;
        return ok;
    }

    private static bool TryCompileParts(
        IQueryBinding binding,
        IReadOnlyList<SchematronMessagePart> source,
        QueryStaticScope scope,
        IProblemSink problems,
        out MessagePart[]? parts
    )
    {
        var ok = true;
        var compiled = new MessagePart[source.Count];
        for (var i = 0; i < source.Count; i++)
        {
            if (TryCompilePart(binding, source[i], scope, problems) is { } part)
            {
                compiled[i] = part;
            }
            else
            {
                ok = false;
            }
        }

        parts = ok ? compiled : null;
        return ok;
    }

    // The problem of a part is reported by the binding; null means the part did not compile.
    private static MessagePart? TryCompilePart(
        IQueryBinding binding,
        SchematronMessagePart part,
        QueryStaticScope scope,
        IProblemSink problems
    )
    {
        switch (part)
        {
            case SchematronText text:
                return new MessagePart(MessagePartKind.Text, text.Location, text: text.Text);
            case SchematronValueOf valueOf:
                return Compile(binding, valueOf.Select, QueryExpressionKind.Value, scope, problems, out var select)
                    ? new MessagePart(MessagePartKind.ValueOf, valueOf.Location, expression: select)
                    : null;
            case SchematronCopyOf copyOf:
                return Compile(binding, copyOf.Select, QueryExpressionKind.Value, scope, problems, out var copied)
                    ? new MessagePart(MessagePartKind.CopyOf, copyOf.Location, expression: copied)
                    : null;
            case SchematronName name:
                QueryExpression? path = null;
                return
                    name.Path is null
                    || Compile(binding, name.Path, QueryExpressionKind.Value, scope, problems, out path)
                    ? new MessagePart(MessagePartKind.Name, name.Location, expression: path)
                    : null;
            case SchematronEmphasis emphasis:
                return Inline(binding, MessagePartKind.Emphasis, emphasis, null, emphasis.Parts, scope, problems);
            case SchematronDirection direction:
                return Inline(
                    binding,
                    MessagePartKind.Direction,
                    direction,
                    direction.Value,
                    direction.Parts,
                    scope,
                    problems
                );
            case SchematronSpan span:
                return Inline(binding, MessagePartKind.Span, span, span.StyleClass, span.Parts, scope, problems);
            default:
                throw new NotSupportedException($"The message part {part.GetType().Name} is not known.");
        }
    }

    private static MessagePart? Inline(
        IQueryBinding binding,
        MessagePartKind kind,
        SchematronMessagePart element,
        string? attribute,
        IReadOnlyList<SchematronMessagePart> children,
        QueryStaticScope scope,
        IProblemSink problems
    ) =>
        TryCompileParts(binding, children, scope, problems, out var parts)
            ? new MessagePart(kind, element.Location, attribute: attribute, children: parts)
            : null;

    private static bool TryCompileReferences(
        IQueryBinding binding,
        SchematronAssertion assertion,
        SchematronSchema schema,
        bool isProperty,
        QueryStaticScope scope,
        IProblemSink problems,
        out CompiledMessage[]? references
    )
    {
        var ids = isProperty ? assertion.PropertyIds : assertion.DiagnosticIds;
        var ok = true;
        var compiled = new CompiledMessage[ids.Count];
        for (var i = 0; i < ids.Count; i++)
        {
            var found = Find(schema, ids[i], isProperty);
            if (found is null)
            {
                var problem = new SchematronProblem(
                    SchematronDiagnosticCodes.IdrefUnresolved,
                    $"The assertion names the {(isProperty ? "property" : "diagnostic")} '{ids[i]}', which is not declared.",
                    assertion.Location
                );
                problems.Add(in problem);
                ok = false;
            }
            else if (
                TryCompileMessage(
                    binding,
                    ids[i],
                    found.Value.Message,
                    scope,
                    problems,
                    out var message,
                    found.Value.Role,
                    found.Value.Scheme,
                    found.Value.Fpi,
                    found.Value.Icon,
                    found.Value.See
                )
            )
            {
                compiled[i] = message!;
            }
            else
            {
                ok = false;
            }
        }

        references = ok ? compiled : null;
        return ok;
    }

    private static (
        SchematronMessage Message,
        string? Role,
        string? Scheme,
        string? Fpi,
        string? Icon,
        string? See
    )? Find(SchematronSchema schema, string id, bool isProperty)
    {
        if (isProperty)
        {
            var properties = schema.Properties?.Items;
            for (var i = 0; properties is not null && i < properties.Count; i++)
            {
                if (string.Equals(properties[i].Id, id, StringComparison.Ordinal))
                {
                    var property = properties[i];
                    return (
                        property.Message,
                        property.Role,
                        property.Scheme,
                        property.Fpi,
                        property.Icon,
                        property.See
                    );
                }
            }

            return null;
        }

        var diagnostics = schema.Diagnostics?.Items;
        for (var i = 0; diagnostics is not null && i < diagnostics.Count; i++)
        {
            if (string.Equals(diagnostics[i].Id, id, StringComparison.Ordinal))
            {
                var diagnostic = diagnostics[i];
                return (diagnostic.Message, null, null, diagnostic.Fpi, diagnostic.Icon, diagnostic.See);
            }
        }

        return null;
    }
}
