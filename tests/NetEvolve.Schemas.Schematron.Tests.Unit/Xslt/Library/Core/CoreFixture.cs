namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Library.Core;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Bindings.Registry;
using NetEvolve.Schemas.Schematron.Tests.Unit.Parsing.Core;
using NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Library.Core;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The harness of the core library tests: the scope of a binding over a static context that binds <c>xsl</c>, <c>f</c>, <c>fn</c>,
/// and <c>x</c> (a second prefix for the XSLT namespace), and one call of a function through the scope. A call is reported as text:
/// <c>notfound</c>, <c>error:CODE</c>, <c>empty</c>, <c>boolean:true</c>, <c>string:value</c>, or <c>node</c>.
/// </summary>
internal static class CoreFixture
{
    internal const string FunctionNamespace = "http://www.w3.org/2005/xpath-functions";

    internal static readonly XPathStaticContext Statics = XPathStaticContext
        .Default.ToBuilder()
        .WithNamespace("xsl", FunctionTree.Xslt)
        .WithNamespace("x", FunctionTree.Xslt)
        .WithNamespace("f", FunctionTree.Functions)
        .WithNamespace("fn", FunctionNamespace)
        .Build();

    internal static XsltFunctionScope Scope(string binding = "xslt2", XsltFunctionTable? user = null)
    {
        Argument.ThrowIfNull(binding);

        return new(new XPathQueryBinding(binding, binding.StartsWith("xslt", StringComparison.Ordinal)), Statics, user);
    }

    internal static XsltFunctionTable Table(params string[] names)
    {
        var fixture = FunctionFixture.Create();
        var declarations = new System.Collections.Generic.List<XmlElem>();
        for (var i = 0; i < names.Length; i++)
        {
            var parts = names[i].Split('/');
            var parameters = new string[int.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture)];
            for (var p = 0; p < parameters.Length; p++)
            {
                parameters[p] = "p" + p;
            }

            declarations.Add(Fx.Simple(parts[0], "xs:integer", parameters, "1", 10 + (i * 10)));
        }

        _ = fixture.CompileAll([.. declarations]);
        return fixture.Table;
    }

    internal static XPathNode[] Nodes(int count)
    {
        var elements = new XmlElem[count];
        for (var i = 0; i < count; i++)
        {
            elements[i] = FunctionTree.Other("n" + i, "urn:test:nodes", i + 2);
        }

        return [.. FunctionTree.Declarations(elements)];
    }

    internal static string Run(XsltFunctionScope scope, string local, params XdmItem[] arguments)
    {
        using var context = new XPathEvaluationContext();
        return RunIn(scope, context, local, arguments);
    }

    internal static string RunIn(
        XsltFunctionScope scope,
        XPathEvaluationContext context,
        string local,
        params XdmItem[] arguments
    )
    {
        if (!scope.TryResolve(Fn(local), arguments.Length, out var function))
        {
            return "notfound";
        }

        var sequences = new XdmSequence[arguments.Length];
        for (var i = 0; i < sequences.Length; i++)
        {
            sequences[i] = XdmSequence.Create(arguments[i]);
        }

        var call = new XPathFunctionCall(context, sequences);
        return function!.Invoke(ref call) == XPathEvaluationStatus.Error
            ? "error:" + call.Diagnostic.Code
            : Render(call.Result);
    }

    internal static XPathQName Fn(string local) => new(FunctionNamespace, local);

    internal static XdmItem S(string value) => XdmItem.FromString(value);

    internal static XdmItem I(long value) => XdmItem.FromInteger(value);

    private static string Render(in XdmSequence result)
    {
        if (result.IsEmpty)
        {
            return "empty";
        }

        var item = result[0];
        if (item.TryGetBoolean(out var flag) && item.Kind == XdmItemKind.Boolean)
        {
            return "boolean:" + (flag ? "true" : "false");
        }

        if (item.TryGetString(out var text))
        {
            return "string:" + text;
        }

        return item.TryGetNode(out _) ? "node" : "other";
    }
}
