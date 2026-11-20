namespace NetEvolve.Schemas.Xsd.Tests.Unit.IdentityConstraints.Evaluation;

using System;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.IdentityConstraints.Evaluation;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;

public class ListenerGuardTests
{
    [Test]
    public async Task Listener_NullAssessor_Throws() =>
        await Assert
            .That(() => new IdentityConstraintListener(null!, XsdVersion.V11, static _ => null))
            .Throws<ArgumentNullException>();

    [Test]
    public async Task Listener_NullLookup_Throws() =>
        await Assert
            .That(() => new IdentityConstraintListener(new ScriptedAssessor(), XsdVersion.V11, null!))
            .Throws<ArgumentNullException>();

    [Test]
    public async Task Listener_VersionOutsideTheKnownOnes_Throws() =>
        await Assert
            .That(() => new IdentityConstraintListener(new ScriptedAssessor(), (XsdVersion)99, static _ => null))
            .Throws<ArgumentOutOfRangeException>();

    [Test]
    public async Task Listener_WantsEveryHook()
    {
        var listener = new IdentityConstraintListener(new ScriptedAssessor(), XsdVersion.V11, static _ => null);

        await Assert.That(listener.Interest).IsEqualTo(ListenerInterest.All);
    }
}
