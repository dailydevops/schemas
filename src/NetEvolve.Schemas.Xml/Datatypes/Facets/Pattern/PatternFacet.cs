namespace NetEvolve.Schemas.Xml.Datatypes.Facets.Pattern;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Regex.Contracts;
using NetEvolve.Schemas.Xml.Regex.Matching;
using NetEvolve.Schemas.Xml.Regex.Parsing;
using NetEvolve.Schemas.Xml.Text.CharClasses;

/// <summary>
/// The <c>pattern</c> facet of one derivation step (XSD 1.1 Part 2, section 4.3.4): a lexical form satisfies it when the whole of it
/// matches at least one of the patterns of the step, so several patterns are alternatives. The patterns of different derivation
/// steps are different facets in the facet list of the type, and a value must satisfy every one of them. Each pattern is parsed and
/// compiled once, when the facet is created.
/// </summary>
/// <remarks>
/// Limitations: the facet checks the lexical form it is given, which a descriptor has already normalized by its whitespace mode.
/// A match that reaches the step ceiling of the <see cref="RegexStepLimits"/> is reported as <see cref="PatternDiagnosticCode.StepLimitExceeded"/>
/// and the value is treated as not validated, although the specification defines an answer for it; <see cref="IsSatisfiedBy"/> is then
/// <see langword="false"/>. The syntax is the XSD dialect of the regex parser, so its limits apply (pattern length, nesting, program size,
/// and no back references or reluctant quantifiers; <c>^</c> and <c>$</c> are literal characters). In both XSD modes <c>\i \I \c \C</c> read the XML 1.0 Fifth Edition name
/// characters, as the XSD test suite expects since its 2011 update; the name datatypes keep the Second Edition in the 1.0 mode. Whether a pattern is a valid restriction of the pattern of the base type is not
/// checked, as the specification does not define it either.
/// </remarks>
public sealed class PatternFacet : ILexicalFacet
{
    private readonly RegexProgram[] _programs;
    private readonly string[] _patterns;
    private readonly RegexStepLimits _limits;

    private PatternFacet(RegexProgram[] programs, string[] patterns, RegexStepLimits limits)
    {
        _programs = programs;
        _patterns = patterns;
        _limits = limits;
    }

    /// <inheritdoc />
    public FacetKind Kind => FacetKind.Pattern;

    /// <summary>Gets the patterns of the step, in the order they were given.</summary>
    public IReadOnlyList<string> Patterns => _patterns;

    /// <summary>Creates the facet by compiling every pattern.</summary>
    /// <param name="patterns">The patterns of one derivation step, at least one.</param>
    /// <param name="version">The XSD version, which does not change the name characters behind <c>\i \I \c \C</c>: both modes use the Fifth Edition.</param>
    /// <param name="limits">The step ceilings of a match, <see langword="null"/> for <see cref="RegexStepLimits.Default"/>.</param>
    /// <param name="facet">The facet, <see langword="null"/> when a pattern is invalid.</param>
    /// <param name="diagnostic">The diagnostic of the first invalid pattern, with the position in it; none when the facet is created.</param>
    /// <returns><see langword="true"/> when every pattern compiled.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="patterns"/> or one of its elements is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="patterns"/> is empty.</exception>
    public static bool TryCreate(
        IReadOnlyList<string> patterns,
        XsdVersion version,
        RegexStepLimits? limits,
        out PatternFacet? facet,
        out PatternDiagnostic diagnostic
    )
    {
        ArgumentNullException.ThrowIfNull(patterns);
        ArgumentOutOfRangeException.ThrowIfLessThan(patterns.Count, 1);

        // The multi-character escapes use the Fifth Edition in both XSD modes; only the name datatypes split by version.
        _ = version; // kept for the public signature; the escapes do not depend on it
        var syntax = new RegexSyntax(RegexDialect.Xsd, NameEdition.Xml10FifthEdition);
        var programs = new RegexProgram[patterns.Count];
        var texts = new string[patterns.Count];
        for (var i = 0; i < programs.Length; i++)
        {
            var pattern = patterns[i];
            ArgumentNullException.ThrowIfNull(pattern);
            texts[i] = pattern;
            var program = Compile(pattern, syntax, out var error, out var position);
            if (program is null)
            {
                facet = null;
                diagnostic = new(PatternDiagnosticCode.InvalidPattern, pattern, i, error, position);
                return false;
            }

            programs[i] = program;
        }

        facet = new PatternFacet(programs, texts, limits ?? RegexStepLimits.Default);
        diagnostic = default;
        return true;
    }

    /// <inheritdoc />
    public bool IsSatisfiedBy(ReadOnlySpan<char> lexical) => Match(lexical, out _);

    /// <summary>Determines whether a lexical form satisfies the facet and reports why it does not when a step ceiling was reached.</summary>
    /// <param name="lexical">The lexical form, already whitespace-normalized.</param>
    /// <param name="diagnostic"><see cref="PatternDiagnosticCode.StepLimitExceeded"/> with the first pattern that reached the ceiling when no pattern matched and one reached it; none otherwise.</param>
    /// <returns><see langword="true"/> when the whole lexical form matches at least one pattern.</returns>
    /// <remarks>A pattern that matches within its ceiling decides the answer, whatever the order of the patterns.</remarks>
    public bool Match(ReadOnlySpan<char> lexical, out PatternDiagnostic diagnostic)
    {
        var aborted = -1;
        for (var i = 0; i < _programs.Length; i++)
        {
            var result = _programs[i].Match(lexical, _limits);
            if (result == RegexMatchResult.Match)
            {
                diagnostic = default;
                return true;
            }

            if (result == RegexMatchResult.Aborted && aborted < 0)
            {
                aborted = i;
            }
        }

        diagnostic =
            aborted < 0
                ? default
                : new(PatternDiagnosticCode.StepLimitExceeded, _patterns[aborted], aborted, RegexError.None, -1);
        return false;
    }

    private static RegexProgram? Compile(string pattern, RegexSyntax syntax, out RegexError error, out int position)
    {
        var parsed = RegexParser.Parse(pattern.AsSpan(), syntax);
        if (!parsed.Succeeded)
        {
            error = parsed.Error;
            position = parsed.Position;
            return null;
        }

        var tree = parsed.Tree!;
        var sets = new RegexCharacterSet[tree.SetCount];
        for (var i = 0; i < sets.Length; i++)
        {
            sets[i] = tree.GetSet(i);
        }

        error = RegexProgram.TryCompile(tree.Nodes, tree.Root, sets, RegexDialect.Xsd, out var program);
        position = 0;
        return program;
    }
}
