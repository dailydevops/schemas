namespace NetEvolve.Schemas.Schematron.Bindings.Registry;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Bindings.Xslt1;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>
/// Resolves the <c>queryBinding</c> of a schema to a registered <see cref="IQueryBinding"/>. Names compare without regard to
/// case; an absent attribute resolves like <c>xslt</c> (design #137 AP-25). A later package adds a binding by passing one more
/// instance to the constructor; the resolver itself does not change.
/// </summary>
internal sealed class QueryBindingRegistry
{
    private const string DefaultName = "xslt";

    private readonly Dictionary<string, IQueryBinding> _bindings = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _supported;

    internal QueryBindingRegistry(IEnumerable<IQueryBinding> bindings)
    {
        ArgumentNullException.ThrowIfNull(bindings);

        var names = new List<string>();
        foreach (var binding in bindings)
        {
            if (_bindings.ContainsKey(binding.Name))
            {
                // A later registration of the same name replaces the earlier one.
                _ = names.Remove(binding.Name);
            }

            _bindings[binding.Name] = binding;
            names.Add(binding.Name);
        }

        names.Sort(StringComparer.Ordinal);
        _supported = string.Join(", ", names);
    }

    /// <summary>
    /// Gets the registry with the bindings of the XPath 2.0 contract and, for <c>xpath</c>, <c>xslt</c>, and <c>xslt1</c>, the bindings of
    /// <see cref="Registration"/> that turn XPath 1.0 compatibility mode on (SCH-16, #310). The bindings have the functions of XPath 2.0 only;
    /// a build uses <see cref="Create"/> to add the functions of XSLT.
    /// </summary>
    internal static QueryBindingRegistry Default { get; } = Create(null);

    /// <summary>Creates the registry of the six names with the host functions that <paramref name="extension"/> adds to every static context.</summary>
    /// <param name="extension">Adds the host functions, or <see langword="null"/> for none.</param>
    /// <returns>The registry.</returns>
    internal static QueryBindingRegistry Create(XPathQueryBinding.StaticContextExtension? extension) =>
        new([
            .. Registration.Create(extension),
            new XPathQueryBinding("xpath2", allowsXsltInstructions: false, extension: extension),
            new XPathQueryBinding("xslt2", allowsXsltInstructions: true, extension: extension),
            new XPathQueryBinding("xslt3", allowsXsltInstructions: true, extension: extension),
        ]);

    /// <summary>Resolves the binding named by <paramref name="schema"/>; on failure <paramref name="problem"/> is set.</summary>
    /// <remarks>
    /// The problem location is the start of the <c>schema</c> element: the Xml package delivers positions on start tags only,
    /// so the attribute has no position of its own.
    /// </remarks>
    internal bool TryResolve(SchematronSchema schema, out IQueryBinding binding, out SchematronProblem problem)
    {
        ArgumentNullException.ThrowIfNull(schema);

        var name = schema.QueryBinding ?? DefaultName;
        if (_bindings.TryGetValue(name, out binding!))
        {
            problem = default;
            return true;
        }

        problem = new SchematronProblem(
            SchematronDiagnosticCodes.BindingUnsupported,
            $"The queryBinding '{name}' is not supported. Supported bindings: {_supported}.",
            schema.Location
        );
        return false;
    }
}
