namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Library.Formatting;

using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using NetEvolve.Schemas.Schematron.Tests.Unit.Parsing.Core;
using NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Library.Formatting;
using NetEvolve.Schemas.Schematron.Xslt.Registry;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions.FunctionTree;
using FormattingRegistration = NetEvolve.Schemas.Schematron.Xslt.Library.Formatting.Registration;

/// <summary>
/// The compile driver of the decimal format tests: it resolves every element through the registry that the registration of the
/// formatting folder fills, compiles it as a declaration, and collects the declarations in a table builder.
/// </summary>
internal sealed class FormatFixture
{
    internal const string Module = "file:///stylesheets/formats.xsl";

    internal FormatFixture()
    {
        var builder = new XsltRegistryBuilder();
        FormattingRegistration.Register(builder);
        Registry = builder.Build();
        Scope = new QueryStaticScope([], [], Module, SchematronEdition.Iso2016);
    }

    internal FakeProblemSink Problems { get; } = new();

    internal XsltInstructionRegistry Registry { get; }

    internal QueryStaticScope Scope { get; }

    /// <summary>Compiles one element in a position; a problem is reported to <see cref="Problems"/>.</summary>
    internal XsltExecutable? Compile(XPathNode element, XsltPosition position = XsltPosition.Declaration)
    {
        if (!XsltElementResolver.TryResolve(Registry, element, position, Module, out var instruction, out var found))
        {
            Problems.Add(in found);
            return null;
        }

        var context = new XsltCompileContext(element, Scope, _ => null, Problems);
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
    internal IReadOnlyList<XsltDecimalFormatDeclaration> CompileAll(params XmlElem[] declarations)
    {
        var compiled = new List<XsltDecimalFormatDeclaration>();
        foreach (var node in Declarations(declarations))
        {
            if (Compile(node) is XsltDecimalFormatDeclaration declaration)
            {
                compiled.Add(declaration);
            }
        }

        return compiled;
    }

    internal XsltDecimalFormatDeclaration? CompileOne(XmlElem declaration)
    {
        var all = CompileAll(declaration);
        return all.Count == 0 ? null : all[0];
    }

    /// <summary>Compiles the declarations, all at one import precedence, and builds the table; the problems are in <see cref="Problems"/>.</summary>
    internal XsltDecimalFormatTable Build(params XmlElem[] declarations)
    {
        var builder = new XsltDecimalFormatTableBuilder();
        foreach (var declaration in CompileAll(declarations))
        {
            builder.Add(declaration, 1);
        }

        _ = builder.TryBuild(Problems, out var table);
        return table;
    }

    /// <summary>An <c>xsl:decimal-format</c>; a <see langword="null"/> name makes the unnamed format. The attributes are name and value pairs.</summary>
    internal static XmlElem Format(string? name, int line, params string[] attributes)
    {
        var content = new List<object>();
        if (name is not null)
        {
            content.Add(A("name", name));
        }

        for (var i = 0; i < attributes.Length; i += 2)
        {
            content.Add(A(attributes[i], attributes[i + 1]));
        }

        return X("decimal-format", line, [.. content]);
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
}
