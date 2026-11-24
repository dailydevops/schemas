namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Regex;

using System;
using System.Threading;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using NetEvolve.Schemas.Schematron.Tests.Unit.Parsing.Core;
using NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Flow;
using NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Grouping;
using NetEvolve.Schemas.Schematron.Xslt.Registry;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;
using FlowRegistration = NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow.Registration;
using RegexRegistration = NetEvolve.Schemas.Schematron.Xslt.Instructions.Regex.Registration;

/// <summary>
/// Compiles one instruction through a registry of the regex and flow folders, over the regex binding, and runs it over an empty
/// document with a recording sink. The variables are the schema-level names of the compile scope, one slot each; <see cref="Bind"/>
/// gives one a value.
/// </summary>
internal sealed class RegexHarness
{
    internal const string Module = "file:///stylesheets/regex.xsl";

    private readonly string[] _variables;

    internal RegexHarness(XmlElem element, string[]? variables = null, CancellationToken cancellationToken = default)
    {
        _variables = variables ?? [];
        Binding = new RegexBinding();
        var builder = new XsltRegistryBuilder();
        RegexRegistration.Register(builder);
        FlowRegistration.Register(builder);
        Registry = builder.Build();
        Scope = new QueryStaticScope([], _variables, Module, SchematronEdition.Iso2016);
        Document = new GroupDocument(string.Empty);
        Executable = Compile(FunctionTree.Declaration(element), Scope, XsltPosition.Instruction);
        Run = (RegexRun)Binding.BeginRun(null!, cancellationToken);
        Frame = new XsltFrame(Run, cancellationToken) { Focus = new QueryFocus(Document.Root, 1, 1), Messages = null };
    }

    internal RegexBinding Binding { get; }

    internal FakeProblemSink Problems { get; } = new();

    internal XsltInstructionRegistry Registry { get; }

    internal QueryStaticScope Scope { get; }

    internal GroupDocument Document { get; }

    internal XsltExecutable? Executable { get; }

    internal RegexRun Run { get; }

    internal XsltFrame Frame { get; }

    internal RecordingSink Sink { get; } = new();

    /// <summary>Gets the codes of the compile problems, as <c>NES3003/XTSE0010</c> separated by commas.</summary>
    internal string CompileCodes => FunctionFixture.Codes(Problems);

    /// <summary>Binds a value to the slot of a variable.</summary>
    internal void Bind(string name, XdmSequence value) => Run.SetValue(Array.IndexOf(_variables, name), in value);

    /// <summary>Runs the instruction into <see cref="Sink"/>, or into another sink.</summary>
    internal QueryStatus Execute(ISequenceSink? sink = null) => Executable!.TryExecute(Frame, sink ?? Sink);

    /// <summary>Runs the instruction and reports the text or the problem as <c>problem:NES3301/XTDE1150</c>.</summary>
    internal string Result()
    {
        if (Execute() == QueryStatus.Ok)
        {
            return Sink.Joined();
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
