namespace NetEvolve.Schemas.Xsd.SimpleTypes.Checks;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.Xml.Datatypes.Duration;
using NetEvolve.Schemas.Xml.Datatypes.Facets.Core;
using NetEvolve.Schemas.Xml.Datatypes.Facets.Ordered;
using NetEvolve.Schemas.Xml.Datatypes.Facets.Pattern;
using NetEvolve.Schemas.Xml.Datatypes.Numeric;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Validation.Core;

/// <summary>
/// Checks the facets of a restriction step against the base type (Datatypes section 4.3 and Structures section 3.14.6): the facets
/// apply to the type family, the values are representations of the facets, the step is a valid restriction of the facets of the base
/// type, the facets fit together, and enumeration and pattern values are valid in the base value space.
/// </summary>
/// <remarks>
/// <para>
/// The check reads the datatype of the Xml package and re-implements none of it: the order of the bounds, the facet relations, and
/// the applicability table are those of the Xml package, and this class maps their results to the rules of the catalog.
/// </para>
/// <para>
/// Limitations: <c>explicitTimezone</c> (XSD-25) and <c>assertions</c> (XSD-70) are not checked and are ignored; the facets of a base
/// type that is not built in are read from its facet list, so a facet that the list does not model is not compared; an enumeration
/// value is assessed through the assessor, which evaluates the own facets of a user-defined base;
/// a pattern is checked only for being a valid expression, because the specification defines no relation between the patterns of a
/// type and its base; a length or digits value beyond <see cref="int.MaxValue"/> is treated as <see cref="int.MaxValue"/>. Bounds that
/// are not ordered against each other, such as a dateTime without timezone inside the 14 hour window, are an error.
/// Each group of facets, the length group and the ordered group, reports its first violation only. For the built-in integer types, whose
/// bounds are not readable, an exclusive bound is checked together with its neighboring integer, which is exact for a discrete value space.
/// </para>
/// </remarks>
internal static class SimpleTypeFacetCheck
{
    /// <summary>Checks a restriction step and reports every violation as a located schema error.</summary>
    /// <param name="baseType">The resolved base type: a built-in type or a simple type definition.</param>
    /// <param name="baseFixed">The facets that the base type fixes, as an earlier call returned them.</param>
    /// <param name="step">The facets of the restriction in document order.</param>
    /// <param name="location">The location of the restriction, used when a violation names no facet.</param>
    /// <param name="assessor">The assessor that checks enumeration values against the base type.</param>
    /// <param name="version">The XSD version.</param>
    /// <param name="reporter">The reporter.</param>
    /// <returns>The outcome.</returns>
    internal static FacetCheckResult Check(
        IXsdComponent baseType,
        FacetKindSet baseFixed,
        IReadOnlyList<FacetDeclaration> step,
        DocumentLocation location,
        ISimpleValueAssessor assessor,
        XsdVersion version,
        XsdIssueReporter reporter
    )
    {
        ArgumentNullException.ThrowIfNull(baseType);
        ArgumentNullException.ThrowIfNull(step);
        ArgumentNullException.ThrowIfNull(assessor);
        ArgumentNullException.ThrowIfNull(reporter);

        var facts = FacetBase.Resolve(baseType, version);
        if (facts is null)
        {
            return new FacetCheckResult(true, StepFixed(baseFixed, step, null));
        }

        return new Run(baseType, facts, step, location, assessor, version, reporter).Execute(baseFixed);
    }

    // Records the fixed flags of the facets that the step states and that no check skipped.
    private static FacetKindSet StepFixed(FacetKindSet inherited, IReadOnlyList<FacetDeclaration> step, bool[]? skipped)
    {
        var fixedFacets = inherited;
        for (var i = 0; i < step.Count; i++)
        {
            var kind = step[i].Kind;
            if (
                step[i].IsFixed
                && (skipped is null || !skipped[i])
                && kind
                    is not (
                        FacetKind.Pattern
                        or FacetKind.Enumeration
                        or FacetKind.Assertions
                        or FacetKind.ExplicitTimezone
                    )
            )
            {
                fixedFacets = fixedFacets.With(kind);
            }
        }

        return fixedFacets;
    }

    private static string Name(FacetKind kind) =>
        kind switch
        {
            FacetKind.Length => "length",
            FacetKind.MinLength => "minLength",
            FacetKind.MaxLength => "maxLength",
            FacetKind.Pattern => "pattern",
            FacetKind.Enumeration => "enumeration",
            FacetKind.WhiteSpace => "whiteSpace",
            FacetKind.MaxInclusive => "maxInclusive",
            FacetKind.MaxExclusive => "maxExclusive",
            FacetKind.MinExclusive => "minExclusive",
            FacetKind.MinInclusive => "minInclusive",
            FacetKind.TotalDigits => "totalDigits",
            FacetKind.FractionDigits => "fractionDigits",
            FacetKind.Assertions => "assertions",
            FacetKind.ExplicitTimezone => "explicitTimezone",
            _ => "facet",
        };

    private static bool IsBound(FacetKind kind) =>
        kind is FacetKind.MinInclusive or FacetKind.MinExclusive or FacetKind.MaxInclusive or FacetKind.MaxExclusive;

    private static bool IsOrdered(FacetKind kind) =>
        IsBound(kind) || kind is FacetKind.TotalDigits or FacetKind.FractionDigits;

    private static bool IsCore(FacetKind kind) =>
        kind is FacetKind.Length or FacetKind.MinLength or FacetKind.MaxLength or FacetKind.WhiteSpace;

    // The state of one check: which declarations a rule already rejected, and the values that the declarations parsed to.
    private sealed class Run
    {
        private readonly IXsdComponent _baseType;
        private readonly FacetBase _facts;
        private readonly IReadOnlyList<FacetDeclaration> _step;
        private readonly DocumentLocation _location;
        private readonly ISimpleValueAssessor _assessor;
        private readonly XsdVersion _version;
        private readonly XsdIssueReporter _reporter;
        private readonly bool[] _skipped;
        private int? _length;
        private int? _minLength;
        private int? _maxLength;
        private WhitespaceMode? _whiteSpace;
        private int? _totalDigits;
        private int? _fractionDigits;

        internal Run(
            IXsdComponent baseType,
            FacetBase facts,
            IReadOnlyList<FacetDeclaration> step,
            DocumentLocation location,
            ISimpleValueAssessor assessor,
            XsdVersion version,
            XsdIssueReporter reporter
        )
        {
            _baseType = baseType;
            _facts = facts;
            _step = step;
            _location = location;
            _assessor = assessor;
            _version = version;
            _reporter = reporter;
            _skipped = new bool[step.Count];
        }

        private bool IsValid { get; set; } = true;

        internal FacetCheckResult Execute(FacetKindSet baseFixed)
        {
            var fixedFacets = _facts.SeedFixed(baseFixed);
            RejectRepeatedFacets();
            RejectInapplicableFacets();
            CheckPatterns();
            CheckEnumerations();
            ParseValues();
            CheckLengthAndWhiteSpace(fixedFacets);
            CheckBoundsAndDigits(fixedFacets);

            return new FacetCheckResult(IsValid, StepFixed(fixedFacets, _step, _skipped));
        }

        // Datatypes 4.1.3 (src-single-facet-value): every facet but pattern and enumeration is stated once.
        private void RejectRepeatedFacets()
        {
            for (var i = 0; i < _step.Count; i++)
            {
                var kind = _step[i].Kind;
                if (kind is FacetKind.Assertions or FacetKind.ExplicitTimezone)
                {
                    // Left to XSD-70 and XSD-25.
                    _skipped[i] = true;
                    continue;
                }

                if (kind is FacetKind.Pattern or FacetKind.Enumeration)
                {
                    continue;
                }

                for (var j = 0; j < i; j++)
                {
                    if (!_skipped[j] && _step[j].Kind == kind)
                    {
                        _skipped[i] = true;
                        Report(
                            XsdRuleCodes.SrcSingleFacetValue,
                            _step[i].Location,
                            "The facet '" + Name(kind) + "' is stated more than once."
                        );
                        break;
                    }
                }
            }
        }

        // Datatypes 4.1.5 (cos-applicable-facets).
        private void RejectInapplicableFacets()
        {
            for (var i = 0; i < _step.Count; i++)
            {
                if (
                    !_skipped[i]
                    && !FacetApplicability.IsApplicable(_step[i].Kind, _facts.Variety, _facts.Primitive, _version)
                )
                {
                    _skipped[i] = true;
                    Report(
                        XsdRuleCodes.CosApplicableFacets,
                        _step[i].Location,
                        "The facet '" + Name(_step[i].Kind) + "' is not applicable to the base type."
                    );
                }
            }
        }

        // Datatypes 4.3.4.3 (src-pattern-value): the value is a regular expression.
        private void CheckPatterns()
        {
            for (var i = 0; i < _step.Count; i++)
            {
                if (_skipped[i] || _step[i].Kind != FacetKind.Pattern)
                {
                    continue;
                }

                if (!PatternFacet.TryCreate([_step[i].Value], _version, null, out _, out var diagnostic))
                {
                    Report(
                        XsdRuleCodes.SrcPatternValue,
                        _step[i].Location,
                        "The pattern is not a valid regular expression (position "
                            + diagnostic.Position.ToString(CultureInfo.InvariantCulture)
                            + ")."
                    );
                }
            }
        }

        // Datatypes 4.3.5.5 (enumeration-valid-restriction) and 3.2.19 (enumeration-required-notation).
        private void CheckEnumerations()
        {
            var enumerates = _facts.HasEnumeration;
            for (var i = 0; i < _step.Count; i++)
            {
                if (_skipped[i] || _step[i].Kind != FacetKind.Enumeration)
                {
                    continue;
                }

                enumerates = true;
                if (!_assessor.Assess(_baseType, _step[i].Value.AsSpan(), out _).IsValid)
                {
                    Report(
                        XsdRuleCodes.EnumerationValidRestriction,
                        _step[i].Location,
                        "The enumeration value is not in the value space of the base type."
                    );
                }
            }

            if (!enumerates && _facts.Primitive == PrimitiveKind.Notation)
            {
                Report(
                    XsdRuleCodes.EnumerationRequiredNotation,
                    _location,
                    "A type derived from NOTATION must specify an enumeration."
                );
            }
        }

        // The integer valued facets and whiteSpace: the value must be a representation of the facet.
        private void ParseValues()
        {
            var registry = BuiltInDatatypes.For(_version);
            var nonNegative = (IDatatypeDescriptor<XsdInteger>)registry.Get("nonNegativeInteger");
            var positive = (IDatatypeDescriptor<XsdInteger>)registry.Get("positiveInteger");
            for (var i = 0; i < _step.Count; i++)
            {
                if (_skipped[i])
                {
                    continue;
                }

                var declaration = _step[i];
                var parsed = declaration.Kind switch
                {
                    FacetKind.Length => TakeInteger(nonNegative, declaration, ref _length),
                    FacetKind.MinLength => TakeInteger(nonNegative, declaration, ref _minLength),
                    FacetKind.MaxLength => TakeInteger(nonNegative, declaration, ref _maxLength),
                    FacetKind.FractionDigits => TakeInteger(nonNegative, declaration, ref _fractionDigits),
                    FacetKind.TotalDigits => TakeInteger(positive, declaration, ref _totalDigits),
                    FacetKind.WhiteSpace => TakeWhiteSpace(declaration),
                    _ => true,
                };
                if (!parsed)
                {
                    _skipped[i] = true;
                    Report(
                        XsdRuleCodes.StRestrictFacets,
                        declaration.Location,
                        "The value of the facet '" + Name(declaration.Kind) + "' is not valid for the facet."
                    );
                }
            }
        }

        private static bool TakeInteger(
            IDatatypeDescriptor<XsdInteger> type,
            FacetDeclaration declaration,
            ref int? target
        )
        {
            var parsed = type.Parse(declaration.Value.AsSpan());
            if (!parsed.Success)
            {
                return false;
            }

            // A value beyond the int range is as unbounded as the facets can tell.
            target = parsed.Value.Value > int.MaxValue ? int.MaxValue : (int)parsed.Value.Value;
            return true;
        }

        private bool TakeWhiteSpace(FacetDeclaration declaration)
        {
            switch (declaration.Value)
            {
                case "preserve":
                    _whiteSpace = WhitespaceMode.Preserve;
                    return true;
                case "replace":
                    _whiteSpace = WhitespaceMode.Replace;
                    return true;
                case "collapse":
                    _whiteSpace = WhitespaceMode.Collapse;
                    return true;
                default:
                    return false;
            }
        }

        private void CheckLengthAndWhiteSpace(FacetKindSet fixedFacets)
        {
            if (!Has(IsCore))
            {
                return;
            }

            // Datatypes 1.0, 4.3.1.4: length with minLength or maxLength is allowed only across derivation steps; XSD 1.1 drops the rule.
            if (_version == XsdVersion.V10 && _length.HasValue && (_minLength.HasValue || _maxLength.HasValue))
            {
                ReportResult(
                    XsdRuleCodes.LengthMinLengthMaxLength,
                    _minLength.HasValue ? FacetKind.MinLength : FacetKind.MaxLength,
                    IsCore
                );
                return;
            }

            var result = FacetRestriction.Validate(
                _facts.CoreSet(fixedFacets),
                new FacetSet(_length, _minLength, _maxLength, _whiteSpace),
                _facts.Variety,
                _facts.Primitive,
                _version
            );
            if (!result.IsValid)
            {
                ReportResult(FacetRuleCodes.Of(result), result.Facet, IsCore);
            }
        }

        private void CheckBoundsAndDigits(FacetKindSet fixedFacets)
        {
            if (!Has(IsOrdered))
            {
                return;
            }

            switch (_facts.Descriptor)
            {
                case IDatatypeDescriptor<XsdDecimal> type:
                    CheckOrdered(type, fixedFacets);
                    break;
                case IDatatypeDescriptor<XsdInteger> type:
                    CheckOrdered(type, fixedFacets);
                    break;
                case IDatatypeDescriptor<float> type:
                    CheckOrdered(type, fixedFacets);
                    break;
                case IDatatypeDescriptor<double> type:
                    CheckOrdered(type, fixedFacets);
                    break;
                case IDatatypeDescriptor<XsdDuration> type:
                    CheckOrdered(type, fixedFacets);
                    break;
                case IDatatypeDescriptor<XsdDateTime> type:
                    CheckOrdered(type, fixedFacets);
                    break;
            }
        }

        private void CheckOrdered<TValue>(IDatatypeDescriptor<TValue> type, FacetKindSet fixedFacets)
            where TValue : struct
        {
            var bounds = ParseBounds(type);
            var derived = new OrderedFacetSet<TValue>(
                bounds.MinInclusive,
                bounds.MinExclusive,
                bounds.MaxInclusive,
                bounds.MaxExclusive,
                _totalDigits,
                _fractionDigits
            );
            var result = OrderedFacetRestriction.Validate(
                type,
                _facts.Primitive,
                _facts.OrderedSet<TValue>(fixedFacets),
                derived
            );
            if (!result.IsValid)
            {
                var upper =
                    bounds.MaxInclusive.HasValue ? FacetKind.MaxInclusive
                    : bounds.MaxExclusive.HasValue ? FacetKind.MaxExclusive
                    : FacetKind.None;
                ReportResult(FacetRuleCodes.Of(result, upper), result.Facet, IsOrdered);
            }
        }

        // The bounds are values of the type, so they are parsed with its datatype: a lexical error is not a representation of the
        // facet, and a value outside the facets of the base type, such as 300 for the built-in byte type, is not a valid restriction of it.
        private Bounds<TValue> ParseBounds<TValue>(IDatatypeDescriptor<TValue> type)
            where TValue : struct
        {
            var bounds = new Bounds<TValue>();
            for (var i = 0; i < _step.Count; i++)
            {
                var kind = _step[i].Kind;
                if (_skipped[i] || !IsBound(kind))
                {
                    continue;
                }

                var parsed = type.Parse(_step[i].Value.AsSpan());
                if (parsed.Success && NeighborInValueSpace(type, parsed.Value, kind))
                {
                    bounds.Set(kind, parsed.Value);
                    continue;
                }

                _skipped[i] = true;
                if (parsed.Success || parsed.Error == DatatypeError.FacetViolation)
                {
                    Report(
                        FacetRuleCodes.ValidRestriction(kind),
                        _step[i].Location,
                        "The bound of the facet '" + Name(kind) + "' is outside the value space of the base type."
                    );
                }
                else
                {
                    Report(
                        XsdRuleCodes.StRestrictFacets,
                        _step[i].Location,
                        "The value of the facet '" + Name(kind) + "' is not a value of the base type."
                    );
                }
            }

            return bounds;
        }

        // The bounds of the built-in integer types cannot be read (see FacetBase), and the value space is discrete: an exclusive
        // bound lies inside the range of the base only when the value next to it, inside the bound, is in the range as well.
        private static bool NeighborInValueSpace<TValue>(IDatatypeDescriptor<TValue> type, TValue value, FacetKind kind)
            where TValue : struct
        {
            if (
                kind is not (FacetKind.MaxExclusive or FacetKind.MinExclusive)
                || type is not IDatatypeDescriptor<XsdInteger> integerType
                || value is not XsdInteger integer
            )
            {
                return true;
            }

            var neighbor =
                kind == FacetKind.MaxExclusive ? integer.Value - BigInteger.One : integer.Value + BigInteger.One;
            return integerType.Parse(neighbor.ToString(CultureInfo.InvariantCulture).AsSpan()).Success;
        }

        private bool Has(Func<FacetKind, bool> predicate)
        {
            for (var i = 0; i < _step.Count; i++)
            {
                if (!_skipped[i] && predicate(_step[i].Kind))
                {
                    return true;
                }
            }

            return false;
        }

        // The violation sits on the facet element that the Xml package names; a violation without a facet sits on the restriction.
        private void ReportResult(DiagnosticCode code, FacetKind facet, Func<FacetKind, bool> group)
        {
            var at = LocationOf(facet, group);

            Report(
                code,
                at,
                "The facet '"
                    + Name(facet)
                    + "' is not a valid restriction of the base type: "
                    + XsdRuleLookup.Find(code).Title
                    + "."
            );
        }

        // A facet of the base type, such as the maxLength that a length of the step conflicts with, is not in the step: the violation
        // then sits on the first facet of the same group that the step states.
        private DocumentLocation LocationOf(FacetKind facet, Func<FacetKind, bool> group)
        {
            var fallback = _location;
            var foundFallback = false;
            for (var i = 0; i < _step.Count; i++)
            {
                if (_skipped[i])
                {
                    continue;
                }

                if (_step[i].Kind == facet)
                {
                    return _step[i].Location;
                }

                if (!foundFallback && group(_step[i].Kind))
                {
                    fallback = _step[i].Location;
                    foundFallback = true;
                }
            }

            return fallback;
        }

        private void Report(DiagnosticCode code, DocumentLocation at, string message)
        {
            IsValid = false;
            _reporter.Report(code, at, message);
        }
    }

    private sealed class Bounds<TValue>
        where TValue : struct
    {
        internal TValue? MinInclusive { get; private set; }

        internal TValue? MinExclusive { get; private set; }

        internal TValue? MaxInclusive { get; private set; }

        internal TValue? MaxExclusive { get; private set; }

        internal void Set(FacetKind kind, TValue value)
        {
            switch (kind)
            {
                case FacetKind.MinInclusive:
                    MinInclusive = value;
                    break;
                case FacetKind.MinExclusive:
                    MinExclusive = value;
                    break;
                case FacetKind.MaxInclusive:
                    MaxInclusive = value;
                    break;
                default:
                    MaxExclusive = value;
                    break;
            }
        }
    }
}
