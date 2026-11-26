namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.XRechnung;

using NetEvolve.Schemas.Conformance.Fetch;
using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.Resources.FileSystem;
using NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.En16931;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Model.Loading;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// Runs every instance of the pinned XRechnung test suite through the scenario chain of the pinned KoSIT validator configuration: the
/// scenario is selected by its match expression, then the XML Schema, the CEN rules and the XRechnung rules run in the order of the scenario.
/// </summary>
/// <remarks>
/// The test suite holds only instances that are valid for XRechnung, so every step is expected to be valid. A step whose rule set does not
/// build fails the instance with <see cref="XRechnungRun.BuildBlockedPrefix"/>, a known failure and never a pass. Limitation: the report
/// step of the scenario (the XSLT report transform) is not run; only the verdicts of the validation steps count.
/// </remarks>
internal static class KositScenarioRun
{
    /// <summary>The key of the configuration manifest.</summary>
    public const string ConfigurationKey = "kosit-xrechnung-configuration";

    /// <summary>The test set of the results.</summary>
    public const string TestSet = "scenarios";

    /// <summary>The package name of the rate rows in the job summary.</summary>
    public const string RatePackage = "NetEvolve.Schemas.Schematron (KoSIT scenario goldens)";

    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromMinutes(10) };
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static IReadOnlyList<CaseResult>? s_results;
    private static string? s_configuration;

    /// <summary>Fetches the configuration by its pinned checksum, or reuses the cache.</summary>
    /// <param name="cancellationToken">A token to cancel the fetch.</param>
    /// <returns>The configuration root.</returns>
    public static async Task<string> GetConfigurationAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Gate.WaitAsync(cancellationToken);
        try
        {
            return s_configuration ??= await new SuiteFetcher(
                new HttpSuiteTransport(Client),
                ConformanceCache.ResolveRoot()
            ).FetchAsync(XRechnungRun.LoadManifest(ConfigurationKey), cancellationToken);
        }
        finally
        {
            _ = Gate.Release();
        }
    }

    /// <summary>Reads the scenarios of a configuration.</summary>
    /// <param name="configuration">The configuration root.</param>
    /// <returns>The scenarios in file order.</returns>
    public static IReadOnlyList<KositScenario> LoadScenarios(string configuration) =>
        KositScenarioReader.Read(File.ReadAllText(Path.Combine(configuration, "scenarios.xml")));

    /// <summary>Finds the first scenario whose match expression holds for an instance.</summary>
    /// <param name="scenarios">The scenarios in file order.</param>
    /// <param name="instance">The instance bytes.</param>
    /// <returns>The scenario, or <see langword="null"/> when none matches.</returns>
    public static KositScenario? Match(IReadOnlyList<KositScenario> scenarios, byte[] instance)
    {
        ArgumentNullException.ThrowIfNull(scenarios);
        ArgumentNullException.ThrowIfNull(instance);
        var document = XmlDocumentLoader.Load(instance.AsSpan());
        foreach (var scenario in scenarios)
        {
            var statics = XPathStaticContext.Default.ToBuilder();
            foreach (var (prefix, uri) in scenario.Namespaces)
            {
                _ = statics.WithNamespace(prefix, uri);
            }

            using var context = new XPathEvaluationContext();
            context.SetContextItem(XdmItem.FromNode(new XPathNode(document.Document)));
            if (XPathExpression.Compile(scenario.Match, statics.Build()).EvaluateBoolean(context))
            {
                return scenario;
            }
        }

        return null;
    }

    /// <summary>Gets the results of every instance.</summary>
    /// <param name="cancellationToken">A token to cancel the run.</param>
    /// <returns>One result per instance of the suite.</returns>
    public static async Task<IReadOnlyList<CaseResult>> GetResultsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var configuration = await GetConfigurationAsync(cancellationToken);
        var (rules, suite) = await XRechnungRun.GetRootsAsync(cancellationToken);
        var cen = await En16931Run.GetRootAsync(cancellationToken);
        await Gate.WaitAsync(cancellationToken);
        try
        {
            return s_results ??= await RunAsync(configuration, rules, cen, suite, cancellationToken);
        }
        finally
        {
            _ = Gate.Release();
        }
    }

    private static async Task<List<CaseResult>> RunAsync(
        string configuration,
        string rules,
        string cen,
        string suite,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var scenarios = LoadScenarios(configuration);
        var xsds = new Dictionary<string, IXsdValidator>(StringComparer.Ordinal);
        var ruleSets = new Dictionary<KositRuleSet, (ISchematronValidator? Validator, string? Blocked)>();
        var files = XRechnungRun
            .Syntaxes.SelectMany(s => XRechnungRun.Instances(suite, s.Syntax))
            .OrderBy(f => f, StringComparer.Ordinal);
        var results = new List<CaseResult>();
        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var id = Path.GetRelativePath(suite, file).Replace('\\', '/');
            var reason = await RunInstanceAsync(file);
            results.Add(
                reason is null
                    ? new CaseResult(id, TestSet, CaseOutcome.Pass)
                    : new CaseResult(id, TestSet, CaseOutcome.Fail, reason)
            );
        }

        return results;

        async Task<string?> RunInstanceAsync(string file)
        {
            var bytes = await File.ReadAllBytesAsync(file, cancellationToken);
            var scenario = Match(scenarios, bytes);
            if (scenario is null)
            {
                return "No scenario matches the instance.";
            }

            var uri = new Uri(file).AbsoluteUri;
            var schemaResult = await (await XsdOf(scenario.XsdLocation)).ValidateAsync(bytes, uri, cancellationToken);
            if (!schemaResult.IsValid)
            {
                var messages = schemaResult
                    .Issues.Where(i => i.Severity == IssueSeverity.Error)
                    .Take(3)
                    .Select(i => i.Message);
                return $"{scenario.Name}: the XML Schema step is invalid: {string.Join(" ", messages)}";
            }

            foreach (var ruleSet in scenario.RuleSets)
            {
                if (!ruleSets.TryGetValue(ruleSet, out var built))
                {
                    built = BuildRuleSet(ruleSet);
                    ruleSets[ruleSet] = built;
                }

                var problem = built.Blocked
                    ?? XRechnungRun.Validate(built.Validator!, file, cancellationToken, scenario.CustomLevels);
                if (problem is not null)
                {
                    return $"{scenario.Name}: the {ruleSet.Layer} step failed: {problem}";
                }
            }

            return null;
        }

        async Task<IXsdValidator> XsdOf(string location)
        {
            if (!xsds.TryGetValue(location, out var validator))
            {
                var path = Path.Combine(configuration, location);
                validator = await SchemaValidator
                    .ForXsd()
                    .WithVersion(XsdVersion.V10)
                    .WithResolver(new SandboxedFileSystemResourceResolver(configuration))
                    .AddSchema(await File.ReadAllBytesAsync(path, cancellationToken), new Uri(path).AbsoluteUri)
                    .BuildAsync(cancellationToken);
                xsds[location] = validator;
            }

            return validator;
        }

        (ISchematronValidator? Validator, string? Blocked) BuildRuleSet(KositRuleSet ruleSet)
        {
            try
            {
                return ruleSet.Layer == "cen"
                    ? (En16931Runner.Build(En16931Run.SchemaPath(cen, ruleSet.Syntax)), null)
                    : (XRechnungRun.Build(XRechnungRun.SchemaPath(rules, ruleSet.Syntax)), null);
            }
            catch (SchemaCompilationException exception)
            {
                var codes = exception.Issues.Select(i => i.Code.ToString()).Distinct(StringComparer.Ordinal);
                return (null, $"{XRechnungRun.BuildBlockedPrefix}: {string.Join(", ", codes)}.");
            }
        }
    }
}
