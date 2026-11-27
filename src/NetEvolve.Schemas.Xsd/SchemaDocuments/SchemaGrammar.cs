namespace NetEvolve.Schemas.Xsd.SchemaDocuments;

using System;
using System.Collections.Generic;

/// <summary>
/// The XML representation of the schema elements of XSD 1.0 and 1.1 (Structures section 3, summaries of the XML
/// representation), as data.
/// </summary>
/// <remarks>
/// Limitation: the order and the number of the children of one element are not checked, only whether a child may appear at
/// all, except that <c>annotation</c> comes first; conditional attribute rules (<c>name</c> or <see langword="ref"/>, <c>type</c> with
/// a type child, and so on) belong to the component builders. XSD 1.1 syntax is admitted in every mode; rejecting it in
/// strict 1.0 mode is XSD-59.
/// </remarks>
internal static class SchemaGrammar
{
    internal const string Namespace = "http://www.w3.org/2001/XMLSchema";

    private const string Facets =
        "minExclusive minInclusive maxExclusive maxInclusive totalDigits fractionDigits length minLength maxLength enumeration whiteSpace pattern explicitTimezone assertion";
    private const string Particles = "element group choice sequence any";
    private const string Definitions = "simpleType complexType group attributeGroup";
    private const string Id = "id";
    private const string Occurs = "minOccurs:uint maxOccurs:max";
    private const string Wildcard = "namespace processContents:set=skip|lax|strict notNamespace notQName";
    private const string ContentChildren =
        "annotation openContent group all choice sequence attribute attributeGroup anyAttribute assert";

    private static readonly Dictionary<string, SchemaElementRule> Rules = Build();

    /// <summary>Finds the rule of an element; the context decides for <c>restriction</c>, <c>extension</c>, <see langword="group"/>, and <c>attributeGroup</c>.</summary>
    /// <param name="name">The local name, known to the grammar.</param>
    /// <param name="parent">The local name of the parent element.</param>
    /// <returns>The rule.</returns>
    internal static SchemaElementRule Find(string name, string parent) =>
        name switch
        {
            "restriction" => Rules[
                parent switch
                {
                    "simpleType" => "restriction@simpleType",
                    "simpleContent" => "restriction@simpleContent",
                    _ => "restriction@complexContent",
                }
            ],
            "extension" => Rules[
                string.Equals(parent, "simpleContent", StringComparison.Ordinal)
                    ? "extension@simpleContent"
                    : "extension@complexContent"
            ],
            "group" => Rules[IsDefinitionParent(parent) ? "group@definition" : "group@reference"],
            "attributeGroup" => Rules[
                IsDefinitionParent(parent) ? "attributeGroup@definition" : "attributeGroup@reference"
            ],
            _ => Rules[name],
        };

    /// <summary>Tells whether a name is a schema element in some context.</summary>
    /// <param name="name">The local name.</param>
    /// <returns><see langword="true"/> when known.</returns>
    internal static bool IsKnown(string name) =>
        Rules.ContainsKey(name) || name is "restriction" or "extension" or "group" or "attributeGroup";

    private static bool IsDefinitionParent(string parent) => parent is "schema" or "redefine" or "override";

    private static Dictionary<string, SchemaElementRule> Build()
    {
        var rules = new Dictionary<string, SchemaElementRule>(StringComparer.Ordinal);
        AddDocumentElements(rules);
        AddSimpleTypeElements(rules);
        AddComplexTypeElements(rules);
        AddParticleElements(rules);
        AddIdentityConstraintElements(rules);
        return rules;
    }

    private static void Add(
        Dictionary<string, SchemaElementRule> rules,
        string key,
        DiagnosticCode code,
        string children,
        string attributes,
        bool opaque = false,
        bool repeatable = false
    ) => rules.Add(key, new SchemaElementRule(key.Split('@')[0], code, children, attributes, opaque, repeatable));

    private static void AddDocumentElements(Dictionary<string, SchemaElementRule> rules)
    {
        Add(
            rules,
            "schema",
            XsdRuleCodes.SchPropsCorrect,
            "include import redefine override annotation defaultOpenContent simpleType complexType group attributeGroup element attribute notation",
            Id
                + " targetNamespace:nonempty version attributeFormDefault:set=qualified|unqualified elementFormDefault:set=qualified|unqualified"
                + " blockDefault:list=extension|restriction|substitution finalDefault:list=extension|restriction|list|union"
                + " defaultAttributes:qname xpathDefaultNamespace:xpathns",
            repeatable: true
        );
        Add(rules, "include", XsdRuleCodes.SrcInclude, "annotation", Id + " schemaLocation!");
        Add(rules, "import", XsdRuleCodes.SrcImport, "annotation", Id + " namespace schemaLocation");
        Add(
            rules,
            "redefine",
            XsdRuleCodes.SrcRedefine,
            "annotation " + Definitions,
            Id + " schemaLocation!",
            repeatable: true
        );
        Add(
            rules,
            "override",
            XsdRuleCodes.SrcOverride,
            "annotation " + Definitions + " element attribute notation",
            Id + " schemaLocation!",
            repeatable: true
        );
        Add(rules, "annotation", XsdRuleCodes.SrcAnnotation, "appinfo documentation", Id);
        Add(rules, "appinfo", XsdRuleCodes.SrcAnnotation, string.Empty, "source", opaque: true);
        Add(rules, "documentation", XsdRuleCodes.SrcAnnotation, string.Empty, "source", opaque: true);
        Add(
            rules,
            "defaultOpenContent",
            XsdRuleCodes.SrcCt,
            "annotation any",
            Id + " appliesToEmpty:bool mode:set=interleave|suffix"
        );
        Add(rules, "openContent", XsdRuleCodes.SrcCt, "annotation any", Id + " mode:set=none|interleave|suffix");
    }

    private static void AddSimpleTypeElements(Dictionary<string, SchemaElementRule> rules)
    {
        Add(
            rules,
            "simpleType",
            XsdRuleCodes.SrcSimpleType,
            "annotation restriction list union",
            Id + " name:ncname final:list=restriction|list|union|extension"
        );
        Add(
            rules,
            "restriction@simpleType",
            XsdRuleCodes.SrcSimpleType,
            "annotation simpleType " + Facets,
            Id + " base:qname"
        );
        Add(rules, "list", XsdRuleCodes.SrcSimpleType, "annotation simpleType", Id + " itemType:qname");
        Add(rules, "union", XsdRuleCodes.SrcSimpleType, "annotation simpleType", Id + " memberTypes:qnames");
        foreach (
            var facet in new[]
            {
                "minExclusive",
                "minInclusive",
                "maxExclusive",
                "maxInclusive",
                "totalDigits",
                "fractionDigits",
                "length",
                "minLength",
                "maxLength",
            }
        )
        {
            Add(rules, facet, XsdRuleCodes.SrcSimpleType, "annotation", Id + " value! fixed:bool");
        }

        Add(rules, "enumeration", XsdRuleCodes.SrcSimpleType, "annotation", Id + " value!");
        Add(rules, "pattern", XsdRuleCodes.SrcSimpleType, "annotation", Id + " value!");
        Add(
            rules,
            "whiteSpace",
            XsdRuleCodes.SrcSimpleType,
            "annotation",
            Id + " value:set=preserve|replace|collapse! fixed:bool"
        );
        Add(
            rules,
            "explicitTimezone",
            XsdRuleCodes.SrcSimpleType,
            "annotation",
            Id + " value:set=optional|required|prohibited! fixed:bool"
        );
        Add(rules, "assertion", XsdRuleCodes.SrcSimpleType, "annotation", Id + " test! xpathDefaultNamespace:xpathns");
    }

    private static void AddComplexTypeElements(Dictionary<string, SchemaElementRule> rules)
    {
        Add(rules, "assert", XsdRuleCodes.SrcCt, "annotation", Id + " test! xpathDefaultNamespace:xpathns");
        Add(
            rules,
            "complexType",
            XsdRuleCodes.SrcCt,
            "annotation simpleContent complexContent openContent group all choice sequence attribute attributeGroup anyAttribute assert",
            Id
                + " name:ncname mixed:bool abstract:bool final:list=extension|restriction block:list=extension|restriction defaultAttributesApply:bool"
        );
        Add(rules, "simpleContent", XsdRuleCodes.SrcCt, "annotation restriction extension", Id);
        Add(rules, "complexContent", XsdRuleCodes.SrcCt, "annotation restriction extension", Id + " mixed:bool");
        Add(
            rules,
            "restriction@simpleContent",
            XsdRuleCodes.SrcCt,
            "annotation simpleType " + Facets + " attribute attributeGroup anyAttribute assert",
            Id + " base:qname!"
        );
        Add(rules, "restriction@complexContent", XsdRuleCodes.SrcCt, ContentChildren, Id + " base:qname!");
        Add(
            rules,
            "extension@simpleContent",
            XsdRuleCodes.SrcCt,
            "annotation attribute attributeGroup anyAttribute assert",
            Id + " base:qname!"
        );
        Add(rules, "extension@complexContent", XsdRuleCodes.SrcCt, ContentChildren, Id + " base:qname!");
    }

    private static void AddParticleElements(Dictionary<string, SchemaElementRule> rules)
    {
        Add(
            rules,
            "group@definition",
            XsdRuleCodes.SrcModelGroupDefn,
            "annotation all choice sequence",
            Id + " name:ncname"
        );
        Add(rules, "group@reference", XsdRuleCodes.SrcModelGroupDefn, "annotation", Id + " ref:qname " + Occurs);
        Add(rules, "all", XsdRuleCodes.SrcModelGroup, "annotation element any group", Id + " " + Occurs);
        Add(rules, "choice", XsdRuleCodes.SrcModelGroup, "annotation " + Particles, Id + " " + Occurs);
        Add(rules, "sequence", XsdRuleCodes.SrcModelGroup, "annotation " + Particles, Id + " " + Occurs);
        Add(rules, "any", XsdRuleCodes.SrcWildcard, "annotation", Id + " " + Wildcard + " " + Occurs);
        Add(rules, "anyAttribute", XsdRuleCodes.SrcWildcard, "annotation", Id + " " + Wildcard);

        Add(
            rules,
            "attribute",
            XsdRuleCodes.SrcAttribute,
            "annotation simpleType",
            Id
                + " name:ncname ref:qname type:qname use:set=optional|prohibited|required default fixed form:set=qualified|unqualified targetNamespace inheritable:bool"
        );
        Add(
            rules,
            "attributeGroup@definition",
            XsdRuleCodes.SrcAttributeGroup,
            "annotation attribute attributeGroup anyAttribute",
            Id + " name:ncname"
        );
        Add(rules, "attributeGroup@reference", XsdRuleCodes.SrcAttributeGroup, "annotation", Id + " ref:qname");
        Add(
            rules,
            "element",
            XsdRuleCodes.SrcElement,
            "annotation simpleType complexType alternative unique key keyref",
            Id
                + " name:ncname ref:qname type:qname substitutionGroup:qnames default fixed form:set=qualified|unqualified nillable:bool abstract:bool"
                + " final:list=extension|restriction block:list=extension|restriction|substitution targetNamespace "
                + Occurs
        );
        Add(
            rules,
            "alternative",
            XsdRuleCodes.SrcTa,
            "annotation simpleType complexType",
            Id + " test type:qname xpathDefaultNamespace:xpathns"
        );
    }

    private static void AddIdentityConstraintElements(Dictionary<string, SchemaElementRule> rules)
    {
        Add(
            rules,
            "unique",
            XsdRuleCodes.SrcIdentityConstraint,
            "annotation selector field",
            Id + " name:ncname ref:qname"
        );
        Add(
            rules,
            "key",
            XsdRuleCodes.SrcIdentityConstraint,
            "annotation selector field",
            Id + " name:ncname ref:qname"
        );
        Add(
            rules,
            "keyref",
            XsdRuleCodes.SrcIdentityConstraint,
            "annotation selector field",
            Id + " name:ncname ref:qname refer:qname!"
        );
        Add(
            rules,
            "selector",
            XsdRuleCodes.SrcIdentityConstraint,
            "annotation",
            Id + " xpath! xpathDefaultNamespace:xpathns"
        );
        Add(
            rules,
            "field",
            XsdRuleCodes.SrcIdentityConstraint,
            "annotation",
            Id + " xpath! xpathDefaultNamespace:xpathns"
        );
        Add(rules, "notation", XsdRuleCodes.SrcNotation, "annotation", Id + " name:ncname! public system");
    }
}
