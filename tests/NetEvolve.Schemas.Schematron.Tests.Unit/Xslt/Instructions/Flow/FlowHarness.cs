namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Flow;

using System;
using System.Threading;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Parsing.Core;
using NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// Compiles one instruction through the registry and runs it over the test language with a recording sink. The variables of the
/// harness are the schema-level names of the compile scope, one slot each; <see cref="Bind"/> gives one a value.
/// </summary>
internal sealed class FlowHarness
{
    private readonly string[] _variables;

    internal FlowHarness(XmlElem element, params string[] variables)
        : this(element, CancellationToken.None, variables) { }

    internal FlowHarness(XmlElem element, CancellationToken cancellationToken, params string[] variables)
    {
        _variables = variables;
        Fixture = FunctionFixture.Create(variables);
        var node = FunctionTree.Declaration(element);
        Executable = Fixture.Compile(node, Fixture.FunctionScope, XsltPosition.Instruction);
        Run = (MiniRun)Fixture.Binding.BeginRun(null!, cancellationToken);
        Frame = new XsltFrame(Run, cancellationToken);
        Frame.Messages = Messages;
    }

    internal FunctionFixture Fixture { get; }

    internal XsltExecutable? Executable { get; }

    internal MiniRun Run { get; }

    internal XsltFrame Frame { get; }

    internal RecordingSink Sink { get; } = new();

    internal RecordingMessages Messages { get; } = new();

    /// <summary>Gets the codes of the compile problems, as <c>NES3003/XTSE0010</c> separated by commas.</summary>
    internal string CompileCodes => FunctionFixture.Codes(Fixture.Problems);

    /// <summary>Binds a value to the slot of a variable.</summary>
    internal void Bind(string name, XdmSequence value) => Run.SetValue(Array.IndexOf(_variables, name), in value);

    /// <summary>Runs the instruction into <see cref="Sink"/>, or into another sink.</summary>
    internal QueryStatus Execute(ISequenceSink? sink = null) => Executable!.TryExecute(Frame, sink ?? Sink);
}
