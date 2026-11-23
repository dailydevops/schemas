namespace NetEvolve.Schemas.Benchmarks.Regex;

using NetEvolve.Schemas.Xml.Regex.Contracts;
using NetEvolve.Schemas.Xml.Regex.Matching;
using NetEvolve.Schemas.Xml.Regex.Parsing;
using NetEvolve.Schemas.Xml.Text.CharClasses;

/// <summary>Compiles patterns for the XSD dialect the way the pattern facet does.</summary>
internal static class RegexBenchmarkCompiler
{
    private static readonly RegexSyntax Syntax = new(RegexDialect.Xsd, NameEdition.Xml10FifthEdition);

    /// <summary>Compiles one pattern.</summary>
    /// <param name="pattern">The pattern, in XSD syntax.</param>
    /// <returns>The program.</returns>
    /// <exception cref="InvalidOperationException">The pattern does not compile.</exception>
    public static RegexProgram Compile(string pattern)
    {
        var parsed = RegexParser.Parse(pattern.AsSpan(), Syntax);
        if (
            !parsed.Succeeded
            || RegexProgram.TryCompile(parsed.Tree!, out var program) != RegexError.None
            || program is null
        )
        {
            throw new InvalidOperationException($"The pattern '{pattern}' does not compile.");
        }

        return program;
    }

    /// <summary>Compiles all patterns.</summary>
    /// <param name="patterns">The patterns, in XSD syntax.</param>
    /// <returns>The programs, in order.</returns>
    public static RegexProgram[] CompileAll(string[] patterns)
    {
        var programs = new RegexProgram[patterns.Length];
        for (var i = 0; i < programs.Length; i++)
        {
            programs[i] = Compile(patterns[i]);
        }

        return programs;
    }
}
