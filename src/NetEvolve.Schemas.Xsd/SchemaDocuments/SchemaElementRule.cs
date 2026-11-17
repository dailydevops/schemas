namespace NetEvolve.Schemas.Xsd.SchemaDocuments;

using System;
using System.Collections.Generic;

/// <summary>What one schema element admits: its attributes and child elements, and the rule that its violations report.</summary>
internal sealed class SchemaElementRule
{
    internal SchemaElementRule(
        string name,
        DiagnosticCode code,
        string children,
        string attributes,
        bool opaque = false,
        bool repeatableAnnotation = false
    )
    {
        Name = name;
        Code = code;
        Children = children.Length == 0 ? [] : children.Split(' ');
        Opaque = opaque;
        RepeatableAnnotation = repeatableAnnotation;

        var list = new List<SchemaAttributeRule>();
        if (attributes.Length > 0)
        {
            foreach (var spec in attributes.Split(' '))
            {
                list.Add(SchemaAttributeRule.Parse(spec));
            }
        }

        Attributes = list.ToArray();
    }

    internal string Name { get; }

    /// <summary>Gets the rule that violations of this element report.</summary>
    internal DiagnosticCode Code { get; }

    internal string[] Children { get; }

    internal SchemaAttributeRule[] Attributes { get; }

    /// <summary>Gets a value indicating whether the content is skipped (<c>appinfo</c> and <c>documentation</c>).</summary>
    internal bool Opaque { get; }

    /// <summary>Gets a value indicating whether <c>annotation</c> may follow other children.</summary>
    internal bool RepeatableAnnotation { get; }

    internal bool Allows(string child) => Array.IndexOf(Children, child) >= 0;

    internal SchemaAttributeRule? FindAttribute(string name)
    {
        for (var i = 0; i < Attributes.Length; i++)
        {
            if (string.Equals(Attributes[i].Name, name, StringComparison.Ordinal))
            {
                return Attributes[i];
            }
        }

        return null;
    }
}
