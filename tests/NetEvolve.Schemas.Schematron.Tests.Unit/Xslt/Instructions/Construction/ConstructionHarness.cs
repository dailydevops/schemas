namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Construction;

using System;
using System.Threading;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Construction;
using NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Construction;
using NetEvolve.Schemas.Schematron.Xslt.Registry;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;
using ConstructionRegistration = NetEvolve.Schemas.Schematron.Xslt.Instructions.Construction.Registration;
using FlowRegistration = NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow.Registration;
using FunctionsRegistration = NetEvolve.Schemas.Schematron.Xslt.Functions.Registration;

/// <summary>
/// Compiles one stylesheet fragment through a registry of the construction, flow, and function folders, over the mini binding, and
/// runs it into a sink of the test. The variables are the schema-level names of the compile scope, one slot each; <see cref="Bind"/>
/// gives one a value, <see cref="Focus(XPathNode)"/> sets the context item. The stylesheet root excludes every namespace from the result
/// unless the test gives other attributes.
/// </summary>
internal sealed class ConstructionHarness
{
    internal const string Module = "file:///stylesheets/construction.xsl";

    private readonly string[] _variables;

    internal ConstructionHarness(
        string fragment,
        string? rootAttributes = null,
        string[]? variables = null,
        CancellationToken cancellationToken = default
    )
    {
        _variables = variables ?? [];
        var builder = new XsltRegistryBuilder();
        ConstructionRegistration.Register(builder);
        FlowRegistration.Register(builder);
        FunctionsRegistration.Register(builder);
        Registry = builder.Build();
        Scope = new QueryStaticScope([], _variables, Module, SchematronEdition.Iso2016);
        Element = ConstructionSheet.Fragment(fragment, rootAttributes ?? "exclude-result-prefixes=\"#all\"");
        Executable = Compile(Element, Scope, XsltPosition.Instruction);
        Run = (MiniRun)Binding.BeginRun(null!, cancellationToken);
        Frame = new XsltFrame(Run, cancellationToken);
    }

    internal MiniBinding Binding { get; } = new();

    internal FakeProblemSink Problems { get; } = new();

    internal XsltInstructionRegistry Registry { get; }

    internal QueryStaticScope Scope { get; }

    internal XPathNode Element { get; }

    internal XsltExecutable? Executable { get; }

    internal MiniRun Run { get; }

    internal XsltFrame Frame { get; }

    /// <summary>Gets the codes of the compile problems, as <c>NES3003/XTSE0010</c> separated by commas.</summary>
    internal string CompileCodes => FunctionFixture.Codes(Problems);

    /// <summary>Binds a value to the slot of a variable.</summary>
    internal void Bind(string name, XdmSequence value) => Run.SetValue(Array.IndexOf(_variables, name), in value);

    /// <summary>Sets the context item, which is the node, with position and size one.</summary>
    internal void Focus(XPathNode node) => Frame.Focus = new QueryFocus(node, 1, 1);

    /// <summary>Sets an atomic item as the context item.</summary>
    internal void Focus(in XdmItem item) => Frame.Focus = QueryFocus.ForItem(in item, 1, 1);

    internal static TreeContentSink NewTree() => new(SinkTestSupport.NewTable());

    /// <summary>Runs the instruction into a sink.</summary>
    internal QueryStatus Execute(ISequenceSink sink) => Executable!.TryExecute(Frame, sink);

    /// <summary>Runs the instruction into tree content and returns the text of the document, or the problem as <c>problem:NES3301/XTDE0410</c>.</summary>
    internal string Tree()
    {
        using var sink = NewTree();
        return Execute(sink) == QueryStatus.Ok ? NodeDump.Of(sink.Document) : ProblemText();
    }

    /// <summary>Runs the instruction into an item sink and returns the items, each as the text of the node, or the problem.</summary>
    internal string Items()
    {
        var sink = new ItemSequenceSink(SinkTestSupport.NewTable());
        if (Execute(sink) != QueryStatus.Ok)
        {
            return ProblemText();
        }

        var result = sink.Result;
        var items = new string[result.Count];
        for (var i = 0; i < items.Length; i++)
        {
            var item = result[i];
            items[i] = item.IsNode ? NodeDump.Of(in item) : "atomic";
        }

        return string.Join("|", items);
    }

    /// <summary>Takes the problem of a failed run as <c>problem:NES3301/XTDE0410</c>.</summary>
    internal string ProblemText()
    {
        var problem = Frame.TakeProblem();
        return "problem:" + problem.Code + (problem.Constraint is null ? string.Empty : "/" + problem.Constraint);
    }

    private XsltExecutable? Compile(XPathNode element, QueryStaticScope scope, XsltPosition position)
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
}
