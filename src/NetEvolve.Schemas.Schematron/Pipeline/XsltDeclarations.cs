namespace NetEvolve.Schemas.Schematron.Pipeline;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Bindings.Registry;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Scopes;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Modules;
using NetEvolve.Schemas.Schematron.Xslt.Registry;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>
/// The compile step of the XSLT declarations of a schema (design #150 EP-14): it owns the scope, the binding, and the compile context
/// that <see cref="XsltModuleCompiler"/> leaves to the caller. The declarations compile in two passes because a function sees the
/// slots of the global variables, which the planner assigns from the globals that won the import precedence: the first pass compiles
/// everything except <c>xsl:function</c>, the planner runs over its globals, and the second pass compiles the functions in a scope
/// with the names of the plan.
/// </summary>
internal sealed class XsltDeclarations
{
    private readonly SchematronSchema _schema;
    private readonly IQueryBinding _binding;
    private readonly IXsltInstructionRegistry _registry;
    private readonly IReadOnlyList<string> _variableNames;

    private XsltDeclarations(SchematronSchema schema, IQueryBinding binding, IReadOnlyList<string> variableNames)
    {
        _schema = schema;
        _binding = binding;
        _registry = XsltRegistryCatalog.Shared.Default;
        _variableNames = variableNames;
    }

    /// <summary>
    /// Gives a binding the signature of every stylesheet function of the modules, before any body is compiled (design #150 EP-13): a
    /// call of a function then compiles in a rule, a variable, and another function body, and a call with an arity that no function
    /// declares stays the static error XPST0017. One signature-only host function stands for every declaration of a name and arity;
    /// the winner is resolved when the call runs. A declaration with an invalid name is left to the compile of the function, which
    /// reports it.
    /// </summary>
    /// <param name="binding">The binding of the schema.</param>
    /// <param name="schema">The effective schema, whose namespaces bind the prefix of a function name.</param>
    /// <param name="modules">The linked modules.</param>
    /// <returns>The derived binding, or <paramref name="binding"/> when it takes no host functions or the modules declare no function.</returns>
    internal static IQueryBinding WithSignatures(IQueryBinding binding, SchematronSchema schema, XsltModuleSet modules)
    {
        ArgumentNullException.ThrowIfNull(binding);
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(modules);

        if (binding is not IFunctionHostBinding host)
        {
            return binding;
        }

        var seen = new HashSet<(XPathQName Name, int Arity)>();
        var functions = new List<XPathHostFunction>();
        foreach (var declaration in modules.Declarations)
        {
            if (
                XsltElementReader.IsXslt(declaration.Element.Node, "function")
                && XsltFunctionInstruction.TryReadSignature(
                    declaration.Element,
                    schema.Namespaces,
                    out var name,
                    out var arity
                )
                && seen.Add((name, arity))
            )
            {
                functions.Add(new XsltUserFunction(name, arity));
            }
        }

        return functions.Count == 0 ? binding : host.WithFunctions(functions);
    }

    /// <summary>Compiles the declarations that are not functions: the globals, the keys, and the decimal formats.</summary>
    /// <param name="schema">The effective schema.</param>
    /// <param name="modules">The linked modules.</param>
    /// <param name="binding">The binding that compiles the expressions.</param>
    /// <param name="problems">The sink of the problems.</param>
    /// <param name="tables">The tables of the declarations that compiled; the function table is empty.</param>
    /// <returns><see langword="true"/> when every declaration compiled and no table reported a conflict.</returns>
    internal static bool TryCompileGlobals(
        SchematronSchema schema,
        XsltModuleSet modules,
        IQueryBinding binding,
        IProblemSink problems,
        out XsltModuleTables tables
    )
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(modules);
        ArgumentNullException.ThrowIfNull(binding);
        ArgumentNullException.ThrowIfNull(problems);

        var step = new XsltDeclarations(schema, binding, []);
        return XsltModuleCompiler.TryCompile(
            Select(modules, functions: false),
            step._registry,
            step.Compile,
            problems,
            out tables
        );
    }

    /// <summary>Compiles the functions in the scope of the plan and puts them into the tables of the first pass.</summary>
    /// <param name="schema">The effective schema.</param>
    /// <param name="modules">The linked modules.</param>
    /// <param name="binding">The binding that compiles the expressions.</param>
    /// <param name="plan">The plan over the globals of <paramref name="globals"/>.</param>
    /// <param name="globals">The tables of the first pass.</param>
    /// <param name="problems">The sink of the problems.</param>
    /// <param name="tables">The tables with the functions.</param>
    /// <returns><see langword="true"/> when every function compiled and no table reported a conflict.</returns>
    internal static bool TryCompileFunctions(
        SchematronSchema schema,
        XsltModuleSet modules,
        IQueryBinding binding,
        ScopePlan plan,
        XsltModuleTables globals,
        IProblemSink problems,
        out XsltModuleTables tables
    )
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(modules);
        ArgumentNullException.ThrowIfNull(binding);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(globals);
        ArgumentNullException.ThrowIfNull(problems);

        // The slot of a name is its index in the names of the scope: the names of the plan, and an empty entry for a slot that no name
        // of the global scope holds.
        var names = new List<string>(plan.GlobalScope.VariableNames);
        while (names.Count < plan.SlotCount)
        {
            names.Add(string.Empty);
        }

        var step = new XsltDeclarations(schema, binding, names);
        var ok = XsltModuleCompiler.TryCompile(
            Select(modules, functions: true),
            step._registry,
            step.Compile,
            problems,
            out var compiled
        );
        tables = new XsltModuleTables(compiled.Functions, globals.Globals, globals.Keys, globals.DecimalFormats);
        return ok;
    }

    private static XsltModuleSet Select(XsltModuleSet modules, bool functions)
    {
        var selected = new List<XsltModuleDeclaration>(modules.Declarations.Count);
        foreach (var declaration in modules.Declarations)
        {
            if (XsltElementReader.IsXslt(declaration.Element.Node, "function") == functions)
            {
                selected.Add(declaration);
            }
        }

        return new XsltModuleSet(selected);
    }

    private XsltExecutable? Compile(
        XsltModuleDeclaration declaration,
        IXsltInstruction instruction,
        IProblemSink problems
    ) => Compile(declaration.Element, instruction, Scope(declaration.Element), declaration.SystemId, problems);

    private XsltExecutable? Compile(
        XPathNode element,
        IXsltInstruction instruction,
        QueryStaticScope scope,
        string? systemId,
        IProblemSink problems
    )
    {
        XsltExecutable? Child(XPathNode child, QueryStaticScope inner)
        {
            if (
                !XsltElementResolver.TryResolve(
                    _registry,
                    child,
                    XsltPosition.Instruction,
                    systemId,
                    out var resolved,
                    out var found
                )
            )
            {
                problems.Add(in found);
                return null;
            }

            return Compile(child, resolved, inner, systemId, problems);
        }

        var context = new XsltCompileContext(element, scope, child => Child(child, scope), problems, _binding, Child);
        if (instruction.TryCompile(context, out var executable, out var problem))
        {
            return executable;
        }

        // A default problem means that the instruction reported its problems through the context.
        if (problem.Code != default)
        {
            problems.Add(in problem);
        }

        return null;
    }

    // The prefixes of the schema, overridden by the bindings in scope at the declaration, which a module declares for itself.
    private QueryStaticScope Scope(XPathNode element)
    {
        ArgumentNullException.ThrowIfNull(element.Tree);

        var names = element.Tree.Names;
        var namespaces = new List<SchematronNamespace>(_schema.Namespaces);
        for (var ns = element.Node.FirstNamespace; !ns.IsEmpty; ns = ns.NextNamespace)
        {
            var prefix = ns.Name.Local.IsEmpty ? string.Empty : names.GetString(ns.Name.Local);
            if (prefix.Length > 0 && !string.Equals(prefix, "xml", StringComparison.Ordinal))
            {
                namespaces.Add(new SchematronNamespace(_schema.Location, prefix, ns.StringValue));
            }
        }

        return new QueryStaticScope(namespaces, _variableNames, _schema.Location.Source, _schema.Edition);
    }
}
