namespace NetEvolve.Schemas.Schematron.Tests.Unit.Contract;

using System;
using System.Linq;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

public class SeamShapeTests
{
    private const string SeamNamespace = "NetEvolve.Schemas.Schematron.Seams";

    private static readonly string[] Interfaces =
    [
        "IQueryBinding",
        "IQueryRun",
        "IPatternMatcher",
        "IXsltInstruction",
        "IXsltInstructionRegistry",
        "ISequenceSink",
        "IModuleLoader",
        "ISchematronPipeline",
        "IModelParser",
        "IModelResolver",
        "IModelExpander",
        "ISchemaCompiler",
        "IEvaluationEvents",
        "IProblemSink",
    ];

    private static readonly string[] SupportTypes =
    [
        "SchematronProblem",
        "QueryFocus",
        "QueryStaticScope",
        "QueryStatus",
        "QueryExpression",
        "QueryExpressionKind",
        "QueryResources",
        "QueryNodeKinds",
        "CompiledPattern",
        "PatternUse",
        "XsltCompileContext",
        "XsltFrame",
        "XsltExecutable",
        "ModuleReference",
        "ModuleKind",
        "LoadedModule",
        "BuildSettings",
        "ParsedModule",
        "ResolvedModel",
        "CompiledSchema",
    ];

    private static Type Seam(string name) =>
        typeof(SchematronValidatorBuilder).Assembly.GetType(SeamNamespace + "." + name)
        ?? throw new InvalidOperationException("Missing seam type " + name);

    [Test]
    public async Task Seams_Interfaces_ExistAndAreInternal()
    {
        foreach (var name in Interfaces)
        {
            var type = Seam(name);

            await Assert.That(type.IsInterface).IsTrue();
            await Assert.That(type.IsPublic).IsFalse();
        }
    }

    [Test]
    public async Task Seams_SupportTypes_ExistAndAreInternal()
    {
        foreach (var name in SupportTypes)
        {
            await Assert.That(Seam(name).IsPublic).IsFalse();
            await Assert.That(Seam(name).IsNestedPublic).IsFalse();
        }
    }

    [Test]
    public async Task Seams_NoPublicTypeLivesInTheSeamNamespace()
    {
        var publicTypes = typeof(SchematronValidatorBuilder)
            .Assembly.GetExportedTypes()
            .Where(type => string.Equals(type.Namespace, SeamNamespace, StringComparison.Ordinal));

        await Assert.That(publicTypes.Count()).IsEqualTo(0);
    }

    [Test]
    public async Task Seams_QueryBinding_HasTheDesignedMembers()
    {
        var binding = Seam("IQueryBinding");

        await Assert.That(binding.GetMethod("TryCompile")!.GetParameters().Length).IsEqualTo(5);
        await Assert.That(binding.GetMethod("BeginRun")!.GetParameters().Length).IsEqualTo(2);
        await Assert.That(binding.GetProperty("Name")!.PropertyType).IsEqualTo(typeof(string));
        await Assert.That(binding.GetProperty("AllowsXsltInstructions")!.PropertyType).IsEqualTo(typeof(bool));
    }

    [Test]
    public async Task QueryFocus_Size_IsMinusOneUntilRequested()
    {
        var focus = new QueryFocus(default, 1, -1);

        await Assert.That(focus.Size).IsEqualTo(-1);
        await Assert.That(focus.Position).IsEqualTo(1);
        await Assert.That(focus.Node).IsEqualTo(default(XPathNode));
    }

    [Test]
    public async Task Seams_InputProblems_NeverThrow()
    {
        var binding = new FakeQueryBinding();
        var scope = new QueryStaticScope([], [], null, SchematronEdition.Iso2016);

        var ok = binding.TryCompile("a".AsSpan(), QueryExpressionKind.Boolean, scope, out var expression, out _);
        var bad = binding.TryCompile(
            ReadOnlySpan<char>.Empty,
            QueryExpressionKind.Boolean,
            scope,
            out _,
            out var problem
        );
        var run = binding.BeginRun(new QueryResources(DenyAllResourceResolver.Instance), default);
        var status = run.TryEvaluateBoolean(new FakeQueryExpression("bad"), new QueryFocus(default, 1, -1), out _);

        await Assert.That(ok).IsTrue();
        await Assert.That(expression).IsTypeOf<FakeQueryExpression>();
        await Assert.That(bad).IsFalse();
        await Assert.That(problem.Code).IsEqualTo(SchematronDiagnosticCodes.ExpressionInvalid);
        await Assert.That(problem.Constraint).IsEqualTo("XPST0003");
        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(run.GetProblem().Code).IsEqualTo(SchematronDiagnosticCodes.EvaluationFailed);
    }

    [Test]
    public async Task FakeBinding_RunsExpressionsAndBindsVariables()
    {
        var binding = new FakeQueryBinding();
        var scope = new QueryStaticScope([], ["v"], "base.sch", SchematronEdition.Iso2025);
        _ = binding.TryCompile("1".AsSpan(), QueryExpressionKind.Value, scope, out var expression, out _);
        var run = binding.BeginRun(new QueryResources(DenyAllResourceResolver.Instance), default);
        var sink = new FakeSequenceSink();
        var focus = new QueryFocus(default, 1, -1);

        var boolStatus = run.TryEvaluateBoolean(expression, in focus, out var value);
        var evalStatus = run.TryEvaluate(expression, in focus, sink);
        var bindStatus = run.TryBindVariable(0, expression, in focus);
        run.BindVariable(1, "DE");
        run.Dispose();

        await Assert.That(boolStatus).IsEqualTo(QueryStatus.Ok);
        await Assert.That(value).IsTrue();
        await Assert.That(evalStatus).IsEqualTo(QueryStatus.Ok);
        await Assert.That(bindStatus).IsEqualTo(QueryStatus.Ok);
        await Assert.That(sink.Texts[0]).IsEqualTo("1");
        await Assert
            .That(binding.LastRun!.Calls)
            .IsEquivalentTo(["boolean", "evaluate", "bind-expression", "bind-literal:DE"]);
        await Assert.That(binding.LastRun!.Disposed).IsTrue();
        await Assert.That(scope.BaseUri).IsEqualTo("base.sch");
        await Assert.That(scope.Edition).IsEqualTo(SchematronEdition.Iso2025);
        await Assert.That(scope.VariableNames[0]).IsEqualTo("v");
        await Assert.That(scope.Namespaces.Count).IsEqualTo(0);
    }

    [Test]
    public async Task QueryResources_WrapsTheCoreResolverOnce()
    {
        var resolver = new Testing.Fakes.InMemoryResourceResolver();

        var resources = new QueryResources(resolver);

        await Assert.That(resources.Resolver).IsSameReferenceAs(resolver);
        await Assert.That(resources.XPathResolver).IsNotNull();
        await Assert.That(() => new QueryResources(null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task FakeMatcher_PrefiltersByNodeKindAndName()
    {
        var matcher = new FakePatternMatcher();
        var scope = new QueryStaticScope([], [], null, SchematronEdition.Iso2016);
        var binding = new FakeQueryBinding();
        var run = binding.BeginRun(new QueryResources(DenyAllResourceResolver.Instance), default);

        var compiled = matcher.TryCompile("invoice".AsSpan(), PatternUse.RuleContext, scope, out var pattern, out _);
        var status = matcher.Matches(pattern, run, default(XPathNode), out var matched);

        await Assert.That(compiled).IsTrue();
        await Assert.That(pattern.NodeKinds).IsEqualTo(QueryNodeKinds.Element);
        await Assert.That(pattern.CandidateNames.Length).IsEqualTo(1);
        await Assert.That(pattern.CandidateNames[0]).IsEqualTo(new XPathQName(string.Empty, "invoice"));
        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        await Assert.That(matched).IsTrue();
        await Assert.That(matcher.MatchCalls).IsEqualTo(1);
    }

    [Test]
    public async Task FakeRegistry_LooksUpInstructionsByExpandedName()
    {
        var name = new XPathQName("http://www.w3.org/1999/XSL/Transform", "value-of");
        var instruction = new FakeXsltInstruction(name);
        var registry = new FakeXsltInstructionRegistry();
        registry.Register(instruction);
        var scope = new QueryStaticScope([], [], null, SchematronEdition.Iso2016);
        var context = new XsltCompileContext(default, scope, static _ => null, new FakeProblemSink());
        var binding = new FakeQueryBinding();
        var frame = new XsltFrame(binding.BeginRun(new QueryResources(DenyAllResourceResolver.Instance), default));
        var sink = new FakeSequenceSink();

        var found = registry.TryGet(in name, out var resolved);
        var compiled = resolved.TryCompile(context, out var executable, out _);
        var status = executable.TryExecute(frame, sink);
        var other = new XPathQName("http://www.w3.org/1999/XSL/Transform", "unknown");

        await Assert.That(found).IsTrue();
        await Assert.That(compiled).IsTrue();
        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        await Assert.That(instruction.Executable.Executions).IsEqualTo(1);
        await Assert.That(registry.IsAvailable(in name)).IsTrue();
        await Assert.That(registry.IsAvailable(in other)).IsFalse();
        await Assert.That(registry.TryGet(in other, out _)).IsFalse();
        await Assert.That(sink.Texts[0]).IsEqualTo("done");
        await Assert.That(context.Scope).IsSameReferenceAs(scope);
        await Assert.That(context.CompileChild(default)).IsNull();
        await Assert.That(context.Problems).IsNotNull();
        await Assert.That(context.Element).IsEqualTo(default(XPathNode));
        await Assert.That(() => new XsltFrame(null!)).Throws<ArgumentNullException>();
        await Assert
            .That(() => new XsltCompileContext(default, null!, static _ => null, new FakeProblemSink()))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task FakeLoader_ReportsAProblemInsteadOfThrowing()
    {
        var loader = new FakeModuleLoader();
        var at = new DocumentLocation("main.sch", 2, 3);

        var ok = loader.TryLoad(
            new ModuleReference(ModuleKind.SchematronInclude, "lib.sch", "main.sch", at),
            default,
            out var module,
            out _
        );
        var missing = loader.TryLoad(
            new ModuleReference(ModuleKind.XsltImport, "missing.sch", null, at),
            default,
            out _,
            out var problem
        );

        await Assert.That(ok).IsTrue();
        await Assert.That(module.SystemId).IsEqualTo("lib.sch");
        await Assert.That(missing).IsFalse();
        await Assert.That(problem.Code).IsEqualTo(CoreDiagnosticCodes.ResourceNotFound);
        await Assert.That(problem.Location).IsEqualTo(at);
        await Assert.That(problem.Message).IsEqualTo("not found");
        await Assert.That(loader.Loaded).IsEquivalentTo(["lib.sch", "missing.sch"]);
        await Assert.That(new ModuleReference(ModuleKind.XsltInclude, "x", "b", at).BaseUri).IsEqualTo("b");
        await Assert
            .That(new ModuleReference(ModuleKind.SchematronExtends, "x", null, at).Kind)
            .IsEqualTo(ModuleKind.SchematronExtends);
    }

    [Test]
    public async Task FakeStages_AreExercisedOnce()
    {
        var problems = new FakeProblemSink();
        var parser = new FakeModelParser();
        var resolver = new FakeModelResolver();
        var expander = new FakeModelExpander();
        var compiler = new FakeSchemaCompiler();
        var settings = new BuildSettings(
            "basic",
            new System.Collections.Generic.Dictionary<string, string>(StringComparer.Ordinal),
            ValidationLimits.Default,
            DenyAllResourceResolver.Instance
        );

        _ = parser.TryParse(SchemaSourceHolder.FromChars("x".AsSpan(), null), problems, out var parsed);
        _ = resolver.TryResolve(parsed, new FakeModuleLoader(), problems, out var resolved);
        _ = expander.TryExpand(resolved, settings, problems, out var effective);
        _ = compiler.TryCompile(effective, settings, problems, out var compiled);

        await Assert.That(parser.Calls).IsEqualTo(1);
        await Assert.That(resolver.Calls).IsEqualTo(1);
        await Assert.That(expander.Calls).IsEqualTo(1);
        await Assert.That(compiler.Calls).IsEqualTo(1);
        await Assert.That(compiled.EffectivePhase).IsEqualTo("basic");
        await Assert.That(problems.Problems.Count).IsEqualTo(0);
    }

    [Test]
    public async Task BuildSettings_And_CompiledSchema_RejectNulls()
    {
        var parameters = new System.Collections.Generic.Dictionary<string, string>(StringComparer.Ordinal);

        await Assert
            .That(() =>
                new BuildSettings(null!, parameters, ValidationLimits.Default, DenyAllResourceResolver.Instance)
            )
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => new BuildSettings("p", null!, ValidationLimits.Default, DenyAllResourceResolver.Instance))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => new BuildSettings("p", parameters, null!, DenyAllResourceResolver.Instance))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => new BuildSettings("p", parameters, ValidationLimits.Default, null!))
            .Throws<ArgumentNullException>();
        await Assert.That(() => new CompiledSchema(null!, "p")).Throws<ArgumentNullException>();
        await Assert.That(() => new CompiledSchema(FakeModel.Schema(), null!)).Throws<ArgumentNullException>();
        await Assert.That(() => SchemaSourceHolder.FromStream(null!, null)).Throws<ArgumentNullException>();
        await Assert
            .That(() => new SchematronValidatorBuilder(SchemaSourceHolder.FromChars("x".AsSpan(), null)).UsePipeline(null!))
            .Throws<ArgumentNullException>();
    }
}
