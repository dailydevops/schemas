namespace NetEvolve.Schemas.Schematron.Xslt.Modules;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Globals;
using NetEvolve.Schemas.Schematron.Xslt.Keys;
using NetEvolve.Schemas.Schematron.Xslt.Library.Formatting;
using NetEvolve.Schemas.Schematron.Xslt.Registry;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>
/// Compiles the declarations of a <see cref="XsltModuleSet"/> and merges them into the function, global, key, and decimal format tables with the
/// import precedence of their placement (XSLT 2.0 sections 3.10.2, 9.5, 10.3, and 16.3). The registry classifies every element as a
/// declaration, so an XSLT element that is not registered, such as <c>xsl:template</c>, is the unsupported-construct error located
/// in the module that holds it. A declaration that the registry knows but no table takes is reported the same way.
/// </summary>
/// <remarks>
/// A module that is placed twice is compiled once and added at each placement, so the two ranks decide and no duplicate is raised;
/// a key has no precedence and is added once.
/// Every declaration is compiled, also one that a declaration of a higher precedence shadows, because a static error in a module is
/// an error of the stylesheet. Two declarations of one function or one global at the same precedence are the errors of the tables
/// (XTSE0770 and XTSE0630); this step adds the modules of both declarations to the message and the module to the location, which
/// the tables cannot know. The attribute <see langword="override"/> of <c>xsl:function</c> is not read: the issue and design #150 EP-14 give
/// the declaration of the higher precedence the win in every case.
/// </remarks>
internal static class XsltModuleCompiler
{
    /// <summary>Compiles the declarations and merges them.</summary>
    /// <param name="set">The linked declarations.</param>
    /// <param name="registry">The registry that classifies the elements.</param>
    /// <param name="compile">The compile step of the schema.</param>
    /// <param name="problems">The sink of the problems.</param>
    /// <param name="tables">The tables of the declarations that compiled, also when the result is <see langword="false"/>.</param>
    /// <returns><see langword="true"/> when every declaration compiled and no table reported a conflict.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    internal static bool TryCompile(
        XsltModuleSet set,
        IXsltInstructionRegistry registry,
        XsltDeclarationCompiler compile,
        IProblemSink problems,
        out XsltModuleTables tables
    )
    {
        ArgumentNullException.ThrowIfNull(set);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(compile);
        ArgumentNullException.ThrowIfNull(problems);

        var functions = new XsltFunctionTableBuilder();
        var globals = new XsltGlobalTableBuilder();
        var keys = new List<XsltKeyComponent>();
        var decimals = new XsltDecimalFormatTableBuilder();
        var compiled = new Dictionary<XPathNode, XsltExecutable?>();
        var winners = new Dictionary<string, Winner>(StringComparer.Ordinal);
        var ok = true;
        foreach (var declaration in set.Declarations)
        {
            var first = !compiled.TryGetValue(declaration.Element, out var executable);
            if (first)
            {
                executable = Compile(declaration, registry, compile, problems);
                compiled.Add(declaration.Element, executable);
            }

            ok &=
                executable is not null
                && Merge(declaration, executable, first, functions, globals, keys, decimals, winners, problems);
        }

        ok &= decimals.TryBuild(problems, out var formats);
        tables = new XsltModuleTables(functions.Build(), globals.Build(), XsltKeyTable.Create(keys), formats);
        return ok;
    }

    private static XsltExecutable? Compile(
        XsltModuleDeclaration declaration,
        IXsltInstructionRegistry registry,
        XsltDeclarationCompiler compile,
        IProblemSink problems
    )
    {
        if (
            !XsltElementResolver.TryResolve(
                registry,
                declaration.Element,
                XsltPosition.Declaration,
                declaration.SystemId,
                out var instruction,
                out var found
            )
        )
        {
            problems.Add(in found);
            return null;
        }

        return compile(declaration, instruction, new XsltModuleProblemSink(problems, declaration.SystemId));
    }

    private static bool Merge(
        XsltModuleDeclaration declaration,
        XsltExecutable executable,
        bool first,
        XsltFunctionTableBuilder functions,
        XsltGlobalTableBuilder globals,
        List<XsltKeyComponent> keys,
        XsltDecimalFormatTableBuilder decimals,
        Dictionary<string, Winner> winners,
        IProblemSink problems
    )
    {
        SchematronProblem problem;
        string key;
        switch (executable)
        {
            case XsltFunctionDeclaration function:
                var name = function.Component.Name;
                key = "f|" + name.NamespaceName + "|" + name.LocalName + "|" + function.Component.Arity;
                if (functions.TryAdd(function.Component, declaration.Precedence, out problem))
                {
                    Track(winners, key, declaration);
                    return true;
                }

                break;
            case XsltGlobalDeclaration global:
                key = "g|" + global.Name.NamespaceName + "|" + global.Name.LocalName;
                if (globals.TryAdd(global, declaration.Precedence, out problem))
                {
                    Track(winners, key, declaration);
                    return true;
                }

                break;
            case XsltKeyDeclaration keyDeclaration:
                // A key has no precedence: a module placed twice adds it once, or the key list would hold identical duplicates.
                if (first)
                {
                    keys.Add(keyDeclaration.Component);
                }

                return true;
            case XsltDecimalFormatDeclaration format:
                // The formats of one name merge attribute by attribute with the precedence, so every placement is added.
                decimals.Add(format, declaration.Precedence);
                return true;
            default:
                problems.Add(
                    new SchematronProblem(
                        SchematronDiagnosticCodes.XsltUnsupported,
                        $"The XSLT declaration '{Describe(declaration)}' in {DescribeModule(declaration.SystemId)} is not supported.",
                        XsltElementResolver.LocationOf(declaration.Element, declaration.SystemId)
                    )
                );
                return false;
        }

        // A duplicate at one precedence: the table kept the declaration that is tracked, so the message names both modules.
        var other = winners[key];
        problems.Add(
            new SchematronProblem(
                problem.Code,
                $"{problem.Message} The declarations are in {DescribeModule(other.SystemId)} and {DescribeModule(declaration.SystemId)}.",
                new DocumentLocation(
                    problem.Location.Source ?? declaration.SystemId,
                    problem.Location.Line,
                    problem.Location.Column
                ),
                problem.Constraint
            )
        );
        return false;
    }

    private static void Track(Dictionary<string, Winner> winners, string key, XsltModuleDeclaration declaration)
    {
        if (!winners.TryGetValue(key, out var existing) || declaration.Precedence > existing.Precedence)
        {
            winners[key] = new Winner(declaration.Precedence, declaration.SystemId);
        }
    }

    private static string Describe(XsltModuleDeclaration declaration)
    {
        var element = declaration.Element;
        ArgumentNullException.ThrowIfNull(element.Tree);

        var node = element.Node;
        return "xsl:" + element.Tree.Names.GetString(node.Name.Local);
    }

    private static string DescribeModule(string? systemId) =>
        systemId is null ? "the principal module" : "module '" + systemId + "'";

    private readonly struct Winner
    {
        internal Winner(int precedence, string? systemId)
        {
            Precedence = precedence;
            SystemId = systemId;
        }

        internal int Precedence { get; }

        internal string? SystemId { get; }
    }
}
