namespace NetEvolve.Schemas.Schematron.Evaluation.Annotations;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>
/// Compiles the <c>role</c>, <c>flag</c>, and <c>severity</c> attributes of one element (design #150 EP-6). An attribute whose whole
/// value is a variable reference, <c>$name</c>, is dynamic (decided by #106 Q3): the reference is compiled as a value expression in the
/// scope where the attribute is written, so a variable of a rule is never visible to the attribute of its pattern, and a name that is
/// not declared there is reported by the binding. Every other value is static and copied as written; the tokens of a <c>flag</c> are
/// joined with one space once, here.
/// </summary>
/// <remarks>
/// A value counts as a variable reference when, after the surrounding white space is removed, it is a dollar sign followed by letters,
/// digits, and the characters <c>_ - . :</c>; anything else, such as <c>$a + $b</c> or a lone dollar sign, is static text, because
/// Q3 rules out full attribute value templates. A <c>flag</c> is dynamic only when it is one token. The <c>role</c> of a
/// <c>diagnostic</c> or <c>property</c> is not compiled here (design #150 EP-6: copied verbatim, a documented limitation).
/// </remarks>
internal static class AnnotationCompiler
{
    /// <summary>Compiles the attributes of one element; every problem is reported and compiling continues, so one call finds them all.</summary>
    /// <param name="role">The <c>role</c> attribute, or <see langword="null"/> when absent.</param>
    /// <param name="flags">The <c>flag</c> tokens, or <see langword="null"/> or empty when absent.</param>
    /// <param name="severity">The <c>severity</c> attribute, or <see langword="null"/> when absent.</param>
    /// <param name="location">The location of the element, which problems and failed values name.</param>
    /// <param name="scope">The scope where the attributes are written: the rule scope for an assertion or a rule, the pattern scope for a pattern or group.</param>
    /// <param name="binding">The binding that compiles a variable reference.</param>
    /// <param name="problems">Receives every problem.</param>
    /// <param name="compiled">The compiled attributes; valid only when the method returns <see langword="true"/>.</param>
    /// <returns><see langword="true"/> when no problem was reported.</returns>
    internal static bool TryCompile(
        string? role,
        IReadOnlyList<string>? flags,
        string? severity,
        DocumentLocation location,
        QueryStaticScope scope,
        IQueryBinding binding,
        IProblemSink problems,
        out CompiledAnnotations compiled
    )
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(binding);
        ArgumentNullException.ThrowIfNull(problems);

        var ok = TryCompile(AnnotationKind.Role, role, location, scope, binding, problems, out var compiledRole);
        var flagText = flags is { Count: > 0 } ? (flags.Count == 1 ? flags[0] : string.Join(" ", flags)) : null;
        ok &= TryCompile(AnnotationKind.Flag, flagText, location, scope, binding, problems, out var compiledFlags);
        ok &= TryCompile(
            AnnotationKind.Severity,
            severity,
            location,
            scope,
            binding,
            problems,
            out var compiledSeverity
        );

        compiled = ok
            ? compiledRole is null && compiledFlags is null && compiledSeverity is null
                ? CompiledAnnotations.None
                : new CompiledAnnotations(compiledRole, compiledFlags, compiledSeverity)
            : null!;
        return ok;
    }

    private static bool TryCompile(
        AnnotationKind kind,
        string? text,
        DocumentLocation location,
        QueryStaticScope scope,
        IQueryBinding binding,
        IProblemSink problems,
        out CompiledAnnotation? compiled
    )
    {
        compiled = null;
        if (text is null)
        {
            return true;
        }

        var reference = text.AsSpan().Trim();
        if (!IsVariableReference(reference))
        {
            compiled = CompiledAnnotation.CreateStatic(kind, text, location);
            return true;
        }

        if (!binding.TryCompile(reference, QueryExpressionKind.Value, scope, out var expression, out var problem))
        {
            problem = problem.At(location);
            problems.Add(in problem);
            return false;
        }

        compiled = CompiledAnnotation.CreateDynamic(kind, reference.ToString(), expression, location);
        return true;
    }

    private static bool IsVariableReference(ReadOnlySpan<char> text)
    {
        if (text.Length < 2 || text[0] != '$')
        {
            return false;
        }

        for (var i = 1; i < text.Length; i++)
        {
            var c = text[i];
            if (!char.IsLetterOrDigit(c) && c is not ('_' or '-' or '.' or ':'))
            {
                return false;
            }
        }

        return true;
    }
}
