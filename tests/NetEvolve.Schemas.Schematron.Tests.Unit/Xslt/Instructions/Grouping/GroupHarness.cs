namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Grouping;

using System.Threading;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using NetEvolve.Schemas.Schematron.Tests.Unit.Parsing.Core;
using NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Flow;
using NetEvolve.Schemas.Schematron.Xslt.Registry;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;
using FlowRegistration = NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow.Registration;
using FunctionsRegistration = NetEvolve.Schemas.Schematron.Xslt.Functions.Registration;
using GroupingRegistration = NetEvolve.Schemas.Schematron.Xslt.Instructions.Grouping.Registration;

/// <summary>
/// Compiles one instruction through a registry of the grouping, flow, and function folders, over the grouping binding, and runs it
/// over one document with a recording sink. The frame starts with the root element of the document as the focus. The variables are the
/// schema-level names of the compile scope, one slot each; <see cref="Bind"/> gives one a value.
/// </summary>
internal sealed class GroupHarness
{
    internal const string Module = "file:///stylesheets/grouping.xsl";

    private readonly string[] _variables;

    internal GroupHarness(
        XmlElem element,
        string document = "",
        string binding = "xslt2",
        CancellationToken cancellationToken = default,
        string[]? variables = null
    )
    {
        _variables = variables ?? [];
        Binding = new GroupBinding(binding);
        var builder = new XsltRegistryBuilder();
        GroupingRegistration.Register(builder);
        FlowRegistration.Register(builder);
        FunctionsRegistration.Register(builder);
        Registry = builder.Build();
        Scope = new QueryStaticScope([], _variables, Module, SchematronEdition.Iso2016);
        Document = new GroupDocument(document);
        Executable = Compile(FunctionTree.Declaration(element), Scope, XsltPosition.Instruction);
        Run = (GroupRun)Binding.BeginRun(null!, cancellationToken);
        Frame = new XsltFrame(Run, cancellationToken) { Focus = new QueryFocus(Document.Root, 1, 1), Messages = null };
    }

    internal GroupBinding Binding { get; }

    internal FakeProblemSink Problems { get; } = new();

    internal XsltInstructionRegistry Registry { get; }

    internal QueryStaticScope Scope { get; }

    internal GroupDocument Document { get; }

    internal XsltExecutable? Executable { get; }

    internal GroupRun Run { get; }

    internal XsltFrame Frame { get; }

    internal RecordingSink Sink { get; } = new();

    /// <summary>Gets the codes of the compile problems, as <c>NES3003/XTSE0010</c> separated by commas.</summary>
    internal string CompileCodes => FunctionFixture.Codes(Problems);

    /// <summary>Gets the text that the instruction wrote.</summary>
    internal string Output => Sink.Joined();

    /// <summary>Binds a value to the slot of a variable.</summary>
    internal void Bind(string name, XdmSequence value) =>
        Run.SetValue(System.Array.IndexOf(_variables, name), in value);

    /// <summary>Runs the instruction into <see cref="Sink"/>, or into another sink.</summary>
    internal QueryStatus Execute(ISequenceSink? sink = null) => Executable!.TryExecute(Frame, sink ?? Sink);

    /// <summary>Runs the instruction and reports the text or the problem as <c>problem:NES3301/XTTE1100</c>.</summary>
    internal string Result()
    {
        if (Execute() == QueryStatus.Ok)
        {
            return Output;
        }

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
