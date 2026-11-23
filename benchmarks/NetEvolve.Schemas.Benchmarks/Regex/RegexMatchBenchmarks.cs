namespace NetEvolve.Schemas.Benchmarks.Regex;

using BenchmarkDotNet.Attributes;
using NetEvolve.Schemas.Xml.Regex.Contracts;
using NetEvolve.Schemas.Xml.Regex.Matching;

/// <summary>
/// Matching an accepted input against each compiled pattern of the set, against <c>System.Xml.Schema</c> pattern facet
/// validation of the same inputs. Both sides compile outside of the measurement.
/// </summary>
#pragma warning disable CA1515 // BenchmarkDotNet requires public benchmark classes
public class RegexMatchBenchmarks
#pragma warning restore CA1515
{
    private readonly string[] _inputs = RegexBenchmarkInput.Inputs;
    private readonly RegexProgram[] _programs = RegexBenchmarkCompiler.CompileAll(RegexBenchmarkInput.Patterns);
    private readonly SystemXmlPatternValidation _systemXml = new(RegexBenchmarkInput.Patterns);

    /// <summary>Proves that both engines accept every input of the set.</summary>
    [GlobalSetup]
    public void Setup()
    {
        var count = _inputs.Length;
        if (_systemXml.Validate(_inputs) != count || MatchCompiled() != count)
        {
            throw new InvalidOperationException("The engines disagree on the accepted inputs.");
        }
    }

    /// <summary>Baseline: <c>System.Xml.Schema</c> validates each input against its pattern.</summary>
    /// <returns>The number of accepted inputs.</returns>
    [Benchmark(Baseline = true)]
    public int MatchCompiledSystemXml() => _systemXml.Validate(_inputs);

    /// <summary>Matches each input against its compiled pattern.</summary>
    /// <returns>The number of accepted inputs.</returns>
    [Benchmark]
    public int MatchCompiled()
    {
        var limits = RegexStepLimits.Default;
        var matched = 0;
        for (var i = 0; i < _programs.Length; i++)
        {
            if (_programs[i].Match(_inputs[i].AsSpan(), limits) == RegexMatchResult.Match)
            {
                matched++;
            }
        }

        return matched;
    }
}
