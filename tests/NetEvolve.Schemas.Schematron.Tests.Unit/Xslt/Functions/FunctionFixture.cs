namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions;

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
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Registry;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Text.NameTable;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions.FunctionTree;

/// <summary>
/// The compile driver of the function tests: it resolves every element through the registry that <see cref="Registration"/>
/// fills, as the compiler of the package does, compiles through the mini binding, and keeps the table of the compiled functions.
/// </summary>
internal sealed class FunctionFixture
{
    internal const string Module = "file:///stylesheets/functions.xsl";

    private FunctionFixture(ScopePlan? plan, IReadOnlyList<string> globals)
    {
        var builder = new XsltRegistryBuilder();
        Registration.Register(builder);
        Registry = builder.Build();
        Plan = plan;
        var names = new List<string>(globals);
        var first = plan?.SlotCount ?? names.Count;
        while (names.Count < first)
        {
            names.Add(string.Empty);
        }

        FunctionScope = new QueryStaticScope([], names, Module, SchematronEdition.Iso2016);
    }

    internal MiniBinding Binding { get; } = new();

    internal FakeProblemSink Problems { get; } = new();

    internal XsltInstructionRegistry Registry { get; }

    internal ScopePlan? Plan { get; }

    /// <summary>The scope a function declaration is compiled in: the schema-level names, padded up to the first slot a function uses.</summary>
    internal QueryStaticScope FunctionScope { get; }

    internal XsltFunctionTable Table { get; private set; } = new XsltFunctionTableBuilder().Build();

    internal static FunctionFixture Create(params string[] globals) => new(null, globals);

    /// <summary>Plans the scopes of a schema with the mini binding, so that the function scope is the real one.</summary>
    internal static FunctionFixture ForSchema(SchematronSchema schema)
    {
        var problems = new FakeProblemSink();
        var binding = new MiniBinding();
        if (!PhaseSelector.TryResolve(schema, "#ALL", problems, out var selection))
        {
            throw new InvalidOperationException("The phase does not resolve.");
        }

        if (!ScopePlanner.TryPlan(schema, selection, binding, problems, out var plan))
        {
            throw new InvalidOperationException("The scopes do not plan.");
        }

        var names = new List<string>();
        foreach (var name in plan.GlobalScope.VariableNames)
        {
            names.Add(name);
        }

        return new FunctionFixture(plan, names);
    }

    /// <summary>Compiles one element in a position; a problem is reported to <see cref="Problems"/>.</summary>
    internal XsltExecutable? Compile(XPathNode element, QueryStaticScope scope, XsltPosition position)
    {
        if (!XsltElementResolver.TryResolve(Registry, element, position, Module, out var instruction, out var found))
        {
            Problems.Add(in found);
            return null;
        }

        var context = new XsltCompileContext(
            element,
            scope,
            child => Compile(child, scope, XsltPosition.Instruction),
            Problems,
            Binding,
            (child, inner) => Compile(child, inner, XsltPosition.Instruction)
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

    /// <summary>Compiles declarations as top-level elements and builds the table; a failed declaration is left out.</summary>
    internal IReadOnlyList<XsltFunctionComponent> CompileAll(params XmlElem[] declarations)
    {
        var components = new List<XsltFunctionComponent>();
        var table = new XsltFunctionTableBuilder();
        foreach (var node in Declarations(declarations))
        {
            if (Compile(node, FunctionScope, XsltPosition.Declaration) is XsltFunctionDeclaration declaration)
            {
                components.Add(declaration.Component);
                if (!table.TryAdd(declaration.Component, 0, out var problem))
                {
                    Problems.Add(in problem);
                }
            }
        }

        Table = table.Build();
        return components;
    }

    internal XsltFunctionComponent? CompileOne(XmlElem declaration)
    {
        var all = CompileAll(declaration);
        return all.Count == 0 ? null : all[0];
    }

    /// <summary>Creates a run and a caller wired to the table; the binder binds the schema-level slots when a plan exists.</summary>
    internal (MiniRun Run, XsltFunctionCaller Caller) CreateRun(int maxDepth = XsltFunctionCaller.DefaultMaxDepth) =>
        CreateCancellableRun(maxDepth, CancellationToken.None);

    internal (MiniRun Run, XsltFunctionCaller Caller) CreateCancellableRun(
        int maxDepth,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var run = new MiniRun { Table = Table };
        ISlotBinder binder = Plan is null
            ? new NoSlotBinder()
            : new SlotBinder(Plan, run, new QueryFocus(PatternFixture.Node("doc"), 1, 1), Problems);
        var caller = new XsltFunctionCaller(
            run,
            binder,
            new SpanInterningNameTable(XmlReaderLimits.Default.MaxDistinctNames),
            cancellationToken,
            maxDepth
        );
        run.Caller = caller;
        return (run, caller);
    }

    internal static XdmSequence Str(string value) => XdmSequence.Create(XdmItem.FromString(value));

    internal static XdmSequence Int(long value) => XdmSequence.Create(XdmItem.FromInteger(value));

    internal static string Codes(FakeProblemSink problems)
    {
        var codes = new List<string>();
        foreach (var problem in problems.Problems)
        {
            codes.Add(problem.Code + (problem.Constraint is null ? string.Empty : "/" + problem.Constraint));
        }

        return string.Join(",", codes);
    }

    private sealed class NoSlotBinder : ISlotBinder
    {
        public QueryStatus EnsureBound(ReadOnlySpan<int> slots) => QueryStatus.Ok;
    }
}
