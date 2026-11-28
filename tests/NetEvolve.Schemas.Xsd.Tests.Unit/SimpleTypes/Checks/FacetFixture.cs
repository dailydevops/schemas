namespace NetEvolve.Schemas.Xsd.Tests.Unit.SimpleTypes.Checks;

using System;
using System.Collections.Generic;
using System.Globalization;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Datatypes.Binary;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.Xml.Datatypes.Duration;
using NetEvolve.Schemas.Xml.Datatypes.Facets.Core;
using NetEvolve.Schemas.Xml.Datatypes.Facets.Ordered;
using NetEvolve.Schemas.Xml.Datatypes.Numeric;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.SimpleTypes.Checks;
using NetEvolve.Schemas.Xsd.SimpleTypes.Definitions;
using NetEvolve.Schemas.Xsd.Validation.Core;

/// <summary>Builds base types over the real datatypes of the Xml package, runs the facet check, and reads the issues it reported.</summary>
internal sealed class FacetFixture
{
    internal const string XsNamespace = "http://www.w3.org/2001/XMLSchema";

    private readonly ValidationIssueCollector _collector = new(ValidationLimits.Default);

    internal FacetFixture(XsdVersion version = XsdVersion.V11)
    {
        Version = version;
        Reporter = new XsdIssueReporter(_collector);
        Assessor = new DescriptorValueAssessor(version);
    }

    internal static DocumentLocation Where { get; } = new("file:///f.xsd", 3, 5);

    internal XsdVersion Version { get; }

    internal XsdIssueReporter Reporter { get; }

    internal DescriptorValueAssessor Assessor { get; }

    internal IReadOnlyList<ValidationIssue> Issues => _collector.ToResult().Issues;

    internal static FacetDeclaration Decl(FacetKind kind, string value, int line = 7, bool isFixed = false) =>
        new(kind, value, new DocumentLocation("file:///f.xsd", line, 9), isFixed);

    internal static FacetKindSet Fixed(params FacetKind[] kinds)
    {
        var set = default(FacetKindSet);
        foreach (var kind in kinds)
        {
            set = set.With(kind);
        }

        return set;
    }

    internal static DiagnosticCode CodeOf(string ruleId)
    {
        foreach (var rule in XsdRuleCatalog.All)
        {
            if (string.Equals(rule.Id, ruleId, StringComparison.Ordinal))
            {
                return rule.Code;
            }
        }

        throw new InvalidOperationException("No rule " + ruleId);
    }

    internal IDatatypeDescriptor Descriptor(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        return BuiltInDatatypes.For(Version).Get(name);
    }

    /// <summary>The built-in type as the registry of components holds it.</summary>
    internal IXsdComponent BuiltIn(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        var types = XsdBuiltInTypes.For(Version);
        for (var i = 0; i < types.Count; i++)
        {
            if (string.Equals(types[i].Name.LocalName, name, StringComparison.Ordinal))
            {
                return types[i];
            }
        }

        throw new InvalidOperationException("No built-in type " + name);
    }

    /// <summary>The built-in type wrapped as a simple type definition, the way the type builders hold it.</summary>
    internal SimpleTypeDefinition Wrapped(string name) =>
        SimpleTypeDefinition.FromDatatype(new XPathQName(XsNamespace, name), Where, Descriptor(name));

    /// <summary>
    /// A base type: <c>list:int</c> is a list of int, <c>union:int+string</c> a union, any other name a restriction of that built-in type
    /// that adds the given facets.
    /// </summary>
    internal SimpleTypeDefinition Base(string type, params IFacet[] facets)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (type.StartsWith("list:", StringComparison.Ordinal))
        {
            return SimpleTypeDefinition.List(
                default,
                Where,
                XsdDerivationMethod.None,
                Wrapped(type.Substring(5)),
                facets,
                Reporter
            )!;
        }

        if (type.StartsWith("union:", StringComparison.Ordinal))
        {
            var names = type.Substring(6).Split('+');
            var members = new IXsdComponent[names.Length];
            for (var i = 0; i < names.Length; i++)
            {
                members[i] = Wrapped(names[i]);
            }

            return SimpleTypeDefinition.Union(default, Where, XsdDerivationMethod.None, members, facets, Reporter)!;
        }

        return SimpleTypeDefinition.Restrict(
            new XPathQName("urn:test", "base"),
            Where,
            XsdDerivationMethod.None,
            Wrapped(type),
            facets,
            Reporter
        )!;
    }

    /// <summary>A restriction of a simple type definition that adds the given facets.</summary>
    internal SimpleTypeDefinition Derive(SimpleTypeDefinition parent, params IFacet[] facets) =>
        SimpleTypeDefinition.Restrict(default, Where, XsdDerivationMethod.None, parent, facets, Reporter)!;

    /// <summary>A facet of the Xml package that a base type carries.</summary>
    internal IFacet Facet(string type, FacetKind kind, string lexical)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(lexical);

        switch (kind)
        {
            case FacetKind.Length:
            case FacetKind.MinLength:
            case FacetKind.MaxLength:
                return LengthOf(type, kind, int.Parse(lexical, CultureInfo.InvariantCulture));
            case FacetKind.TotalDigits:
                return new TotalDigitsFacet(int.Parse(lexical, CultureInfo.InvariantCulture));
            case FacetKind.FractionDigits:
                return new FractionDigitsFacet(int.Parse(lexical, CultureInfo.InvariantCulture));
            case FacetKind.WhiteSpace:
                return new WhiteSpaceFacet(Enum.Parse<WhitespaceMode>(lexical, true));
            default:
                return Bound(type, kind, lexical);
        }
    }

    internal FacetCheckResult Check(IXsdComponent baseType, params FacetDeclaration[] step) =>
        Check(baseType, default, step);

    internal FacetCheckResult Check(IXsdComponent baseType, FacetKindSet baseFixed, params FacetDeclaration[] step) =>
        SimpleTypeFacetCheck.Check(baseType, baseFixed, step, Where, Assessor, Version, Reporter);

    private static IFacet LengthOf(string type, FacetKind kind, int value)
    {
        var items = type.StartsWith("list:", StringComparison.Ordinal);
        if (type is "hexBinary" or "base64Binary")
        {
            var encoding = string.Equals(type, "hexBinary", StringComparison.Ordinal)
                ? BinaryKind.HexBinary
                : BinaryKind.Base64Binary;
            return kind switch
            {
                FacetKind.Length => BinaryLengthFacet.Length(value, encoding),
                FacetKind.MinLength => BinaryLengthFacet.MinLength(value, encoding),
                _ => BinaryLengthFacet.MaxLength(value, encoding),
            };
        }

        var unit = items ? LengthUnit.Items : LengthUnit.Characters;
        return kind switch
        {
            FacetKind.Length => LengthFacet.Length(value, unit),
            FacetKind.MinLength => LengthFacet.MinLength(value, unit),
            _ => LengthFacet.MaxLength(value, unit),
        };
    }

    private IFacet Bound(string type, FacetKind kind, string lexical)
    {
        var span = lexical.AsSpan();
        return Descriptor(type) switch
        {
            IDatatypeDescriptor<XsdDecimal> d => new BoundFacet<XsdDecimal>(kind, d, d.Parse(span).Value),
            IDatatypeDescriptor<XsdInteger> d => new BoundFacet<XsdInteger>(kind, d, d.Parse(span).Value),
            IDatatypeDescriptor<float> d => new BoundFacet<float>(kind, d, d.Parse(span).Value),
            IDatatypeDescriptor<double> d => new BoundFacet<double>(kind, d, d.Parse(span).Value),
            IDatatypeDescriptor<XsdDuration> d => new BoundFacet<XsdDuration>(kind, d, d.Parse(span).Value),
            IDatatypeDescriptor<XsdDateTime> d => new BoundFacet<XsdDateTime>(kind, d, d.Parse(span).Value),
            _ => throw new InvalidOperationException("No ordered facet for " + type),
        };
    }
}
