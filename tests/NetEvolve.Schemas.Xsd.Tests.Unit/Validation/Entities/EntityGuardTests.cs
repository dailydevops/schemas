namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Entities;

using System;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;
using NetEvolve.Schemas.Xsd.Validation.Entities;

public class EntityGuardTests
{
    [Test]
    public async Task Listener_NullAssessor_Throws() =>
        await Assert.That(() => new EntityListener(null!)).Throws<ArgumentNullException>();
}
