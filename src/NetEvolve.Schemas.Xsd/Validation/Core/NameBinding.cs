namespace NetEvolve.Schemas.Xsd.Validation.Core;

using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Api;

/// <summary>
/// Binds the names of one run to the dense schema name ids of the model (design #146 DP-6). The key is the pair of the namespace
/// symbol and the local symbol, never the prefix, so two prefixes of one namespace bind alike; the lookup in the model happens
/// once per distinct name and run.
/// </summary>
internal sealed class NameBinding
{
    private readonly ValidationModel _model;
    private readonly XmlNameTable _names;
    private readonly Dictionary<long, int> _ids = [];

    internal NameBinding(ValidationModel model, XmlNameTable names)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(names);

        _model = model;
        _names = names;
    }

    /// <summary>Resolves a name.</summary>
    /// <param name="name">The name handle.</param>
    /// <param name="qname">The expanded name.</param>
    /// <returns>The schema name id, <c>-1</c> when no content model mentions the name.</returns>
    internal int Bind(XmlName name, out XPathQName qname)
    {
        qname = new XPathQName(_names.GetString(name.NamespaceUri), _names.GetString(name.Local));
        var key = ((long)name.NamespaceUri.Value << 32) | (uint)name.Local.Value;
        if (!_ids.TryGetValue(key, out var id))
        {
            id = _model.TryGetNameId(qname, out var found) ? found : -1;
            _ids.Add(key, id);
        }

        return id;
    }
}
