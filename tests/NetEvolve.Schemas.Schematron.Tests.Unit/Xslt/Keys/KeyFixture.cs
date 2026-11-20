namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Keys;

using System;
using System.Collections.Generic;
using System.Threading;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Patterns;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using NetEvolve.Schemas.Schematron.Tests.Unit.Parsing.Core;
using NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Keys;
using NetEvolve.Schemas.Schematron.Xslt.Registry;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Text.NameTable;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions.FunctionTree;
using FlowRegistration = NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow.Registration;
using KeysRegistration = NetEvolve.Schemas.Schematron.Xslt.Keys.Registration;

/// <summary>
/// The compile driver and the run of the key tests: it resolves every element through the registry that the registration of the
/// key folder fills (with the flow instructions, for the content of a key), compiles through the key binding, and builds the table
/// and the provider of a run over the shared pattern matcher.
/// </summary>
internal sealed class KeyFixture
{
    internal const string Module = "file:///stylesheets/keys.xsl";

    internal KeyFixture(params string[] variables)
    {
        var builder = new XsltRegistryBuilder();
        KeysRegistration.Register(builder);
        FlowRegistration.Register(builder);
        Registry = builder.Build();
        Scope = new QueryStaticScope([], variables, Module, SchematronEdition.Iso2016);
    }

    internal KeyBinding Binding { get; } = new();

    internal FakeProblemSink Problems { get; } = new();

    internal XsltInstructionRegistry Registry { get; }

    internal QueryStaticScope Scope { get; }

    internal RecordingBinder Binder { get; } = new();

    internal XmlNameTable Names { get; } = new SpanInterningNameTable(XmlReaderLimits.Default.MaxDistinctNames);

    /// <summary>Compiles one element as a declaration; a problem is reported to <see cref="Problems"/>.</summary>
    internal XsltExecutable? Compile(XPathNode element, XsltPosition position = XsltPosition.Declaration)
    {
        if (!XsltElementResolver.TryResolve(Registry, element, position, Module, out var instruction, out var found))
        {
            Problems.Add(in found);
            return null;
        }

        var context = new XsltCompileContext(
            element,
            Scope,
            child => Compile(child, XsltPosition.Instruction),
            Problems,
            Binding,
            (child, inner) => Compile(child, XsltPosition.Instruction)
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

    /// <summary>Compiles the declarations as top-level elements, in order; a failed declaration is left out.</summary>
    internal IReadOnlyList<XsltKeyComponent> CompileAll(params XmlElem[] declarations)
    {
        var components = new List<XsltKeyComponent>();
        foreach (var node in Declarations(declarations))
        {
            if (Compile(node) is XsltKeyDeclaration declaration)
            {
                components.Add(declaration.Component);
            }
        }

        return components;
    }

    internal XsltKeyComponent? CompileOne(XmlElem declaration)
    {
        var all = CompileAll(declaration);
        return all.Count == 0 ? null : all[0];
    }

    /// <summary>Compiles the declarations and creates the run and the provider over the table of them.</summary>
    internal (KeyBinding.KeyRun Run, XsltKeyProvider Provider) CreateProvider(params XmlElem[] declarations) =>
        CreateCancellable(CancellationToken.None, declarations);

    /// <summary>Like <see cref="CreateProvider"/>, with a token that the index build checks.</summary>
    internal (KeyBinding.KeyRun Run, XsltKeyProvider Provider) CreateCancellable(
        CancellationToken cancellationToken,
        params XmlElem[] declarations
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var table = XsltKeyTable.Create(CompileAll(declarations));
        var run = (KeyBinding.KeyRun)Binding.BeginRun(null!, cancellationToken);
        var provider = new XsltKeyProvider(table, new PatternMatcher(Binding), run, Binder, Names, cancellationToken);
        return (run, provider);
    }

    internal static XPathQName Key(string name) => new(string.Empty, name);

    internal static XdmSequence Str(params string[] values)
    {
        var items = new XdmItem[values.Length];
        for (var i = 0; i < items.Length; i++)
        {
            items[i] = XdmItem.FromString(values[i]);
        }

        return XdmSequence.Create(items);
    }

    internal static IEnumerable<XPathNode> Nodes(XdmSequence sequence)
    {
        for (var i = 0; i < sequence.Count; i++)
        {
            _ = sequence[i].TryGetNode(out var node);
            yield return node;
        }
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

    /// <summary>Records the slots that a build asks to bind, and fails on request.</summary>
    internal sealed class RecordingBinder : ISlotBinder
    {
        internal List<int[]> Calls { get; } = [];

        internal bool Fail { get; set; }

        public QueryStatus EnsureBound(ReadOnlySpan<int> slots)
        {
            Calls.Add(slots.ToArray());
            return Fail ? QueryStatus.Problem : QueryStatus.Ok;
        }
    }
}
