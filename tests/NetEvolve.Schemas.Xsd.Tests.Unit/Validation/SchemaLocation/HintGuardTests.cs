namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.SchemaLocation;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Tests.Unit.Composition;
using NetEvolve.Schemas.Xsd.Validation.Core;
using NetEvolve.Schemas.Xsd.Validation.SchemaLocation;

public class HintGuardTests
{
    private static XsdIssueReporter Reporter => new(new ValidationIssueCollector(ValidationLimits.Default));

    private static SchemaLocationHints Hints() =>
        new(
            XsdVersion.V11,
            DenyAllResourceResolver.Instance,
            ValidationLimits.Default,
            Reporter,
            null,
            CancellationToken.None
        );

    private static ValidationModel Model => new Core.SchemaFixture().Model();

    [Test]
    public async Task Hints_VersionBelowTheRange_Throws() =>
        await Assert
            .That(() =>
                new SchemaLocationHints(
                    (XsdVersion)(-1),
                    DenyAllResourceResolver.Instance,
                    ValidationLimits.Default,
                    Reporter,
                    null,
                    CancellationToken.None
                )
            )
            .Throws<ArgumentOutOfRangeException>();

    [Test]
    public async Task Hints_VersionAboveTheRange_Throws() =>
        await Assert
            .That(() =>
                new SchemaLocationHints(
                    (XsdVersion)99,
                    DenyAllResourceResolver.Instance,
                    ValidationLimits.Default,
                    Reporter,
                    null,
                    CancellationToken.None
                )
            )
            .Throws<ArgumentOutOfRangeException>();

    [Test]
    public async Task Hints_NullResolver_Throws() =>
        await Assert
            .That(() =>
                new SchemaLocationHints(
                    XsdVersion.V11,
                    null!,
                    ValidationLimits.Default,
                    Reporter,
                    null,
                    CancellationToken.None
                )
            )
            .Throws<ArgumentNullException>();

    [Test]
    public async Task Hints_NullLimits_Throws() =>
        await Assert
            .That(() =>
                new SchemaLocationHints(
                    XsdVersion.V11,
                    DenyAllResourceResolver.Instance,
                    null!,
                    Reporter,
                    null,
                    CancellationToken.None
                )
            )
            .Throws<ArgumentNullException>();

    [Test]
    public async Task Hints_NullReporter_Throws() =>
        await Assert
            .That(() =>
                new SchemaLocationHints(
                    XsdVersion.V11,
                    DenyAllResourceResolver.Instance,
                    ValidationLimits.Default,
                    null!,
                    null,
                    CancellationToken.None
                )
            )
            .Throws<ArgumentNullException>();

    [Test]
    public async Task NoteUse_NullNamespace_Throws() =>
        await Assert.That(() => Hints().NoteUse(null!)).Throws<ArgumentNullException>();

    [Test]
    public async Task ApplySchemaLocation_NullModel_Throws() =>
        await Assert
            .That(() => Hints().ApplySchemaLocation(null!, "urn:h h.xsd".AsSpan(), default))
            .Throws<ArgumentNullException>();

    [Test]
    public async Task ApplyNoNamespaceSchemaLocation_NullModel_Throws() =>
        await Assert
            .That(() => Hints().ApplyNoNamespaceSchemaLocation(null!, "n.xsd".AsSpan(), default))
            .Throws<ArgumentNullException>();

    [Test]
    public async Task BudgetResolver_NullInner_Throws() =>
        await Assert.That(() => new BudgetResolver(null!, 1)).Throws<ArgumentNullException>();

    [Test]
    public async Task BudgetResolver_NegativeAllowance_Throws() =>
        await Assert
            .That(() => new BudgetResolver(DenyAllResourceResolver.Instance, -1))
            .Throws<ArgumentOutOfRangeException>();

    [Test]
    public async Task BudgetResolver_CanceledToken_ThrowsBeforeAnything()
    {
        var inner = new MapResolver();
        var budget = new BudgetResolver(inner, 1);
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        await Assert
            .That(() =>
                budget.Resolve(new ResourceRequest(ResourceKind.SchemaLocationHint, "a", null, null), source.Token)
            )
            .Throws<OperationCanceledException>();
        await Assert.That(inner.Requests).IsEmpty();
        await Assert.That(budget.Forwarded).IsEqualTo(0);
    }

    [Test]
    public async Task BudgetResolver_ForwardsTheAllowanceAndRefusesTheRestWithTheLimitMessage(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var inner = new MapResolver().Add("file:///a.xsd", "<a/>");
        var budget = new BudgetResolver(inner, 1);
        var request = new ResourceRequest(ResourceKind.SchemaLocationHint, "a.xsd", null, "file:///a.xsd");

        using var first = budget.Resolve(in request, CancellationToken.None);
        using var second = budget.Resolve(in request, CancellationToken.None);

        await Assert.That(first.Status).IsEqualTo(ResourceStatus.Found);
        await Assert.That(second.Status).IsEqualTo(ResourceStatus.Denied);
        await Assert.That(second.Message).IsEqualTo(BudgetResolver.LimitMessage);
        await Assert.That(inner.Requests.Count).IsEqualTo(1);
        await Assert.That(budget.Forwarded).IsEqualTo(1);
    }

    [Test]
    public async Task BudgetResolver_NoAllowance_NeverReachesTheResolver(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var inner = new MapResolver().Add("file:///a.xsd", "<a/>");
        var budget = new BudgetResolver(inner, 0);

        using var result = budget.Resolve(
            new ResourceRequest(ResourceKind.SchemaLocationHint, "a.xsd", null, "file:///a.xsd"),
            CancellationToken.None
        );

        await Assert.That(result.Status).IsEqualTo(ResourceStatus.Denied);
        await Assert.That(inner.Requests).IsEmpty();
    }

    [Test]
    public async Task Extend_NullArguments_Throw()
    {
        var model = Model;
        var set = new ComponentRegistry(Reporter).Freeze(XsdVersion.V11);
        IReadOnlyList<IXsdComponent> none = [];

        await Assert.That(() => model.Extend(null!, none, Reporter)).Throws<ArgumentNullException>();
        await Assert.That(() => model.Extend(set, null!, Reporter)).Throws<ArgumentNullException>();
        await Assert.That(() => model.Extend(set, none, null!)).Throws<ArgumentNullException>();
        await Assert.That(() => model.Extend(set, [null!], Reporter)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task WithParent_NullParent_Throws()
    {
        var set = new ComponentRegistry(Reporter).Freeze(XsdVersion.V11);

        await Assert.That(() => set.WithParent(null!)).Throws<ArgumentNullException>();
    }
}
