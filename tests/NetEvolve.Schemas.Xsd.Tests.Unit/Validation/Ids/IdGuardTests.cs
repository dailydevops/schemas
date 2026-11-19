namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Ids;

using System;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;
using NetEvolve.Schemas.Xsd.Validation.Ids;

public class IdGuardTests
{
    private static XsdSchemaSet EmptySet =>
        new ComponentRegistry(new XsdIssueReporter(new ValidationIssueCollector(ValidationLimits.Default))).Freeze(
            XsdVersion.V11
        );

    [Test]
    public async Task Listener_NullAssessor_Throws() =>
        await Assert.That(() => new IdListener(null!, EmptySet)).Throws<ArgumentNullException>();

    [Test]
    public async Task Listener_NullSchema_Throws() =>
        await Assert.That(() => new IdListener(new ScriptedAssessor(), null!)).Throws<ArgumentNullException>();
}
