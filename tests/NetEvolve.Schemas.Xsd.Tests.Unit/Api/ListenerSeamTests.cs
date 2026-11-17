namespace NetEvolve.Schemas.Xsd.Tests.Unit.Api;

using System.Collections.Generic;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;

public class ListenerSeamTests
{
    [Test]
    public async Task Seam_Listener_HooksRunInTheDocumentedOrderWithTheirArguments()
    {
        var log = new List<string>();
        var listener = new FakeListener("L", ListenerInterest.All, log);

        FakeListenerDriver.Run([listener]);

        await Assert
            .That(string.Join("|", log))
            .IsEqualTo(
                "L:BeginRun:maxIssues=1000|L:StartElement:1:depth=1|L:Attribute:owner=1:value=1:defaulted=False"
                    + "|L:SimpleValue:1:value=7:member=-1|L:EndElement:1:valid=True|L:EndRun"
            );
    }

    [Test]
    public async Task Seam_Listener_UninterestedHooks_AreNeverCalled()
    {
        var log = new List<string>();
        var listener = new FakeListener("L", ListenerInterest.EndElement, log);

        FakeListenerDriver.Run([listener]);

        await Assert
            .That(string.Join("|", log))
            .IsEqualTo("L:BeginRun:maxIssues=1000|L:EndElement:1:valid=True|L:EndRun");
    }

    [Test]
    public async Task Seam_Listeners_AreCalledInTheGivenOrderPerHook()
    {
        var log = new List<string>();
        var first = new FakeListener("A", ListenerInterest.StartElement, log);
        var second = new FakeListener("B", ListenerInterest.StartElement, log);

        FakeListenerDriver.Run([first, second]);

        await Assert
            .That(string.Join("|", log))
            .IsEqualTo(
                "A:BeginRun:maxIssues=1000|B:BeginRun:maxIssues=1000|A:StartElement:1:depth=1|B:StartElement:1:depth=1|A:EndRun|B:EndRun"
            );
    }

    [Test]
    public async Task Seam_Listener_BaseClassHooks_DoNothing()
    {
        var listener = new SilentListener();

        FakeListenerDriver.Run([listener]);

        await Assert.That(listener.Interest).IsEqualTo(ListenerInterest.All);
    }

    private sealed class SilentListener : ValidationListener
    {
        public override ListenerInterest Interest => ListenerInterest.All;
    }
}
