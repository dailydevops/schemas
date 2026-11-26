namespace NetEvolve.Schemas.Xml.Tests.Integration.Libxml2;

using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.Xml.Regex.Contracts;
using NetEvolve.Schemas.Xml.Regex.Matching;
using NetEvolve.Schemas.Xml.Regex.Parsing;
using NetEvolve.Schemas.Xml.Text.CharClasses;

/// <summary>Runs the cases of the libxml2 regexp files on the XSD dialect of the regex engine.</summary>
internal static class Libxml2RegexRunner
{
    private static readonly RegexSyntax Syntax = new(RegexDialect.Xsd, NameEdition.Xml10FifthEdition);

    /// <summary>Runs the cases.</summary>
    /// <param name="cases">The cases; a compile case precedes the match cases of its pattern.</param>
    /// <param name="cancellationToken">A token to cancel the run.</param>
    /// <returns>One result per case.</returns>
    public static List<CaseResult> Run(IReadOnlyList<Libxml2RegexCase> cases, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(cases);
        var results = new List<CaseResult>(cases.Count);
        RegexProgram? program = null;
        foreach (var c in cases)
        {
            if (c.Input is null)
            {
                var error = Compile(c.Pattern, out program);
                results.Add(Judge(c, (error == RegexError.None) == c.Expected, $"The pattern gives {error}."));
            }
            else if (program is null)
            {
                results.Add(new CaseResult(c.Id, c.TestSet, CaseOutcome.Skipped, "The pattern does not compile."));
            }
            else
            {
                var match = program.Match(c.Input.AsSpan(), RegexStepLimits.Default);
                results.Add(Judge(c, (match == RegexMatchResult.Match) == c.Expected, $"The match result is {match}."));
            }
        }

        return results;
    }

    private static CaseResult Judge(Libxml2RegexCase c, bool passed, string reason) =>
        passed
            ? new CaseResult(c.Id, c.TestSet, CaseOutcome.Pass)
            : new CaseResult(c.Id, c.TestSet, CaseOutcome.Fail, reason);

    private static RegexError Compile(string pattern, out RegexProgram? program)
    {
        var parsed = RegexParser.Parse(pattern.AsSpan(), Syntax);
        if (!parsed.Succeeded)
        {
            program = null;
            return parsed.Error;
        }

        return RegexProgram.TryCompile(parsed.Tree!, out program);
    }
}
