namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Entities;

using System.Text;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Attributes.Declarations;
using NetEvolve.Schemas.Xsd.SimpleTypes.Definitions;
using NetEvolve.Schemas.Xsd.Tests.Unit.SimpleTypes.Definitions;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Ids;
using NetEvolve.Schemas.Xsd.Validation.Entities;

public class EntityTests
{
    private const string Subset = EntityFixture.Declarations;

    private static async Task AssertIssue(ValidationIssue issue, int line, string messagePart)
    {
        await Assert.That(issue.Code).IsEqualTo(ValidationHarness.Code("cvc-simple-type"));
        await Assert.That(issue.Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(issue.Constraint).IsEqualTo("cvc-simple-type");
        await Assert.That(issue.Location.Line).IsEqualTo(line);
        await Assert.That(issue.Location.Column).IsGreaterThan(0);
        await Assert.That(issue.Location.Source).IsEqualTo(ValidationHarness.Source);
        await Assert.That(issue.Message).Contains(messagePart);
    }

    [Test]
    public async Task Entity_ElementValueNamingAnUnparsedEntityOfTheInternalSubset_IsValid()
    {
        var issues = EntityFixture.Run(EntityFixture.Schema(), EntityFixture.Document(Subset, "<n>pic</n>\n"));

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Entity_AttributeValueNamingAnUnparsedEntityOfTheInternalSubset_IsValid()
    {
        var issues = EntityFixture.Run(EntityFixture.Schema(), EntityFixture.Document(Subset, "<e ent=\"logo\"/>\n"));

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Entity_UndeclaredName_AsElementValue_ReportsOneLocatedIssue()
    {
        var issues = EntityFixture.Run(
            EntityFixture.Schema(),
            EntityFixture.Document(Subset, "<n>pic</n>\n<n>nope</n>\n")
        );

        await Assert.That(issues.Count).IsEqualTo(1);
        await AssertIssue(issues[0], 4, "'nope'");
        await Assert.That(issues[0].Message).Contains("no unparsed entity");
    }

    [Test]
    public async Task Entity_UndeclaredName_AsAttributeValue_ReportsOneLocatedIssue()
    {
        var issues = EntityFixture.Run(
            EntityFixture.Schema(),
            EntityFixture.Document(Subset, "<e/>\n<e ent=\"nope\"/>\n")
        );

        await Assert.That(issues.Count).IsEqualTo(1);
        await AssertIssue(issues[0], 4, "'nope'");
    }

    [Test]
    public async Task Entity_NameOfAParsedInternalEntity_ReportsOneLocatedIssue()
    {
        var issues = EntityFixture.Run(EntityFixture.Schema(), EntityFixture.Document(Subset, "<n>text</n>\n"));

        await Assert.That(issues.Count).IsEqualTo(1);
        await AssertIssue(issues[0], 3, "'text'");
        await Assert.That(issues[0].Message).Contains("parsed entity");
    }

    [Test]
    public async Task Entity_NameOfAParsedExternalEntity_ReportsOneLocatedIssue()
    {
        var issues = EntityFixture.Run(EntityFixture.Schema(), EntityFixture.Document(Subset, "<e ent=\"ext\"/>\n"));

        await Assert.That(issues.Count).IsEqualTo(1);
        await AssertIssue(issues[0], 3, "'ext'");
        await Assert.That(issues[0].Message).Contains("parsed entity");
    }

    [Test]
    public async Task Entity_NameOfAParameterEntity_IsNotDeclared()
    {
        // Parameter entities live in their own name space: neither the parsed nor the unparsed one counts.
        var issues = EntityFixture.Run(
            EntityFixture.Schema(),
            EntityFixture.Document(Subset, "<n>pe</n>\n<n>pext</n>\n")
        );

        await Assert.That(issues.Count).IsEqualTo(2);
        await AssertIssue(issues[0], 3, "'pe'");
        await AssertIssue(issues[1], 4, "'pext'");
        await Assert.That(issues[0].Message).Contains("no unparsed entity");
    }

    [Test]
    public async Task Entity_NamesThatDifferInCase_AreDistinct()
    {
        var issues = EntityFixture.Run(EntityFixture.Schema(), EntityFixture.Document(Subset, "<n>PIC</n>\n"));

        await Assert.That(issues.Count).IsEqualTo(1);
        await AssertIssue(issues[0], 3, "'PIC'");
    }

    [Test]
    public async Task Entity_SurroundingWhiteSpace_IsCollapsedBeforeTheLookup()
    {
        var issues = EntityFixture.Run(
            EntityFixture.Schema(),
            EntityFixture.Document(Subset, "<n>  pic\t</n>\n<e ent=\" logo \"/>\n")
        );

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Entities_AllNamesDeclared_AreValid()
    {
        var issues = EntityFixture.Run(
            EntityFixture.Schema(),
            EntityFixture.Document(Subset, "<ns>pic logo  pic</ns>\n<e ents=\"logo pic\"/>\n")
        );

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Entities_OneDeclaredAndOneUndeclaredName_GiveExactlyOneIssueAtTheValue()
    {
        var issues = EntityFixture.Run(EntityFixture.Schema(), EntityFixture.Document(Subset, "<ns>pic\n nope</ns>\n"));

        await Assert.That(issues.Count).IsEqualTo(1);
        await AssertIssue(issues[0], 3, "'nope'");
    }

    [Test]
    public async Task Entities_AttributeWithOneDeclaredAndOneUndeclaredName_GivesExactlyOneIssueAtTheAttribute()
    {
        var issues = EntityFixture.Run(
            EntityFixture.Schema(),
            EntityFixture.Document(Subset, "<e/>\n<e ents=\"nope pic\"/>\n")
        );

        await Assert.That(issues.Count).IsEqualTo(1);
        await AssertIssue(issues[0], 4, "'nope'");
    }

    [Test]
    public async Task Entities_EveryOffendingToken_GetsItsOwnIssue_InTheOrderOfTheTokens()
    {
        var issues = EntityFixture.Run(
            EntityFixture.Schema(),
            EntityFixture.Document(Subset, "<ns>first pic text last</ns>\n")
        );

        await Assert.That(issues.Count).IsEqualTo(3);
        await Assert.That(issues[0].Message).Contains("'first'");
        await Assert.That(issues[1].Message).Contains("'text'");
        await Assert.That(issues[1].Message).Contains("parsed entity");
        await Assert.That(issues[2].Message).Contains("'last'");
    }

    [Test]
    public async Task Entity_WithoutADocumentTypeDeclaration_EveryValueIsInvalid()
    {
        var issues = EntityFixture.Run(
            EntityFixture.Schema(),
            EntityFixture.Document(null, "<n>pic</n>\n<e ent=\"pic\" ents=\"pic logo\"/>\n")
        );

        await Assert.That(issues.Count).IsEqualTo(4);
        await AssertIssue(issues[0], 2, "'pic'");
        await Assert.That(issues[0].Message).Contains("no document type declaration");
        await AssertIssue(issues[1], 3, "'pic'");
        await AssertIssue(issues[2], 3, "'pic'");
        await AssertIssue(issues[3], 3, "'logo'");
    }

    [Test]
    public async Task Entity_DocumentTypeWithoutDeclarations_EveryValueIsUndeclared()
    {
        var issues = EntityFixture.Run(EntityFixture.Schema(), EntityFixture.Document(string.Empty, "<n>pic</n>\n"));

        await Assert.That(issues.Count).IsEqualTo(1);
        await AssertIssue(issues[0], 3, "'pic'");
        await Assert.That(issues[0].Message).Contains("no unparsed entity");
    }

    [Test]
    public async Task Entity_FirstDeclarationBinds_AsInTheDeclarationModel()
    {
        // The parsed declaration comes first, so the later unparsed one does not count.
        var issues = EntityFixture.Run(
            EntityFixture.Schema(),
            EntityFixture.Document(
                "<!NOTATION gif SYSTEM \"g\"><!ENTITY twin \"x\"><!ENTITY twin SYSTEM \"t.gif\" NDATA gif>",
                "<n>twin</n>\n"
            )
        );

        await Assert.That(issues.Count).IsEqualTo(1);
        await AssertIssue(issues[0], 3, "parsed entity");
    }

    [Test]
    public async Task Entity_OnADerivedType_IsChecked()
    {
        var schema = new SchemaFixture();
        var derived = EntityFixture.Restriction(
            EntityFixture.Restriction(SchemaFixture.Simple("ENTITY"), schema),
            schema
        );

        var issues = EntityFixture.Run(
            EntityFixture.Schema(entity: derived),
            EntityFixture.Document(Subset, "<n>pic</n>\n<n>nope</n>\n")
        );

        await Assert.That(issues.Count).IsEqualTo(1);
        await AssertIssue(issues[0], 4, "'nope'");
    }

    [Test]
    public async Task Entities_OnADerivedType_IsCheckedPerToken()
    {
        var schema = new SchemaFixture();
        var derived = EntityFixture.Restriction(SchemaFixture.Simple("ENTITIES"), schema);

        var issues = EntityFixture.Run(
            EntityFixture.Schema(entities: derived),
            EntityFixture.Document(Subset, "<ns>pic nope</ns>\n")
        );

        await Assert.That(issues.Count).IsEqualTo(1);
        await AssertIssue(issues[0], 3, "'nope'");
    }

    [Test]
    public async Task Entities_OnARestrictionOfTheBuiltInListType_IsCheckedPerToken()
    {
        var schema = new SchemaFixture();
        var builtIn = SimpleTypeDefinition.FromDatatype(
            new XPathQName(SchemaFixture.Xs, "ENTITIES"),
            SchemaFixture.Where,
            new FakeDatatype("ENTITIES", DatatypeVariety.List)
        );
        var derived = EntityFixture.Restriction(builtIn, schema);

        var issues = EntityFixture.Run(
            EntityFixture.Schema(entities: derived),
            EntityFixture.Document(Subset, "<ns>pic nope</ns>\n")
        );

        await Assert.That(issues.Count).IsEqualTo(1);
        await AssertIssue(issues[0], 3, "'nope'");
    }

    [Test]
    public async Task Entity_ItemsOfAUserDefinedListOfEntity_AreCheckedPerItem()
    {
        var schema = new SchemaFixture();
        var list = EntityFixture.ListOf(EntityFixture.Restriction(SchemaFixture.Simple("ENTITY"), schema), schema);

        var issues = EntityFixture.Run(
            EntityFixture.Schema(entities: list),
            EntityFixture.Document(Subset, "<ns>pic nope logo</ns>\n")
        );

        await Assert.That(issues.Count).IsEqualTo(1);
        await AssertIssue(issues[0], 3, "'nope'");
    }

    [Test]
    public async Task Entity_UnionMemberThatAcceptsTheValue_DecidesWhetherItIsChecked()
    {
        var schema = new SchemaFixture();
        var union = EntityFixture.UnionOf(schema, SchemaFixture.Simple("int"), SchemaFixture.Simple("ENTITY"));

        // The first member takes digits and decides; the second takes the rest and names an entity.
        var issues = EntityFixture.Run(
            EntityFixture.Schema(entity: union),
            EntityFixture.Document(Subset, "<n>42</n>\n<n>pic</n>\n<n>nope</n>\n")
        );

        await Assert.That(issues.Count).IsEqualTo(1);
        await AssertIssue(issues[0], 5, "'nope'");
    }

    [Test]
    public async Task Entity_UnionWhoseMembersAllRejectTheValue_ReportsNothing()
    {
        var schema = new SchemaFixture();
        var union = EntityFixture.UnionOf(schema, SchemaFixture.Simple("ENTITY"), SchemaFixture.Simple("ENTITY"));
        var assessor = new MembersRejectingAssessor();

        var issues = EntityFixture.Run(
            EntityFixture.Schema(entity: union),
            EntityFixture.Document(Subset, "<n>nope</n>\n"),
            assessor,
            null
        );

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Entity_ValueThatTheDatatypeAlreadyRejected_IsNotCheckedAgain()
    {
        // The datatype fake rejects a value that starts with a digit; the validator reports it, the listener adds nothing.
        var issues = EntityFixture.Run(
            EntityFixture.Schema(),
            EntityFixture.Document(Subset, "<n>1x</n>\n<e ent=\"2y\"/>\n"),
            new RejectingAssessor(),
            null
        );

        await Assert.That(issues.Count).IsEqualTo(2);
        await Assert.That(issues[0].Message).DoesNotContain("unparsed entity");
        await Assert.That(issues[1].Message).DoesNotContain("unparsed entity");
    }

    [Test]
    public async Task Entity_DefaultedAttributeValue_IsCheckedAgainstTheDeclarationsOfTheInstance()
    {
        // Whether a default value names an unparsed entity depends on the DTD of each instance, so it is no schema error.
        var schema = EntityFixture.Schema(
            entConstraint: new AttributeValueConstraint(AttributeValueConstraintKind.Default, "pic")
        );

        var declared = EntityFixture.Run(schema, EntityFixture.Document(Subset, "<e/>\n"));
        var undeclared = EntityFixture.Run(schema, EntityFixture.Document("<!ENTITY other \"x\">", "<e/>\n<e/>\n"));

        await Assert.That(declared.Count).IsEqualTo(0);
        await Assert.That(undeclared.Count).IsEqualTo(2);
        await AssertIssue(undeclared[0], 3, "'pic'");
        await AssertIssue(undeclared[1], 4, "'pic'");
    }

    [Test]
    public async Task Entity_ValueLongerThan64Characters_IsCutAt64CharactersAndMarked()
    {
        var token = new string('x', 100);

        var issues = EntityFixture.Run(
            EntityFixture.Schema(),
            EntityFixture.Document(Subset, "<n>" + token + "</n>\n")
        );

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Message).Contains("'" + new string('x', 64) + "...'");
        await Assert.That(issues[0].Message).DoesNotContain(new string('x', 65));
    }

    [Test]
    public async Task Entity_ValueOfExactly64Characters_IsNotCut()
    {
        var token = new string('z', 64);

        var issues = EntityFixture.Run(
            EntityFixture.Schema(),
            EntityFixture.Document(Subset, "<n>" + token + "</n>\n")
        );

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Message).Contains("'" + token + "'");
    }

    [Test]
    public async Task Entity_ValuesOfOtherTypes_AreNeverChecked()
    {
        var schema = EntityFixture.Schema(
            entity: SchemaFixture.Simple("string"),
            entities: SchemaFixture.Simple("string")
        );

        var issues = EntityFixture.Run(
            schema,
            EntityFixture.Document(null, "<n>nope</n>\n<ns>a b</ns>\n<e ent=\"x\" ents=\"y z\"/>\n")
        );

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Entity_TypeNamedEntityInAnotherNamespace_IsNotTheBuiltInType()
    {
        var other = SimpleTypeDefinition.FromDatatype(
            new XPathQName("urn:other", "ENTITY"),
            SchemaFixture.Where,
            new FakeDatatype("ENTITY", DatatypeVariety.Atomic)
        );

        var issues = EntityFixture.Run(
            EntityFixture.Schema(entity: other),
            EntityFixture.Document(null, "<n>nope</n>\n")
        );

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Run_ReusedForTheNextDocument_DoesNotKeepTheDeclarationsOfTheLastOne()
    {
        var assessor = new ScriptedAssessor();
        var listener = new EntityListener(assessor);
        var schema = EntityFixture.Schema();

        var first = EntityFixture.Run(schema, EntityFixture.Document(Subset, "<n>pic</n>\n"), assessor, listener);
        var second = EntityFixture.Run(schema, EntityFixture.Document(null, "<n>pic</n>\n"), assessor, listener);

        await Assert.That(first.Count).IsEqualTo(0);
        await Assert.That(second.Count).IsEqualTo(1);
        await Assert.That(second[0].Message).Contains("no document type declaration");
    }

    [Test]
    public async Task Entity_ManyDeclarations_AreAllKnown()
    {
        var subset = new StringBuilder("<!NOTATION n SYSTEM \"n\">");
        for (var i = 0; i < 200; i++)
        {
            _ = subset
                .Append("<!ENTITY u")
                .Append(i)
                .Append(" SYSTEM \"u.bin\" NDATA n><!ENTITY p")
                .Append(i)
                .Append(" \"x\">");
        }

        var issues = EntityFixture.Run(
            EntityFixture.Schema(),
            EntityFixture.Document(subset.ToString(), "<n>u0</n>\n<n>u199</n>\n<n>p0</n>\n<n>p199</n>\n<n>u200</n>\n")
        );

        await Assert.That(issues.Count).IsEqualTo(3);
        await AssertIssue(issues[0], 5, "'p0'");
        await AssertIssue(issues[1], 6, "'p199'");
        await AssertIssue(issues[2], 7, "'u200'");
    }
}
