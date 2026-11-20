namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Globals;

using System;
using System.Collections.Generic;
using System.Threading;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Phases;
using NetEvolve.Schemas.Schematron.Scopes;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using NetEvolve.Schemas.Schematron.Tests.Unit.Parsing.Core;
using NetEvolve.Schemas.Schematron.Tests.Unit.Patterns;
using NetEvolve.Schemas.Schematron.Tests.Unit.Scopes;
using NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Globals;
using NetEvolve.Schemas.Schematron.Xslt.Registry;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Text.NameTable;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions.FunctionTree;
using FlowRegistration = NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow.Registration;
using FunctionsRegistration = NetEvolve.Schemas.Schematron.Xslt.Functions.Registration;

/// <summary>
/// The compile driver and the run of the global variable tests. It resolves every element through the registry that the
/// registration of the function folder fills (the global declarations are the same entries as the local ones, widened by the
/// positions), compiles through the mini binding, builds the table of the winners, plans the scopes of a schema with them, and
/// starts a run over the plan with the real slot binder. The compile stage of the package does not exist yet, so this fixture does
/// what that stage does: it hands the winners of the table to the planner.
/// </summary>
internal sealed class GlobalFixture
{
    internal const string Module = "file:///stylesheets/globals.xsl";

    internal GlobalFixture()
    {
        var builder = new XsltRegistryBuilder();
        FunctionsRegistration.Register(builder);
        FlowRegistration.Register(builder);
        Registry = builder.Build();
        Scope = new QueryStaticScope([], [], Module, SchematronEdition.Iso2016);
    }

    internal MiniBinding Binding { get; } = new();

    internal FakeProblemSink Problems { get; } = new();

    internal XsltInstructionRegistry Registry { get; }

    internal QueryStaticScope Scope { get; }

    /// <summary>Compiles one element in a position and a scope; a problem is reported to <see cref="Problems"/>.</summary>
    internal XsltExecutable? Compile(
        XPathNode element,
        XsltPosition position = XsltPosition.Declaration,
        QueryStaticScope? scope = null
    )
    {
        var where = scope ?? Scope;
        if (!XsltElementResolver.TryResolve(Registry, element, position, Module, out var instruction, out var found))
        {
            Problems.Add(in found);
            return null;
        }

        var context = new XsltCompileContext(
            element,
            where,
            child => Compile(child, XsltPosition.Instruction, where),
            Problems,
            Binding,
            (child, inner) => Compile(child, XsltPosition.Instruction, inner)
        );
        if (instruction.TryCompile(context, out var executable, out var problem))
        {
            return executable;
        }

        if (problem.Code != default)
        {
            Problems.Add(in problem);
        }

        return null;
    }

    /// <summary>Compiles one top-level declaration under a schema root; <see langword="null"/> when a problem was reported.</summary>
    internal XsltGlobalDeclaration? CompileOne(XmlElem declaration) =>
        Compile(Declaration(declaration)) as XsltGlobalDeclaration;

    /// <summary>Compiles the declarations as top-level elements, in order, and adds each to a table at the precedence; a failure is reported.</summary>
    internal XsltGlobalTable Table(int precedence, params XmlElem[] declarations)
    {
        var builder = new XsltGlobalTableBuilder();
        foreach (var node in Declarations(declarations))
        {
            if (
                Compile(node) is XsltGlobalDeclaration declaration
                && !builder.TryAdd(declaration, precedence, out var problem)
            )
            {
                Problems.Add(in problem);
            }
        }

        return builder.Build();
    }

    /// <summary>Selects the phase and plans the scopes of a schema over the globals of a table.</summary>
    internal bool TryPlan(SchematronSchema schema, XsltGlobalTable table, out ScopePlan plan, string phase = "#ALL")
    {
        if (!PhaseSelector.TryResolve(schema, phase, Problems, out var selection))
        {
            throw new InvalidOperationException("The phase of the test schema does not resolve.");
        }

        return ScopePlanner.TryPlan(schema, selection, table.Declarations, Binding, Problems, out plan);
    }

    /// <summary>
    /// Compiles the global variables and parameters, plans the schema, compiles the functions in the scope of the plan, and starts a
    /// run with the real slot binder. A planning problem fails the test: the tests of the plan call <see cref="TryPlan"/> themselves.
    /// </summary>
    internal GlobalHarness Start(SchematronSchema schema, params XmlElem[] declarations)
    {
        var globals = new List<XmlElem>();
        var functions = new List<XmlElem>();
        foreach (var declaration in declarations)
        {
            (string.Equals(declaration.Local, "function", StringComparison.Ordinal) ? functions : globals).Add(
                declaration
            );
        }

        var table = Table(0, [.. globals]);
        if (!TryPlan(schema, table, out var plan))
        {
            throw new InvalidOperationException("The scopes do not plan: " + Codes(Problems));
        }

        var run = new MiniRun();
        var binder = new SlotBinder(plan, run, new QueryFocus(PatternFixture.Node("doc"), 1, 1), Problems);
        var names = new List<string>(plan.GlobalScope.VariableNames);
        while (names.Count < plan.SlotCount)
        {
            names.Add(string.Empty);
        }

        var functionScope = new QueryStaticScope([], names, Module, SchematronEdition.Iso2016);
        var functionTable = new XsltFunctionTableBuilder();
        foreach (var node in Declarations([.. functions]))
        {
            if (Compile(node, XsltPosition.Declaration, functionScope) is XsltFunctionDeclaration declaration)
            {
                _ = functionTable.TryAdd(declaration.Component, 0, out _);
            }
        }

        run.Table = functionTable.Build();
        run.Caller = new XsltFunctionCaller(
            run,
            binder,
            new SpanInterningNameTable(XmlReaderLimits.Default.MaxDistinctNames),
            CancellationToken.None
        );
        return new GlobalHarness(this, plan, run, binder, functionScope, table);
    }

    internal static string Codes(FakeProblemSink problems)
    {
        var codes = new List<string>();
        foreach (var problem in problems.Problems)
        {
            codes.Add(problem.Code + (problem.Constraint is null ? string.Empty : "/" + problem.Constraint));
        }

        return string.Join(",", codes);
    }

    /// <summary>A schema without lets, parameters, or patterns.</summary>
    internal static SchematronSchema EmptySchema() => ScopeModel.Schema();

    /// <summary>The state of one run over a plan: the run, the binder, and the names of the plan.</summary>
    internal sealed class GlobalHarness
    {
        private readonly GlobalFixture _fixture;
        private readonly QueryStaticScope _functionScope;

        internal GlobalHarness(
            GlobalFixture fixture,
            ScopePlan plan,
            MiniRun run,
            SlotBinder binder,
            QueryStaticScope functionScope,
            XsltGlobalTable table
        )
        {
            _fixture = fixture;
            Plan = plan;
            Run = run;
            Binder = binder;
            _functionScope = functionScope;
            Table = table;
        }

        internal ScopePlan Plan { get; }

        internal MiniRun Run { get; }

        internal SlotBinder Binder { get; }

        internal XsltGlobalTable Table { get; }

        internal FakeProblemSink Problems => _fixture.Problems;

        /// <summary>Gets the slot of a name in the scope of the functions, or -1.</summary>
        internal int Slot(string name) => MiniBinding.Resolve(_functionScope, name);

        /// <summary>Binds the slots of the names and returns the status.</summary>
        internal QueryStatus Ensure(params string[] names)
        {
            var slots = new int[names.Length];
            for (var i = 0; i < names.Length; i++)
            {
                slots[i] = Slot(names[i]);
            }

            return Binder.EnsureBound(slots);
        }

        /// <summary>Gets the number of times the run evaluated the value of a name.</summary>
        internal int Binds(string name) => Run.Count("bind:" + Slot(name));

        /// <summary>Gets the value of a name as text, or <see langword="null"/> when it is not bound.</summary>
        internal string? Value(string name)
        {
            if (!Run.TryGetValue(Slot(name), out var value))
            {
                return null;
            }

            return MiniQuery.Text(in value);
        }

        /// <summary>Evaluates a boolean expression in the scope of the functions, with the document as focus.</summary>
        internal bool Test(string text)
        {
            if (
                !_fixture.Binding.TryCompile(
                    text.AsSpan(),
                    QueryExpressionKind.Value,
                    _functionScope,
                    out var expression,
                    out var problem
                )
            )
            {
                throw new InvalidOperationException(problem.Message);
            }

            var focus = new QueryFocus(PatternFixture.Node("doc"), 1, 1);
            return Run.TryEvaluateBoolean(expression, in focus, out var value) == QueryStatus.Ok
                ? value
                : throw new InvalidOperationException(Run.GetProblem().Message);
        }
    }
}
