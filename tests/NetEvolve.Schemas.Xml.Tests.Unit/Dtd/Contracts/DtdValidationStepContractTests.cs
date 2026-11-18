namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Contracts;

using System;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Model;
using NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Contracts.Fakes;

public class DtdValidationStepContractTests
{
    private static readonly XmlPosition Position = new(3, 7, 42);

    [Test]
    public async Task Issue_CarriesCodeSeverityAndLocation()
    {
        var issues = new ValidationIssueCollector(ValidationLimits.Default);
        var context = DtdContractFixture.Context(issues);
        var step = new ScriptedDtdValidationStep().On("Text:Text:x", c =>
            c.Report(DtdValidityCodes.XML5004, IssueSeverity.Error, c.Locate(Position), "character data in EMPTY")
        );

        step.BeginDocument(context);
        step.Text("x".AsSpan(), XmlTextKind.Text, in Position);

        var issue = issues.ToResult().Issues[0];
        await Assert.That(issue.Code).IsEqualTo(DtdValidityCodes.XML5004.Code);
        await Assert.That(issue.Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(issue.Location).IsEqualTo(new DocumentLocation("doc.xml", 3, 7));
        await Assert.That(issue.Constraint).IsEqualTo("Element Valid");
        await Assert.That(issue.Message).Contains(DtdValidityCodes.XML5004.Message);
        await Assert.That(issue.Message).Contains("character data in EMPTY");
    }

    [Test]
    public async Task Issue_Severity_IsTheCallersChoice_AndWarningKeepsTheResultValid()
    {
        var issues = new ValidationIssueCollector(ValidationLimits.Default);
        var context = DtdContractFixture.Context(issues);

        context.Report(DtdValidityCodes.XML5911, IssueSeverity.Warning, context.Locate(Position));
        var warningOnly = issues.ToResult();
        context.Report(DtdValidityCodes.XML5911, IssueSeverity.Error, context.Locate(Position), "(a,b)|(a,c)");
        var withError = issues.ToResult();

        await Assert.That(warningOnly.Issues[0].Severity).IsEqualTo(IssueSeverity.Warning);
        await Assert.That(warningOnly.IsValid).IsTrue();
        await Assert.That(withError.Issues[1].Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(withError.IsValid).IsFalse();
    }

    [Test]
    public async Task Issue_ValidityError_DoesNotStopTheRun()
    {
        var issues = new ValidationIssueCollector(ValidationLimits.Default);
        var context = DtdContractFixture.Context(issues);
        var first = new ScriptedDtdValidationStep().On("Comment", c =>
            c.Report(DtdValidityCodes.XML5004, IssueSeverity.Error, c.Locate(Position))
        );
        var second = new ScriptedDtdValidationStep();

        foreach (var step in new[] { first, second })
        {
            step.BeginDocument(context);
            step.Comment(in Position);
            step.EndDocument();
        }

        await Assert.That(issues.IsStopped).IsFalse();
        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(second.Events).IsEquivalentTo(["Begin", "Comment", "End"]);
    }

    [Test]
    public async Task Issue_Truncation_AtMaxIssues()
    {
        var issues = new ValidationIssueCollector(ValidationLimits.Default.WithMaxIssues(2));
        var context = DtdContractFixture.Context(issues);

        for (var i = 0; i < 3; i++)
        {
            context.Report(DtdValidityCodes.XML5012, IssueSeverity.Error, context.Locate(Position), "id" + i);
        }

        var result = issues.ToResult();
        await Assert.That(result.Issues).Count().IsEqualTo(2);
        await Assert.That(result.IsTruncated).IsTrue();
        await Assert.That(result.HasDroppedErrors).IsTrue();
    }

    [Test]
    public async Task Issue_StandaloneClause_IsDiscriminated()
    {
        var issues = new ValidationIssueCollector(ValidationLimits.Default);
        var context = DtdContractFixture.Context(issues, isStandalone: true);

        var clauses = Enum.GetValues<XmlStandaloneClause>();
        foreach (var clause in clauses)
        {
            context.ReportStandalone(clause, context.Locate(Position), "item");
        }

        var result = issues.ToResult();
        await Assert.That(result.Issues).Count().IsEqualTo(4);
        await Assert.That(clauses).Count().IsEqualTo(4);
        var messages = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < result.Issues.Count; i++)
        {
            var issue = result.Issues[i];
            messages.Add(issue.Message);
            await Assert.That(issue.Code).IsEqualTo(DtdValidityCodes.XML5003.Code);
            await Assert.That(issue.Constraint).IsEqualTo("Standalone Document Declaration");
            await Assert.That(issue.Message).Contains(((XmlStandaloneClause)i).ToString());
        }

        await Assert.That(messages).Count().IsEqualTo(4);
    }

    [Test]
    public async Task Stop_EndsTheRunWithAFatalLimitCode_AndLaterIssuesAreIgnored()
    {
        var issues = new ValidationIssueCollector(ValidationLimits.Default);
        var context = DtdContractFixture.Context(issues);
        context.Report(DtdValidityCodes.XML5004, IssueSeverity.Error, context.Locate(Position));
        context.Stop(DtdLimitCodes.XML1118, context.Locate(Position));
        context.Report(DtdValidityCodes.XML5004, IssueSeverity.Error, context.Locate(Position));
        var result = issues.ToResult();
        await Assert.That(context.IsStopped).IsTrue();
        await Assert.That(result.Issues).Count().IsEqualTo(2);
        await Assert.That(result.Issues[1].Code).IsEqualTo(DtdLimitCodes.XML1118.Code);
        await Assert.That(result.Issues[1].Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(result.Issues[1].Constraint).IsEqualTo(DtdLimitCodes.XML1118.Constraint);
        await Assert.That(result.IsTruncated).IsTrue();
        await Assert.That(result.IsValid).IsFalse();
    }

    [Test]
    public async Task Stop_IsNotUsedByValidityIssues()
    {
        var issues = new ValidationIssueCollector(ValidationLimits.Default);
        var context = DtdContractFixture.Context(issues);
        context.Report(DtdValidityCodes.XML5004, IssueSeverity.Error, context.Locate(Position));
        await Assert.That(context.IsStopped).IsFalse();
    }

    [Test]
    public async Task Issue_StandaloneClause_WithoutArgument_NamesOnlyTheClause()
    {
        var issues = new ValidationIssueCollector(ValidationLimits.Default);
        var context = DtdContractFixture.Context(issues, isStandalone: true);

        context.ReportStandalone(XmlStandaloneClause.Defaults, context.Locate(Position));

        var issue = issues.ToResult().Issues[0];
        await Assert.That(issue.Message).EndsWith("(clause: " + XmlStandaloneClause.Defaults + ")");
    }

    [Test]
    public async Task Context_ExposesDeclarationsOptionsStandaloneFlagAndSystemId()
    {
        var issues = new ValidationIssueCollector(ValidationLimits.Default);
        var options = DtdValidationOptions.Default.WithAllowExternalSubset(true);

        var context = DtdContractFixture.Context(issues, options, isStandalone: true, systemId: "file:///doc.xml");

        await Assert.That(context.Declarations.Name).IsEqualTo("root");
        await Assert.That(context.Declarations.Elements.ContainsKey("root")).IsTrue();
        await Assert.That(context.Options).IsSameReferenceAs(options);
        await Assert.That(context.IsStandalone).IsTrue();
        await Assert.That(context.SystemId).IsEqualTo("file:///doc.xml");
        await Assert.That(context.Locate(Position)).IsEqualTo(new DocumentLocation("file:///doc.xml", 3, 7));
    }

    [Test]
    public async Task Context_Guards_RejectNullArguments()
    {
        var issues = new ValidationIssueCollector(ValidationLimits.Default);
        var context = DtdContractFixture.Context(issues);
        var declarations = DtdContractFixture.Declarations();

        await Assert
            .That(() => new DtdValidationContext(null!, DtdValidationOptions.Default, issues, false, null))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => new DtdValidationContext(declarations, null!, issues, false, null))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => new DtdValidationContext(declarations, DtdValidationOptions.Default, null!, false, null))
            .Throws<ArgumentNullException>();
        await Assert.That(() => context.Report(null!, IssueSeverity.Error, default)).Throws<ArgumentNullException>();
        await Assert.That(() => context.Stop(null!, default)).Throws<ArgumentNullException>();
        await Assert
            .That(() => context.Report(DtdValidityCodes.XML5004, (IssueSeverity)9, default))
            .Throws<ArgumentOutOfRangeException>();
        await Assert
            .That(() => context.ReportStandalone((XmlStandaloneClause)9, default))
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Events_StartElement_CarriesNamePositionAndAttributes()
    {
        var attributes = new[]
        {
            new ValidationAttributeInfo(
                "id".AsMemory(),
                "a1".AsMemory(),
                DtdAttributeType.Id,
                isSpecified: true,
                new XmlPosition(3, 15, 50)
            ),
            new ValidationAttributeInfo(
                "kind".AsMemory(),
                "x".AsMemory(),
                declaredType: null,
                isSpecified: false,
                new XmlPosition(3, 22, 57)
            ),
        };
        var step = new ScriptedDtdValidationStep();

        var element = new ValidationElement("root".AsMemory(), Position, attributes);
        step.StartElement(in element);

        var received = step.LastStartElement;
        await Assert.That(step.Events).IsEquivalentTo(["StartElement:root"]);
        await Assert.That(received.Name.ToString()).IsEqualTo("root");
        await Assert.That(received.Position).IsEqualTo(Position);
        await Assert.That(received.Attributes.Length).IsEqualTo(2);
        var first = received.Attributes.Span[0];
        await Assert.That(first.Name.ToString()).IsEqualTo("id");
        await Assert.That(first.Value.ToString()).IsEqualTo("a1");
        await Assert.That(first.DeclaredType).IsEqualTo(DtdAttributeType.Id);
        await Assert.That(first.IsSpecified).IsTrue();
        await Assert.That(first.Position).IsEqualTo(new XmlPosition(3, 15, 50));
        var second = received.Attributes.Span[1];
        await Assert.That(second.DeclaredType).IsNull();
        await Assert.That(second.IsSpecified).IsFalse();
    }

    [Test]
    public async Task Events_EndElement_HasNoAttributes()
    {
        var step = new ScriptedDtdValidationStep();

        var element = new ValidationElement("root".AsMemory(), Position);
        step.EndElement(in element);

        await Assert.That(step.Events).IsEquivalentTo(["EndElement:root"]);
        await Assert.That(element.Attributes.IsEmpty).IsTrue();
    }

    [Test]
    [Arguments(0)]
    [Arguments(1)]
    [Arguments(2)]
    public async Task Events_EntityReference_CarriesTheOutcome(int outcomeValue)
    {
        var outcome = (DtdEntityOutcome)outcomeValue;
        var step = new ScriptedDtdValidationStep();

        var reference = new ValidationEntityReference("copy".AsMemory(), Position, outcome);
        step.EntityReference(in reference);

        await Assert.That(step.Events).IsEquivalentTo(["EntityReference:copy"]);
        await Assert.That(step.LastEntityReference.Outcome).IsEqualTo(outcome);
        await Assert.That(step.LastEntityReference.Position).IsEqualTo(Position);
    }

    [Test]
    public async Task Events_TextCommentAndProcessingInstruction_ReceiveKindAndPosition()
    {
        var step = new ScriptedDtdValidationStep();

        step.Text("a".AsSpan(), XmlTextKind.CData, in Position);
        step.Text("&".AsSpan(), XmlTextKind.Reference, in Position);
        step.Comment(in Position);
        step.ProcessingInstruction(in Position);

        await Assert
            .That(step.Events)
            .IsEquivalentTo(["Text:CData:a", "Text:Reference:&", "Comment", "ProcessingInstruction"]);
        await Assert.That(step.LastPosition).IsEqualTo(Position);
    }

    [Test]
    public async Task Events_Guards_RejectOutOfRangeEnumsAndInvalidPositions()
    {
        await Assert
            .That(() => new ValidationEntityReference("x".AsMemory(), Position, (DtdEntityOutcome)9))
            .Throws<ArgumentOutOfRangeException>();
        await Assert
            .That(() =>
                new ValidationAttributeInfo(
                    "a".AsMemory(),
                    "b".AsMemory(),
                    (DtdAttributeType)99,
                    isSpecified: true,
                    Position
                )
            )
            .Throws<ArgumentOutOfRangeException>();
        await Assert
            .That(() =>
                new ValidationAttributeInfo(
                    "a".AsMemory(),
                    "b".AsMemory(),
                    (DtdAttributeType)(-1),
                    isSpecified: true,
                    Position
                )
            )
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Surface_StepSeamsAndTheirModelView_ArePublic()
    {
        foreach (
            var type in new[]
            {
                typeof(IDtdValidationStep),
                typeof(DtdValidationContext),
                typeof(ValidationElement),
                typeof(ValidationAttributeInfo),
                typeof(ValidationEntityReference),
                typeof(DtdEntityOutcome),
                typeof(XmlStandaloneClause),
                typeof(IDtdDeclarations),
                typeof(DtdElementDeclaration),
                typeof(DtdAttributeDefinition),
                typeof(DtdContentParticle),
                typeof(DtdEntityDeclaration),
                typeof(DtdContentKind),
                typeof(DtdAttributeType),
                typeof(DtdDefaultKind),
                typeof(DtdOccurrence),
                typeof(DtdParticleKind),
                typeof(DtdValidationOptions),
                typeof(DtdValidationLimits),
                typeof(DtdNonDeterminism),
                typeof(DtdValidityCode),
                typeof(DtdValidityCodeKind),
                typeof(DtdValidityCodes),
                typeof(DtdLimitCodes),
            }
        )
        {
            await Assert.That(type.IsPublic).IsTrue();
        }
    }

    [Test]
    public async Task Surface_DeclarationModelBehindTheView_StaysInternal()
    {
        foreach (var type in new[] { typeof(DtdDeclarations), typeof(DtdDeclarationBuilder) })
        {
            await Assert.That(type.IsPublic).IsFalse();
        }

        foreach (
            var type in new[]
            {
                typeof(DtdElementDeclaration),
                typeof(DtdAttributeDefinition),
                typeof(DtdContentParticle),
                typeof(DtdEntityDeclaration),
            }
        )
        {
            await Assert.That(type.IsSealed).IsTrue();
            await Assert.That(type.GetConstructors()).IsEmpty();
        }

        await Assert.That(typeof(IDtdDeclarations).IsAssignableFrom(typeof(DtdDeclarations))).IsTrue();
    }

    [Test]
    public async Task Context_AcceptsAnyDeclarationView_NotOnlyTheParserModel()
    {
        var issues = new ValidationIssueCollector(ValidationLimits.Default);
        IDtdDeclarations view = new FakeDtdDeclarations();

        var context = new DtdValidationContext(view, DtdValidationOptions.Default, issues, false, null);

        await Assert.That(context.Declarations).IsSameReferenceAs(view);
        await Assert.That(context.Declarations.Name).IsEqualTo("fake");
    }

    [Test]
    public async Task Events_PublicConstructors_BuildTheEventsOfAConsumerFakeStep()
    {
        var element = new ValidationElement("a".AsMemory(), Position);
        var reference = new ValidationEntityReference("e".AsMemory(), Position, DtdEntityOutcome.Undeclared);

        await Assert.That(element.Attributes.IsEmpty).IsTrue();
        await Assert.That(element.Name.ToString()).IsEqualTo("a");
        await Assert.That(reference.Outcome).IsEqualTo(DtdEntityOutcome.Undeclared);
        await Assert.That(typeof(ValidationElement).GetConstructors()).IsNotEmpty();
        await Assert.That(typeof(DtdValidationContext).GetConstructors()).IsNotEmpty();
    }
}
