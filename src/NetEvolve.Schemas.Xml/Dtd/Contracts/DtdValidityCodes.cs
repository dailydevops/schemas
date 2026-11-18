namespace NetEvolve.Schemas.Xml.Dtd.Contracts;

using System.Collections.Generic;

/// <summary>
/// The code table of DTD validity (design #122, sections 6 and 7): one code for each of the 26 validity constraints
/// of XML 1.0 (Fifth Edition) in specification order, and the reports that are not validity constraints. The limits
/// added for validation are in <see cref="DtdLimitCodes"/>.
/// </summary>
/// <remarks>
/// Limitation: XML 1.0 only (XML 1.1 is DEF-50); validity issues are reported and never fatal.
/// </remarks>
public static class DtdValidityCodes
{
    // Declared before the code fields: their initializers register into these lists.
    private static readonly List<DtdValidityCode> AllRows = [];
    private static readonly List<DtdValidityCode> ConstraintRows = [];
    private static readonly List<DtdValidityCode> ReportRows = [];

    /// <summary>Root Element Type, section 2.8: the root element type matches the name of the document type declaration.</summary>
    public static readonly DtdValidityCode XML5001 = Constraint(
        5001,
        "Root Element Type",
        "2.8",
        "XML-67",
        "The root element type does not match the name in the document type declaration."
    );

    /// <summary>Proper Declaration/PE Nesting, section 2.8.</summary>
    public static readonly DtdValidityCode XML5002 = Constraint(
        5002,
        "Proper Declaration/PE Nesting",
        "2.8",
        "XML-67",
        "A declaration does not start and end in the same parameter-entity replacement text."
    );

    /// <summary>Standalone Document Declaration, section 2.9; one code for all four clauses.</summary>
    public static readonly DtdValidityCode XML5003 = Constraint(
        5003,
        "Standalone Document Declaration",
        "2.9",
        "XML-67",
        "A standalone document depends on a declaration of the external subset or an external parameter entity."
    );

    /// <summary>Element Valid, section 3.</summary>
    public static readonly DtdValidityCode XML5004 = Constraint(
        5004,
        "Element Valid",
        "3",
        "XML-63",
        "The element does not match its element type declaration."
    );

    /// <summary>Attribute Value Type, section 3.1.</summary>
    public static readonly DtdValidityCode XML5005 = Constraint(
        5005,
        "Attribute Value Type",
        "3.1",
        "XML-64",
        "The attribute is not declared or its value does not match the declared type."
    );

    /// <summary>Unique Element Type Declaration, section 3.2.</summary>
    public static readonly DtdValidityCode XML5006 = Constraint(
        5006,
        "Unique Element Type Declaration",
        "3.2",
        "XML-67",
        "The element type is declared more than once."
    );

    /// <summary>Proper Group/PE Nesting, section 3.2.1.</summary>
    public static readonly DtdValidityCode XML5007 = Constraint(
        5007,
        "Proper Group/PE Nesting",
        "3.2.1",
        "XML-67",
        "A group does not start and end in the same parameter-entity replacement text."
    );

    /// <summary>No Duplicate Types, section 3.2.2.</summary>
    public static readonly DtdValidityCode XML5008 = Constraint(
        5008,
        "No Duplicate Types",
        "3.2.2",
        "XML-67",
        "An element type appears more than once in a mixed-content declaration."
    );

    /// <summary>ID, section 3.3.1.</summary>
    public static readonly DtdValidityCode XML5009 = Constraint(
        5009,
        "ID",
        "3.3.1",
        "XML-65",
        "The value of an ID attribute is not a name or appears more than once in the document."
    );

    /// <summary>One ID per Element Type, section 3.3.1.</summary>
    public static readonly DtdValidityCode XML5010 = Constraint(
        5010,
        "One ID per Element Type",
        "3.3.1",
        "XML-65",
        "An element type has more than one ID attribute."
    );

    /// <summary>ID Attribute Default, section 3.3.1.</summary>
    public static readonly DtdValidityCode XML5011 = Constraint(
        5011,
        "ID Attribute Default",
        "3.3.1",
        "XML-65",
        "An ID attribute has a default other than #IMPLIED or #REQUIRED."
    );

    /// <summary>IDREF, section 3.3.1.</summary>
    public static readonly DtdValidityCode XML5012 = Constraint(
        5012,
        "IDREF",
        "3.3.1",
        "XML-65",
        "The value of an IDREF or IDREFS attribute does not match the value of an ID attribute."
    );

    /// <summary>Entity Name, section 3.3.1.</summary>
    public static readonly DtdValidityCode XML5013 = Constraint(
        5013,
        "Entity Name",
        "3.3.1",
        "XML-66",
        "The value of an ENTITY or ENTITIES attribute does not name a declared unparsed entity."
    );

    /// <summary>Name Token, section 3.3.1.</summary>
    public static readonly DtdValidityCode XML5014 = Constraint(
        5014,
        "Name Token",
        "3.3.1",
        "XML-64",
        "The value of an NMTOKEN or NMTOKENS attribute is not a name token."
    );

    /// <summary>Notation Attributes, section 3.3.1.</summary>
    public static readonly DtdValidityCode XML5015 = Constraint(
        5015,
        "Notation Attributes",
        "3.3.1",
        "XML-66",
        "The value of a NOTATION attribute is not a declared notation of its list."
    );

    /// <summary>One Notation Per Element Type, section 3.3.1.</summary>
    public static readonly DtdValidityCode XML5016 = Constraint(
        5016,
        "One Notation Per Element Type",
        "3.3.1",
        "XML-66",
        "An element type has more than one NOTATION attribute."
    );

    /// <summary>No Notation on Empty Element, section 3.3.1.</summary>
    public static readonly DtdValidityCode XML5017 = Constraint(
        5017,
        "No Notation on Empty Element",
        "3.3.1",
        "XML-66",
        "An EMPTY element type has a NOTATION attribute."
    );

    /// <summary>No Duplicate Tokens, section 3.3.1.</summary>
    public static readonly DtdValidityCode XML5018 = Constraint(
        5018,
        "No Duplicate Tokens",
        "3.3.1",
        "XML-64",
        "A token appears more than once in the enumerated or notation attribute type."
    );

    /// <summary>Enumeration, section 3.3.1.</summary>
    public static readonly DtdValidityCode XML5019 = Constraint(
        5019,
        "Enumeration",
        "3.3.1",
        "XML-64",
        "The value of an enumerated attribute is not one of its declared values."
    );

    /// <summary>Required Attribute, section 3.3.2.</summary>
    public static readonly DtdValidityCode XML5020 = Constraint(
        5020,
        "Required Attribute",
        "3.3.2",
        "XML-64",
        "A required attribute is absent."
    );

    /// <summary>Attribute Default Value Syntactically Correct, section 3.3.2.</summary>
    public static readonly DtdValidityCode XML5021 = Constraint(
        5021,
        "Attribute Default Value Syntactically Correct",
        "3.3.2",
        "XML-64",
        "The declared default value does not match the lexical constraints of the declared type."
    );

    /// <summary>Fixed Attribute Default, section 3.3.2.</summary>
    public static readonly DtdValidityCode XML5022 = Constraint(
        5022,
        "Fixed Attribute Default",
        "3.3.2",
        "XML-64",
        "The value of a #FIXED attribute differs from its default."
    );

    /// <summary>Proper Conditional Section/PE Nesting, section 3.4.</summary>
    public static readonly DtdValidityCode XML5023 = Constraint(
        5023,
        "Proper Conditional Section/PE Nesting",
        "3.4",
        "XML-67",
        "A conditional section does not start and end in the same parameter-entity replacement text."
    );

    /// <summary>Entity Declared, section 4.1; the validity form that never stops the parse.</summary>
    public static readonly DtdValidityCode XML5024 = Constraint(
        5024,
        "Entity Declared",
        "4.1",
        "XML-67",
        "The referenced entity is not declared."
    );

    /// <summary>Notation Declared, section 4.2.2.</summary>
    public static readonly DtdValidityCode XML5025 = Constraint(
        5025,
        "Notation Declared",
        "4.2.2",
        "XML-66",
        "The notation named by an unparsed entity declaration or a notation attribute is not declared."
    );

    /// <summary>Unique Notation Name, section 4.7.</summary>
    public static readonly DtdValidityCode XML5026 = Constraint(
        5026,
        "Unique Notation Name",
        "4.7",
        "XML-66",
        "The notation is declared more than once."
    );

    /// <summary>A declaration mentions an element type that has no declaration; an optional warning.</summary>
    public static readonly DtdValidityCode XML5901 = Report(
        5901,
        "Undeclared Element Type Mentioned",
        "3.2",
        "XML-67",
        IssueSeverity.Warning,
        "A declaration mentions an element type that has no element type declaration."
    );

    /// <summary>Attributes are declared for an element type that has no declaration; an optional warning.</summary>
    public static readonly DtdValidityCode XML5902 = Report(
        5902,
        "Attributes Declared For Undeclared Element Type",
        "3.3",
        "XML-64",
        IssueSeverity.Warning,
        "Attributes are declared for an element type that has no element type declaration."
    );

    /// <summary>More than one attribute-list declaration or attribute definition for the same name; an optional warning.</summary>
    public static readonly DtdValidityCode XML5903 = Report(
        5903,
        "Attribute Declared More Than Once",
        "3.3",
        "XML-64",
        IssueSeverity.Warning,
        "More than one attribute definition exists for the same element type and attribute name; the first binds."
    );

    /// <summary>An entity is declared more than once, the first binds; an optional warning.</summary>
    public static readonly DtdValidityCode XML5904 = Report(
        5904,
        "Entity Declared More Than Once",
        "4.2",
        "XML-66",
        IssueSeverity.Warning,
        "The entity is declared more than once; the first declaration binds."
    );

    /// <summary>In validating mode, the document has no document type declaration; an error that is not a validity constraint.</summary>
    public static readonly DtdValidityCode XML5910 = Report(
        5910,
        "Missing Document Type Declaration",
        "2.8",
        "XML-67",
        IssueSeverity.Error,
        "A valid document has a document type declaration; this document has none."
    );

    /// <summary>A non-deterministic content model; a warning, an error with <see cref="DtdNonDeterminism.Reject"/>.</summary>
    public static readonly DtdValidityCode XML5911 = Report(
        5911,
        "Non-Deterministic Content Model",
        "3.2.1",
        "XML-63",
        IssueSeverity.Warning,
        "The content model is not deterministic: an element can match more than one occurrence of an element type."
    );

    /// <summary>Gets every code in code order: the validity constraints, then the other reports.</summary>
    public static IReadOnlyList<DtdValidityCode> All => AllRows;

    /// <summary>Gets the 26 validity constraints of XML 1.0 (Fifth Edition) in specification order.</summary>
    public static IReadOnlyList<DtdValidityCode> Constraints => ConstraintRows;

    /// <summary>Gets the reports that are not validity constraints.</summary>
    public static IReadOnlyList<DtdValidityCode> Reports => ReportRows;

    private static DtdValidityCode Constraint(int alias, string name, string section, string owner, string message) =>
        Define(
            alias,
            1500 + (alias - 5000),
            DtdValidityCodeKind.ValidityConstraint,
            name,
            section,
            owner,
            IssueSeverity.Error,
            message,
            ConstraintRows
        );

    private static DtdValidityCode Report(
        int alias,
        string name,
        string section,
        string owner,
        IssueSeverity severity,
        string message
    ) =>
        Define(
            alias,
            1600 + (alias - 5900),
            DtdValidityCodeKind.Report,
            name,
            section,
            owner,
            severity,
            message,
            ReportRows
        );

    private static DtdValidityCode Define(
        int alias,
        int number,
        DtdValidityCodeKind kind,
        string name,
        string section,
        string owner,
        IssueSeverity severity,
        string message,
        List<DtdValidityCode> kindRows
    )
    {
        var code = new DtdValidityCode(
            "XML" + alias.ToString("D4", System.Globalization.CultureInfo.InvariantCulture),
            number,
            kind,
            name,
            section,
            owner,
            severity,
            message
        );
        AllRows.Add(code);
        kindRows.Add(code);

        return code;
    }
}
