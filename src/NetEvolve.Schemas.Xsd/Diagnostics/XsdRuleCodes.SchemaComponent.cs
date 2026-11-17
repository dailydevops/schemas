#pragma warning disable NE0002 // Approved public namespace NetEvolve.Schemas.Xsd for the rule catalog (design #146 DP-1)

namespace NetEvolve.Schemas.Xsd;

/// <summary>The rule codes of the SchemaComponent family, NES4300 to NES4599, in specification order.</summary>
public static partial class XsdRuleCodes
{
    /// <summary>Attribute Declaration Properties Correct (<c>a-props-correct</c>), Structures 1.0, 3.2.6.</summary>
    public static readonly DiagnosticCode APropsCorrect = new(4300);

    /// <summary>xmlns Not Allowed (<c>no-xmlns</c>), Structures 1.0, 3.2.6.</summary>
    public static readonly DiagnosticCode NoXmlns = new(4301);

    /// <summary>xsi: Not Allowed (<c>no-xsi</c>), Structures 1.0, 3.2.6.</summary>
    public static readonly DiagnosticCode NoXsi = new(4302);

    /// <summary>Element Declaration Properties Correct (<c>e-props-correct</c>), Structures 1.0, 3.3.6.</summary>
    public static readonly DiagnosticCode EPropsCorrect = new(4303);

    /// <summary>Element Default Valid (Immediate) (<c>cos-valid-default</c>), Structures 1.0, 3.3.6.</summary>
    public static readonly DiagnosticCode CosValidDefault = new(4304);

    /// <summary>Substitution Group OK (Transitive) (<c>cos-equiv-derived-ok-rec</c>), Structures 1.0, 3.3.6.</summary>
    public static readonly DiagnosticCode CosEquivDerivedOkRec = new(4305);

    /// <summary>Substitution Group (<c>cos-equiv-class</c>), Structures 1.0, 3.3.6.</summary>
    public static readonly DiagnosticCode CosEquivClass = new(4306);

    /// <summary>Complex Type Definition Properties Correct (<c>ct-props-correct</c>), Structures 1.0, 3.4.6.</summary>
    public static readonly DiagnosticCode CtPropsCorrect = new(4307);

    /// <summary>Derivation Valid (Extension) (<c>cos-ct-extends</c>), Structures 1.0, 3.4.6.</summary>
    public static readonly DiagnosticCode CosCtExtends = new(4308);

    /// <summary>Derivation Valid (Restriction, Complex) (<c>derivation-ok-restriction</c>), Structures 1.0, 3.4.6.</summary>
    public static readonly DiagnosticCode DerivationOkRestriction = new(4309);

    /// <summary>Type Derivation OK (Complex) (<c>cos-ct-derived-ok</c>), Structures 1.0, 3.4.6.</summary>
    public static readonly DiagnosticCode CosCtDerivedOk = new(4310);

    /// <summary>Attribute Use Correct (<c>au-props-correct</c>), Structures 1.0, 3.5.6.</summary>
    public static readonly DiagnosticCode AuPropsCorrect = new(4311);

    /// <summary>Attribute Group Definition Properties Correct (<c>ag-props-correct</c>), Structures 1.0, 3.6.6.</summary>
    public static readonly DiagnosticCode AgPropsCorrect = new(4312);

    /// <summary>Model Group Definition Properties Correct (<c>mgd-props-correct</c>), Structures 1.0, 3.7.6.</summary>
    public static readonly DiagnosticCode MgdPropsCorrect = new(4313);

    /// <summary>Model Group Correct (<c>mg-props-correct</c>), Structures 1.0, 3.8.6.</summary>
    public static readonly DiagnosticCode MgPropsCorrect = new(4314);

    /// <summary>All Group Limited (<c>cos-all-limited</c>), Structures 1.0, 3.8.6.</summary>
    public static readonly DiagnosticCode CosAllLimited = new(4315);

    /// <summary>Element Declarations Consistent (<c>cos-element-consistent</c>), Structures 1.0, 3.8.6.</summary>
    public static readonly DiagnosticCode CosElementConsistent = new(4316);

    /// <summary>Unique Particle Attribution (<c>cos-nonambig</c>), Structures 1.0, 3.8.6.</summary>
    public static readonly DiagnosticCode CosNonambig = new(4317);

    /// <summary>Effective Total Range (all and sequence) (<c>cos-seq-range</c>), Structures 1.0, 3.8.6.</summary>
    public static readonly DiagnosticCode CosSeqRange = new(4318);

    /// <summary>Effective Total Range (choice) (<c>cos-choice-range</c>), Structures 1.0, 3.8.6.</summary>
    public static readonly DiagnosticCode CosChoiceRange = new(4319);

    /// <summary>Particle Correct (<c>p-props-correct</c>), Structures 1.0, 3.9.6.</summary>
    public static readonly DiagnosticCode PPropsCorrect = new(4320);

    /// <summary>Particle Valid (Extension) (<c>cos-particle-extend</c>), Structures 1.0, 3.9.6.</summary>
    public static readonly DiagnosticCode CosParticleExtend = new(4321);

    /// <summary>Particle Valid (Restriction) (<c>cos-particle-restrict</c>), Structures 1.0, 3.9.6.</summary>
    public static readonly DiagnosticCode CosParticleRestrict = new(4322);

    /// <summary>Occurrence Range OK (<c>range-ok</c>), Structures 1.0, 3.9.6.</summary>
    public static readonly DiagnosticCode RangeOk = new(4323);

    /// <summary>Particle Restriction OK (Elt:Elt -- NameAndTypeOK) (<c>rcase-NameAndTypeOK</c>), Structures 1.0, 3.9.6.</summary>
    public static readonly DiagnosticCode RcaseNameAndTypeOK = new(4324);

    /// <summary>Particle Derivation OK (Elt:Any -- NSCompat) (<c>rcase-NSCompat</c>), Structures 1.0, 3.9.6.</summary>
    public static readonly DiagnosticCode RcaseNSCompat = new(4325);

    /// <summary>Particle Derivation OK (Elt:All/Choice/Sequence -- RecurseAsIfGroup) (<c>rcase-RecurseAsIfGroup</c>), Structures 1.0, 3.9.6.</summary>
    public static readonly DiagnosticCode RcaseRecurseAsIfGroup = new(4326);

    /// <summary>Particle Derivation OK (Any:Any -- NSSubset) (<c>rcase-NSSubset</c>), Structures 1.0, 3.9.6.</summary>
    public static readonly DiagnosticCode RcaseNSSubset = new(4327);

    /// <summary>Particle Derivation OK (All/Choice/Sequence:Any -- NSRecurseCheckCardinality) (<c>rcase-NSRecurseCheckCardinality</c>), Structures 1.0, 3.9.6.</summary>
    public static readonly DiagnosticCode RcaseNSRecurseCheckCardinality = new(4328);

    /// <summary>Particle Derivation OK (All:All,Sequence:Sequence -- Recurse) (<c>rcase-Recurse</c>), Structures 1.0, 3.9.6.</summary>
    public static readonly DiagnosticCode RcaseRecurse = new(4329);

    /// <summary>Particle Derivation OK (Choice:Choice -- RecurseLax) (<c>rcase-RecurseLax</c>), Structures 1.0, 3.9.6.</summary>
    public static readonly DiagnosticCode RcaseRecurseLax = new(4330);

    /// <summary>Particle Derivation OK (Sequence:All -- RecurseUnordered) (<c>rcase-RecurseUnordered</c>), Structures 1.0, 3.9.6.</summary>
    public static readonly DiagnosticCode RcaseRecurseUnordered = new(4331);

    /// <summary>Particle Derivation OK (Sequence:Choice -- MapAndSum) (<c>rcase-MapAndSum</c>), Structures 1.0, 3.9.6.</summary>
    public static readonly DiagnosticCode RcaseMapAndSum = new(4332);

    /// <summary>Particle Emptiable (<c>cos-group-emptiable</c>), Structures 1.0, 3.9.6.</summary>
    public static readonly DiagnosticCode CosGroupEmptiable = new(4333);

    /// <summary>Wildcard Properties Correct (<c>w-props-correct</c>), Structures 1.0, 3.10.6.</summary>
    public static readonly DiagnosticCode WPropsCorrect = new(4334);

    /// <summary>Wildcard Subset (<c>cos-ns-subset</c>), Structures 1.0, 3.10.6.</summary>
    public static readonly DiagnosticCode CosNsSubset = new(4335);

    /// <summary>Attribute Wildcard Union (<c>cos-aw-union</c>), Structures 1.0, 3.10.6.</summary>
    public static readonly DiagnosticCode CosAwUnion = new(4336);

    /// <summary>Attribute Wildcard Intersection (<c>cos-aw-intersect</c>), Structures 1.0, 3.10.6.</summary>
    public static readonly DiagnosticCode CosAwIntersect = new(4337);

    /// <summary>Identity-constraint Definition Properties Correct (<c>c-props-correct</c>), Structures 1.0, 3.11.6.</summary>
    public static readonly DiagnosticCode CPropsCorrect = new(4338);

    /// <summary>Selector Value OK (<c>c-selector-xpath</c>), Structures 1.0, 3.11.6.</summary>
    public static readonly DiagnosticCode CSelectorXpath = new(4339);

    /// <summary>Fields Value OK (<c>c-fields-xpaths</c>), Structures 1.0, 3.11.6.</summary>
    public static readonly DiagnosticCode CFieldsXpaths = new(4340);

    /// <summary>Notation Declaration Correct (<c>n-props-correct</c>), Structures 1.0, 3.12.6.</summary>
    public static readonly DiagnosticCode NPropsCorrect = new(4341);

    /// <summary>Annotation Correct (<c>an-props-correct</c>), Structures 1.0, 3.13.6.</summary>
    public static readonly DiagnosticCode AnPropsCorrect = new(4342);

    /// <summary>Simple Type Definition Properties Correct (<c>st-props-correct</c>), Structures 1.0, 3.14.6.</summary>
    public static readonly DiagnosticCode StPropsCorrect = new(4343);

    /// <summary>Derivation Valid (Restriction, Simple) (<c>cos-st-restricts</c>), Structures 1.0, 3.14.6.</summary>
    public static readonly DiagnosticCode CosStRestricts = new(4344);

    /// <summary>Type Derivation OK (Simple) (<c>cos-st-derived-ok</c>), Structures 1.0, 3.14.6.</summary>
    public static readonly DiagnosticCode CosStDerivedOk = new(4345);

    /// <summary>Simple Type Restriction (Facets) (<c>st-restrict-facets</c>), Structures 1.0, 3.14.6.</summary>
    public static readonly DiagnosticCode StRestrictFacets = new(4346);

    /// <summary>Schema Properties Correct (<c>sch-props-correct</c>), Structures 1.0, 3.15.6.</summary>
    public static readonly DiagnosticCode SchPropsCorrect = new(4347);

    /// <summary>enumeration facet value required for NOTATION (<c>enumeration-required-notation</c>), Datatypes 1.0, 3.2.19.</summary>
    public static readonly DiagnosticCode EnumerationRequiredNotation = new(4348);

    /// <summary>applicable facets (<c>cos-applicable-facets</c>), Datatypes 1.0, 4.1.5.</summary>
    public static readonly DiagnosticCode CosApplicableFacets = new(4349);

    /// <summary>list of atomic (<c>cos-list-of-atomic</c>), Datatypes 1.0, 4.1.5.</summary>
    public static readonly DiagnosticCode CosListOfAtomic = new(4350);

    /// <summary>no circular unions (<c>cos-no-circular-unions</c>), Datatypes 1.0, 4.1.5.</summary>
    public static readonly DiagnosticCode CosNoCircularUnions = new(4351);

    /// <summary>length and minLength or maxLength (<c>length-minLength-maxLength</c>), Datatypes 1.0, 4.3.1.4.</summary>
    public static readonly DiagnosticCode LengthMinLengthMaxLength = new(4352);

    /// <summary>length valid restriction (<c>length-valid-restriction</c>), Datatypes 1.0, 4.3.1.4.</summary>
    public static readonly DiagnosticCode LengthValidRestriction = new(4353);

    /// <summary>minLength &lt;= maxLength (<c>minLength-less-than-equal-to-maxLength</c>), Datatypes 1.0, 4.3.2.4.</summary>
    public static readonly DiagnosticCode MinLengthLessThanEqualToMaxLength = new(4354);

    /// <summary>minLength valid restriction (<c>minLength-valid-restriction</c>), Datatypes 1.0, 4.3.2.4.</summary>
    public static readonly DiagnosticCode MinLengthValidRestriction = new(4355);

    /// <summary>maxLength valid restriction (<c>maxLength-valid-restriction</c>), Datatypes 1.0, 4.3.3.4.</summary>
    public static readonly DiagnosticCode MaxLengthValidRestriction = new(4356);

    /// <summary>enumeration valid restriction (<c>enumeration-valid-restriction</c>), Datatypes 1.0, 4.3.5.5.</summary>
    public static readonly DiagnosticCode EnumerationValidRestriction = new(4357);

    /// <summary>whiteSpace valid restriction (<c>whiteSpace-valid-restriction</c>), Datatypes 1.0, 4.3.6.4.</summary>
    public static readonly DiagnosticCode WhiteSpaceValidRestriction = new(4358);

    /// <summary>minInclusive &lt;= maxInclusive (<c>minInclusive-less-than-equal-to-maxInclusive</c>), Datatypes 1.0, 4.3.7.4.</summary>
    public static readonly DiagnosticCode MinInclusiveLessThanEqualToMaxInclusive = new(4359);

    /// <summary>maxInclusive valid restriction (<c>maxInclusive-valid-restriction</c>), Datatypes 1.0, 4.3.7.4.</summary>
    public static readonly DiagnosticCode MaxInclusiveValidRestriction = new(4360);

    /// <summary>maxInclusive and maxExclusive (<c>maxInclusive-maxExclusive</c>), Datatypes 1.0, 4.3.8.4.</summary>
    public static readonly DiagnosticCode MaxInclusiveMaxExclusive = new(4361);

    /// <summary>minExclusive &lt;= maxExclusive (<c>minExclusive-less-than-equal-to-maxExclusive</c>), Datatypes 1.0, 4.3.8.4.</summary>
    public static readonly DiagnosticCode MinExclusiveLessThanEqualToMaxExclusive = new(4362);

    /// <summary>maxExclusive valid restriction (<c>maxExclusive-valid-restriction</c>), Datatypes 1.0, 4.3.8.4.</summary>
    public static readonly DiagnosticCode MaxExclusiveValidRestriction = new(4363);

    /// <summary>minInclusive and minExclusive (<c>minInclusive-minExclusive</c>), Datatypes 1.0, 4.3.9.4.</summary>
    public static readonly DiagnosticCode MinInclusiveMinExclusive = new(4364);

    /// <summary>minExclusive &lt; maxInclusive (<c>minExclusive-less-than-maxInclusive</c>), Datatypes 1.0, 4.3.9.4.</summary>
    public static readonly DiagnosticCode MinExclusiveLessThanMaxInclusive = new(4365);

    /// <summary>minExclusive valid restriction (<c>minExclusive-valid-restriction</c>), Datatypes 1.0, 4.3.9.4.</summary>
    public static readonly DiagnosticCode MinExclusiveValidRestriction = new(4366);

    /// <summary>minInclusive &lt; maxExclusive (<c>minInclusive-less-than-maxExclusive</c>), Datatypes 1.0, 4.3.10.4.</summary>
    public static readonly DiagnosticCode MinInclusiveLessThanMaxExclusive = new(4367);

    /// <summary>minInclusive valid restriction (<c>minInclusive-valid-restriction</c>), Datatypes 1.0, 4.3.10.4.</summary>
    public static readonly DiagnosticCode MinInclusiveValidRestriction = new(4368);

    /// <summary>totalDigits valid restriction (<c>totalDigits-valid-restriction</c>), Datatypes 1.0, 4.3.11.4.</summary>
    public static readonly DiagnosticCode TotalDigitsValidRestriction = new(4369);

    /// <summary>fractionDigits less than or equal to totalDigits (<c>fractionDigits-totalDigits</c>), Datatypes 1.0, 4.3.12.4.</summary>
    public static readonly DiagnosticCode FractionDigitsTotalDigits = new(4370);

    /// <summary>fractionDigits valid restriction (<c>fractionDigits-valid-restriction</c>), Datatypes 1.0, 4.3.12.4.</summary>
    public static readonly DiagnosticCode FractionDigitsValidRestriction = new(4371);

    /// <summary>Simple Default Valid (<c>cos-valid-simple-default</c>), Structures 1.1, 3.2.6.2.</summary>
    public static readonly DiagnosticCode CosValidSimpleDefault = new(4372);

    /// <summary>Content type restricts (Complex Content) (<c>cos-content-act-restrict</c>), Structures 1.1, 3.4.6.4.</summary>
    public static readonly DiagnosticCode CosContentActRestrict = new(4373);

    /// <summary>Type Alternative Properties Correct (<c>ta-props-correct</c>), Structures 1.1, 3.12.6.</summary>
    public static readonly DiagnosticCode TaPropsCorrect = new(4374);

    /// <summary>Assertion Properties Correct (<c>as-props-correct</c>), Structures 1.1, 3.13.6.1.</summary>
    public static readonly DiagnosticCode AsPropsCorrect = new(4375);

    /// <summary>XPath Valid (<c>xpath-valid</c>), Structures 1.1, 3.13.6.2.</summary>
    public static readonly DiagnosticCode XpathValid = new(4376);

    /// <summary>Valid restriction of pattern (<c>cos-pattern-restriction</c>), Datatypes 1.1, 4.3.4.5.</summary>
    public static readonly DiagnosticCode CosPatternRestriction = new(4377);

    /// <summary>Valid restriction of assertions (<c>cos-assertions-restriction</c>), Datatypes 1.1, 4.3.13.4.</summary>
    public static readonly DiagnosticCode CosAssertionsRestriction = new(4378);

    /// <summary>timezone valid restriction (<c>timezone-valid-restriction</c>), Datatypes 1.1, 4.3.14.4.</summary>
    public static readonly DiagnosticCode TimezoneValidRestriction = new(4379);

    internal static XsdRule[] SchemaComponentRules { get; } =
    [
        new(
            APropsCorrect,
            "a-props-correct",
            "Attribute Declaration Properties Correct",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Structures 1.0, 3.2.6"
        ),
        new(
            NoXmlns,
            "no-xmlns",
            "xmlns Not Allowed",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Structures 1.0, 3.2.6"
        ),
        new(
            NoXsi,
            "no-xsi",
            "xsi: Not Allowed",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Structures 1.0, 3.2.6"
        ),
        new(
            EPropsCorrect,
            "e-props-correct",
            "Element Declaration Properties Correct",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Structures 1.0, 3.3.6"
        ),
        new(
            CosValidDefault,
            "cos-valid-default",
            "Element Default Valid (Immediate)",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Structures 1.0, 3.3.6"
        ),
        new(
            CosEquivDerivedOkRec,
            "cos-equiv-derived-ok-rec",
            "Substitution Group OK (Transitive)",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Structures 1.0, 3.3.6"
        ),
        new(
            CosEquivClass,
            "cos-equiv-class",
            "Substitution Group",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.V10,
            "Structures 1.0, 3.3.6"
        ),
        new(
            CtPropsCorrect,
            "ct-props-correct",
            "Complex Type Definition Properties Correct",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Structures 1.0, 3.4.6"
        ),
        new(
            CosCtExtends,
            "cos-ct-extends",
            "Derivation Valid (Extension)",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Structures 1.0, 3.4.6"
        ),
        new(
            DerivationOkRestriction,
            "derivation-ok-restriction",
            "Derivation Valid (Restriction, Complex)",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Structures 1.0, 3.4.6"
        ),
        new(
            CosCtDerivedOk,
            "cos-ct-derived-ok",
            "Type Derivation OK (Complex)",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Structures 1.0, 3.4.6"
        ),
        new(
            AuPropsCorrect,
            "au-props-correct",
            "Attribute Use Correct",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Structures 1.0, 3.5.6"
        ),
        new(
            AgPropsCorrect,
            "ag-props-correct",
            "Attribute Group Definition Properties Correct",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Structures 1.0, 3.6.6"
        ),
        new(
            MgdPropsCorrect,
            "mgd-props-correct",
            "Model Group Definition Properties Correct",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Structures 1.0, 3.7.6"
        ),
        new(
            MgPropsCorrect,
            "mg-props-correct",
            "Model Group Correct",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Structures 1.0, 3.8.6"
        ),
        new(
            CosAllLimited,
            "cos-all-limited",
            "All Group Limited",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Structures 1.0, 3.8.6"
        ),
        new(
            CosElementConsistent,
            "cos-element-consistent",
            "Element Declarations Consistent",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Structures 1.0, 3.8.6"
        ),
        new(
            CosNonambig,
            "cos-nonambig",
            "Unique Particle Attribution",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Structures 1.0, 3.8.6"
        ),
        new(
            CosSeqRange,
            "cos-seq-range",
            "Effective Total Range (all and sequence)",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Structures 1.0, 3.8.6"
        ),
        new(
            CosChoiceRange,
            "cos-choice-range",
            "Effective Total Range (choice)",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Structures 1.0, 3.8.6"
        ),
        new(
            PPropsCorrect,
            "p-props-correct",
            "Particle Correct",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Structures 1.0, 3.9.6"
        ),
        new(
            CosParticleExtend,
            "cos-particle-extend",
            "Particle Valid (Extension)",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Structures 1.0, 3.9.6"
        ),
        new(
            CosParticleRestrict,
            "cos-particle-restrict",
            "Particle Valid (Restriction)",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.V10,
            "Structures 1.0, 3.9.6"
        ),
        new(
            RangeOk,
            "range-ok",
            "Occurrence Range OK",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.V10,
            "Structures 1.0, 3.9.6"
        ),
        new(
            RcaseNameAndTypeOK,
            "rcase-NameAndTypeOK",
            "Particle Restriction OK (Elt:Elt -- NameAndTypeOK)",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.V10,
            "Structures 1.0, 3.9.6"
        ),
        new(
            RcaseNSCompat,
            "rcase-NSCompat",
            "Particle Derivation OK (Elt:Any -- NSCompat)",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.V10,
            "Structures 1.0, 3.9.6"
        ),
        new(
            RcaseRecurseAsIfGroup,
            "rcase-RecurseAsIfGroup",
            "Particle Derivation OK (Elt:All/Choice/Sequence -- RecurseAsIfGroup)",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.V10,
            "Structures 1.0, 3.9.6"
        ),
        new(
            RcaseNSSubset,
            "rcase-NSSubset",
            "Particle Derivation OK (Any:Any -- NSSubset)",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.V10,
            "Structures 1.0, 3.9.6"
        ),
        new(
            RcaseNSRecurseCheckCardinality,
            "rcase-NSRecurseCheckCardinality",
            "Particle Derivation OK (All/Choice/Sequence:Any -- NSRecurseCheckCardinality)",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.V10,
            "Structures 1.0, 3.9.6"
        ),
        new(
            RcaseRecurse,
            "rcase-Recurse",
            "Particle Derivation OK (All:All,Sequence:Sequence -- Recurse)",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.V10,
            "Structures 1.0, 3.9.6"
        ),
        new(
            RcaseRecurseLax,
            "rcase-RecurseLax",
            "Particle Derivation OK (Choice:Choice -- RecurseLax)",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.V10,
            "Structures 1.0, 3.9.6"
        ),
        new(
            RcaseRecurseUnordered,
            "rcase-RecurseUnordered",
            "Particle Derivation OK (Sequence:All -- RecurseUnordered)",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.V10,
            "Structures 1.0, 3.9.6"
        ),
        new(
            RcaseMapAndSum,
            "rcase-MapAndSum",
            "Particle Derivation OK (Sequence:Choice -- MapAndSum)",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.V10,
            "Structures 1.0, 3.9.6"
        ),
        new(
            CosGroupEmptiable,
            "cos-group-emptiable",
            "Particle Emptiable",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Structures 1.0, 3.9.6"
        ),
        new(
            WPropsCorrect,
            "w-props-correct",
            "Wildcard Properties Correct",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Structures 1.0, 3.10.6"
        ),
        new(
            CosNsSubset,
            "cos-ns-subset",
            "Wildcard Subset",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Structures 1.0, 3.10.6"
        ),
        new(
            CosAwUnion,
            "cos-aw-union",
            "Attribute Wildcard Union",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Structures 1.0, 3.10.6"
        ),
        new(
            CosAwIntersect,
            "cos-aw-intersect",
            "Attribute Wildcard Intersection",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Structures 1.0, 3.10.6"
        ),
        new(
            CPropsCorrect,
            "c-props-correct",
            "Identity-constraint Definition Properties Correct",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Structures 1.0, 3.11.6"
        ),
        new(
            CSelectorXpath,
            "c-selector-xpath",
            "Selector Value OK",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Structures 1.0, 3.11.6"
        ),
        new(
            CFieldsXpaths,
            "c-fields-xpaths",
            "Fields Value OK",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Structures 1.0, 3.11.6"
        ),
        new(
            NPropsCorrect,
            "n-props-correct",
            "Notation Declaration Correct",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Structures 1.0, 3.12.6"
        ),
        new(
            AnPropsCorrect,
            "an-props-correct",
            "Annotation Correct",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Structures 1.0, 3.13.6"
        ),
        new(
            StPropsCorrect,
            "st-props-correct",
            "Simple Type Definition Properties Correct",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Structures 1.0, 3.14.6"
        ),
        new(
            CosStRestricts,
            "cos-st-restricts",
            "Derivation Valid (Restriction, Simple)",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Structures 1.0, 3.14.6"
        ),
        new(
            CosStDerivedOk,
            "cos-st-derived-ok",
            "Type Derivation OK (Simple)",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Structures 1.0, 3.14.6"
        ),
        new(
            StRestrictFacets,
            "st-restrict-facets",
            "Simple Type Restriction (Facets)",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Structures 1.0, 3.14.6"
        ),
        new(
            SchPropsCorrect,
            "sch-props-correct",
            "Schema Properties Correct",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Structures 1.0, 3.15.6"
        ),
        new(
            EnumerationRequiredNotation,
            "enumeration-required-notation",
            "enumeration facet value required for NOTATION",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Datatypes 1.0, 3.2.19"
        ),
        new(
            CosApplicableFacets,
            "cos-applicable-facets",
            "applicable facets",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Datatypes 1.0, 4.1.5"
        ),
        new(
            CosListOfAtomic,
            "cos-list-of-atomic",
            "list of atomic",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.V10,
            "Datatypes 1.0, 4.1.5"
        ),
        new(
            CosNoCircularUnions,
            "cos-no-circular-unions",
            "no circular unions",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.V10,
            "Datatypes 1.0, 4.1.5"
        ),
        new(
            LengthMinLengthMaxLength,
            "length-minLength-maxLength",
            "length and minLength or maxLength",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Datatypes 1.0, 4.3.1.4"
        ),
        new(
            LengthValidRestriction,
            "length-valid-restriction",
            "length valid restriction",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Datatypes 1.0, 4.3.1.4"
        ),
        new(
            MinLengthLessThanEqualToMaxLength,
            "minLength-less-than-equal-to-maxLength",
            "minLength <= maxLength",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Datatypes 1.0, 4.3.2.4"
        ),
        new(
            MinLengthValidRestriction,
            "minLength-valid-restriction",
            "minLength valid restriction",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Datatypes 1.0, 4.3.2.4"
        ),
        new(
            MaxLengthValidRestriction,
            "maxLength-valid-restriction",
            "maxLength valid restriction",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Datatypes 1.0, 4.3.3.4"
        ),
        new(
            EnumerationValidRestriction,
            "enumeration-valid-restriction",
            "enumeration valid restriction",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Datatypes 1.0, 4.3.5.5"
        ),
        new(
            WhiteSpaceValidRestriction,
            "whiteSpace-valid-restriction",
            "whiteSpace valid restriction",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Datatypes 1.0, 4.3.6.4"
        ),
        new(
            MinInclusiveLessThanEqualToMaxInclusive,
            "minInclusive-less-than-equal-to-maxInclusive",
            "minInclusive <= maxInclusive",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Datatypes 1.0, 4.3.7.4"
        ),
        new(
            MaxInclusiveValidRestriction,
            "maxInclusive-valid-restriction",
            "maxInclusive valid restriction",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Datatypes 1.0, 4.3.7.4"
        ),
        new(
            MaxInclusiveMaxExclusive,
            "maxInclusive-maxExclusive",
            "maxInclusive and maxExclusive",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Datatypes 1.0, 4.3.8.4"
        ),
        new(
            MinExclusiveLessThanEqualToMaxExclusive,
            "minExclusive-less-than-equal-to-maxExclusive",
            "minExclusive <= maxExclusive",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Datatypes 1.0, 4.3.8.4"
        ),
        new(
            MaxExclusiveValidRestriction,
            "maxExclusive-valid-restriction",
            "maxExclusive valid restriction",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Datatypes 1.0, 4.3.8.4"
        ),
        new(
            MinInclusiveMinExclusive,
            "minInclusive-minExclusive",
            "minInclusive and minExclusive",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Datatypes 1.0, 4.3.9.4"
        ),
        new(
            MinExclusiveLessThanMaxInclusive,
            "minExclusive-less-than-maxInclusive",
            "minExclusive < maxInclusive",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Datatypes 1.0, 4.3.9.4"
        ),
        new(
            MinExclusiveValidRestriction,
            "minExclusive-valid-restriction",
            "minExclusive valid restriction",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Datatypes 1.0, 4.3.9.4"
        ),
        new(
            MinInclusiveLessThanMaxExclusive,
            "minInclusive-less-than-maxExclusive",
            "minInclusive < maxExclusive",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Datatypes 1.0, 4.3.10.4"
        ),
        new(
            MinInclusiveValidRestriction,
            "minInclusive-valid-restriction",
            "minInclusive valid restriction",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Datatypes 1.0, 4.3.10.4"
        ),
        new(
            TotalDigitsValidRestriction,
            "totalDigits-valid-restriction",
            "totalDigits valid restriction",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Datatypes 1.0, 4.3.11.4"
        ),
        new(
            FractionDigitsTotalDigits,
            "fractionDigits-totalDigits",
            "fractionDigits less than or equal to totalDigits",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Datatypes 1.0, 4.3.12.4"
        ),
        new(
            FractionDigitsValidRestriction,
            "fractionDigits-valid-restriction",
            "fractionDigits valid restriction",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.All,
            "Datatypes 1.0, 4.3.12.4"
        ),
        new(
            CosValidSimpleDefault,
            "cos-valid-simple-default",
            "Simple Default Valid",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.V11,
            "Structures 1.1, 3.2.6.2"
        ),
        new(
            CosContentActRestrict,
            "cos-content-act-restrict",
            "Content type restricts (Complex Content)",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.V11,
            "Structures 1.1, 3.4.6.4"
        ),
        new(
            TaPropsCorrect,
            "ta-props-correct",
            "Type Alternative Properties Correct",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.V11,
            "Structures 1.1, 3.12.6"
        ),
        new(
            AsPropsCorrect,
            "as-props-correct",
            "Assertion Properties Correct",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.V11,
            "Structures 1.1, 3.13.6.1"
        ),
        new(
            XpathValid,
            "xpath-valid",
            "XPath Valid",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.V11,
            "Structures 1.1, 3.13.6.2"
        ),
        new(
            CosPatternRestriction,
            "cos-pattern-restriction",
            "Valid restriction of pattern",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.V11,
            "Datatypes 1.1, 4.3.4.5"
        ),
        new(
            CosAssertionsRestriction,
            "cos-assertions-restriction",
            "Valid restriction of assertions",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.V11,
            "Datatypes 1.1, 4.3.13.4"
        ),
        new(
            TimezoneValidRestriction,
            "timezone-valid-restriction",
            "timezone valid restriction",
            XsdRuleKind.SchemaComponent,
            XsdVersionMask.V11,
            "Datatypes 1.1, 4.3.14.4"
        ),
    ];
}
