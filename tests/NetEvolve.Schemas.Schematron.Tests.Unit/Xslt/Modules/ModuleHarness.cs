namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Modules;

using System;
using System.Collections.Generic;
using System.Threading;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Schematron.Loading.Inclusion;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using NetEvolve.Schemas.Schematron.Tests.Unit.Loading.Inclusion;
using NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Construction;
using NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Keys;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Modules;
using NetEvolve.Schemas.Schematron.Xslt.Registry;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Text.NameTable;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using FlowRegistration = NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow.Registration;
using FunctionsRegistration = NetEvolve.Schemas.Schematron.Xslt.Functions.Registration;
using KeysRegistration = NetEvolve.Schemas.Schematron.Xslt.Keys.Registration;

/// <summary>
/// The driver of the module tests: stylesheets are written as text, the modules are served by a fake core resolver through the
/// real module loader, and the linked declarations are compiled through the registry of the function, flow, and key folders (the
/// declarations of a global variable are read by the function folder). Every top-level element sits on a line of its own, so the
/// line of a problem tells the declarations apart.
/// </summary>
internal sealed class ModuleHarness
{
    internal const string Principal = "file:///m/main.xsl";
    internal const string Xslt = "http://www.w3.org/1999/XSL/Transform";

    private readonly IResourceResolver _resolver;
    private readonly ValidationLimits _limits;
    private readonly XsltInstructionRegistry _registry;
    private readonly KeyBinding _keyBinding = new();

    internal ModuleHarness(
        IResourceResolver? resolver = null,
        ValidationLimits? limits = null,
        Action<XsltRegistryBuilder>? extra = null
    )
    {
        Files = new FakeResources();
        _resolver = resolver ?? Files;
        _limits = limits ?? ValidationLimits.Default;
        var builder = new XsltRegistryBuilder();
        FunctionsRegistration.Register(builder);
        FlowRegistration.Register(builder);
        KeysRegistration.Register(builder);
        extra?.Invoke(builder);
        _registry = builder.Build();
    }

    internal FakeResources Files { get; }

    internal FakeProblemSink Problems { get; } = new();

    internal MiniBinding Binding { get; } = new();

    /// <summary>Gets the number of declarations that the compile callback was asked to compile.</summary>
    internal int Compiled { get; private set; }

    internal XsltModuleSet Set { get; private set; } = XsltModuleSet.Empty;

    internal XsltModuleTables Tables { get; private set; } = null!;

    /// <summary>A stylesheet module with one top-level element per line, starting on line 2.</summary>
    internal static string Sheet(params string[] parts) =>
        $"<xsl:stylesheet xmlns:xsl=\"{Xslt}\" xmlns:f=\"{FunctionTree.Functions}\" xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" version=\"2.0\">\n"
        + string.Join("\n", parts)
        + "\n</xsl:stylesheet>";

    internal static string Import(string href) => $"<xsl:import href=\"{href}\"/>";

    internal static string Include(string href) => $"<xsl:include href=\"{href}\"/>";

    /// <summary>A global variable; the linker does not read it, so it stands for any declaration.</summary>
    internal static string Decl(string name) => $"<xsl:variable name=\"{name}\" select=\"1\"/>";

    /// <summary>The function <c>f:local</c> with one integer parameter <c>a</c> and the body as its result.</summary>
    internal static string Fn(string local, string body, string attributes = "") =>
        $"<xsl:function name=\"f:{local}\" as=\"xs:integer\" {attributes}><xsl:param name=\"a\" as=\"xs:integer\"/><xsl:sequence select=\"{body}\"/></xsl:function>";

    /// <summary>The top-level elements of the root of a document, in order, as the Resolve stage hands them over.</summary>
    internal static IReadOnlyList<XPathNode> TopLevel(string xml)
    {
        var tree = ConstructionSheet.Parse(xml);
        var items = new List<XPathNode>();
        var root = tree.Document.FirstChild;
        while (root.Kind != XmlNodeKind.Element)
        {
            root = root.NextSibling;
        }

        for (var child = root.FirstChild; !child.IsEmpty; child = child.NextSibling)
        {
            if (child.Kind == XmlNodeKind.Element)
            {
                items.Add(new XPathNode(child));
            }
        }

        return items;
    }

    /// <summary>Links the principal module with the loader over the resolver of the harness.</summary>
    internal bool Link(string principal, string? systemId = Principal)
    {
        var loader = new ModuleLoader(_resolver, _limits);
        var linker = new XsltModuleLinker(loader, Problems, CancellationToken.None);
        var ok = linker.TryLink(TopLevel(principal), systemId, out var set);
        Set = set;
        return ok;
    }

    /// <summary>Compiles the linked declarations and merges them into the tables.</summary>
    internal bool Compile()
    {
        var ok = XsltModuleCompiler.TryCompile(Set, _registry, CompileDeclaration, Problems, out var tables);
        Tables = tables;
        return ok;
    }

    internal static string Describe(XsltModuleSet set)
    {
        var parts = new List<string>();
        foreach (var declaration in set.Declarations)
        {
            _ = XsltElementReader.TryGetAttribute(declaration.Element, "name", out var name);
            parts.Add(name + "@" + declaration.Precedence);
        }

        return string.Join(",", parts);
    }

    internal static string Codes(FakeProblemSink problems) => FunctionFixture.Codes(problems);

    /// <summary>Calls <c>f:name</c> with one integer argument through the table of the compiled functions.</summary>
    internal long Call(string name, long argument)
    {
        using var run = new MiniRun { Table = Tables.Functions };
        var caller = new XsltFunctionCaller(
            run,
            new NoSlotBinder(),
            new SpanInterningNameTable(XmlReaderLimits.Default.MaxDistinctNames),
            CancellationToken.None,
            XsltFunctionCaller.DefaultMaxDepth,
            null
        );
        run.Caller = caller;
        _ = Tables.Functions.TryGet(new XPathQName(FunctionTree.Functions, name), 1, out var function);
        var status = caller.TryCall(function, [FunctionFixture.Int(argument)], out var result);
        return status == QueryStatus.Ok
            ? MiniQuery.Integer(result)
            : throw new InvalidOperationException("call failed");
    }

    private XsltExecutable? CompileDeclaration(
        XsltModuleDeclaration declaration,
        IXsltInstruction instruction,
        IProblemSink problems
    )
    {
        Compiled++;
        var module = declaration.SystemId ?? Principal;
        var scope = new QueryStaticScope([], [], module, SchematronEdition.Iso2016);
        return CompileElement(declaration.Element, instruction, scope, module, problems);
    }

    private XsltExecutable? CompileElement(
        XPathNode element,
        IXsltInstruction instruction,
        QueryStaticScope scope,
        string module,
        IProblemSink problems
    )
    {
        IQueryBinding binding = XsltElementReader.IsXslt(element.Node, "key") ? _keyBinding : Binding;
        var context = new XsltCompileContext(
            element,
            scope,
            child => CompileChild(child, scope, module, problems),
            problems,
            binding,
            (child, inner) => CompileChild(child, inner, module, problems)
        );
        if (instruction.TryCompile(context, out var executable, out var problem))
        {
            return executable;
        }

        if (problem.Code != default)
        {
            problems.Add(in problem);
        }

        return null;
    }

    private XsltExecutable? CompileChild(XPathNode child, QueryStaticScope scope, string module, IProblemSink problems)
    {
        if (
            !XsltElementResolver.TryResolve(
                _registry,
                child,
                XsltPosition.Instruction,
                module,
                out var instruction,
                out var found
            )
        )
        {
            problems.Add(in found);
            return null;
        }

        return CompileElement(child, instruction, scope, module, problems);
    }

    private sealed class NoSlotBinder : ISlotBinder
    {
        public QueryStatus EnsureBound(ReadOnlySpan<int> slots) => QueryStatus.Ok;
    }
}
