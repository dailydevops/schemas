namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Declarations;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Declarations;
using NetEvolve.Schemas.Xml.Dtd.Model;

/// <summary>Parses an internal subset and dumps the declaration model as one canonical string.</summary>
internal static class DtdHarness
{
    internal static (string Model, XmlErrorCode? Error) Parse(
        string subset,
        string systemId = "",
        XmlReaderLimits? limits = null
    )
    {
        var info = new XmlDoctypeInfo("doc".AsSpan(), default, systemId.AsSpan(), subset.AsSpan());
        var error = DtdSubsetParser.Parse(in info, limits ?? XmlReaderLimits.Default, out var declarations);

        return (declarations is null ? "<none>" : Dump(declarations), error);
    }

    internal static string Dump(DtdDeclarations model)
    {
        var lines = new List<string>();
        foreach (var element in model.Elements.Values.OrderBy(e => e.Name, StringComparer.Ordinal))
        {
            lines.Add($"E:{element.Name}:{DumpContent(element)}");
        }

        foreach (var list in model.AttributeLists.OrderBy(l => l.Key, StringComparer.Ordinal))
        {
            foreach (var attribute in list.Value)
            {
                lines.Add($"A:{list.Key}/{attribute.Name}:{DumpType(attribute)}:{DumpDefault(attribute)}");
            }
        }

        foreach (var entity in model.GeneralEntities.Values.OrderBy(e => e.Name, StringComparer.Ordinal))
        {
            lines.Add($"GE:{DumpEntity(entity)}");
        }

        foreach (var entity in model.ParameterEntities.Values.OrderBy(e => e.Name, StringComparer.Ordinal))
        {
            lines.Add($"PE:{DumpEntity(entity)}");
        }

        foreach (var notation in model.Notations.Values.OrderBy(n => n.Name, StringComparer.Ordinal))
        {
            lines.Add($"N:{notation.Name} {notation.PublicId ?? "-"} {notation.SystemId ?? "-"}");
        }

        return string.Join("|", lines);
    }

    private static string DumpContent(DtdElementDeclaration element) =>
        element.ContentKind switch
        {
            DtdContentKind.Empty => "EMPTY",
            DtdContentKind.Any => "ANY",
            DtdContentKind.Mixed => DumpMixed(element.Content!),
            _ => DumpParticle(element.Content!),
        };

    private static string DumpMixed(DtdContentParticle particle)
    {
        var builder = new StringBuilder("(#PCDATA");
        foreach (var item in particle.Items)
        {
            builder.Append('|').Append(item.Name);
        }

        return builder.Append(')').Append(particle.Occurrence == DtdOccurrence.ZeroOrMore ? "*" : "").ToString();
    }

    private static string DumpParticle(DtdContentParticle particle)
    {
        var occurrence = particle.Occurrence switch
        {
            DtdOccurrence.Optional => "?",
            DtdOccurrence.ZeroOrMore => "*",
            DtdOccurrence.OneOrMore => "+",
            _ => "",
        };

        return particle.Kind == DtdParticleKind.Name
            ? particle.Name + occurrence
            : "("
                + string.Join(particle.Kind == DtdParticleKind.Sequence ? "," : "|", particle.Items.Select(DumpParticle))
                + ")"
                + occurrence;
    }

    private static string DumpType(DtdAttributeDefinition attribute) =>
        attribute.Type switch
        {
            DtdAttributeType.Enumeration => "(" + string.Join("|", attribute.Values) + ")",
            DtdAttributeType.Notation => "NOTATION(" + string.Join("|", attribute.Values) + ")",
            _ => attribute.Type.ToString().ToUpperInvariant(),
        };

    private static string DumpDefault(DtdAttributeDefinition attribute) =>
        attribute.DefaultKind switch
        {
            DtdDefaultKind.Required => "#REQUIRED",
            DtdDefaultKind.Implied => "#IMPLIED",
            DtdDefaultKind.Fixed => "#FIXED \"" + attribute.DefaultValue + "\"",
            _ => "\"" + attribute.DefaultValue + "\"",
        };

    private static string DumpEntity(DtdEntityDeclaration entity)
    {
        if (entity.SystemId is null)
        {
            return $"{entity.Name}=\"{entity.Value}\"";
        }

        return $"{entity.Name} {entity.PublicId ?? "-"} {entity.SystemId}"
            + (entity.NotationName is null ? "" : " NDATA " + entity.NotationName);
    }
}
