namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Annotations;

using System;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;
using NetEvolve.Schemas.Xsd.Validation.Annotations;

public class AnnotationGuardTests
{
    private static readonly XmlTree Tree = AnnotationFixture.AnnotatedRun.Build("<r/>");

    [Test]
    public async Task Source_NullTable_Throws() =>
        await Assert
            .That(() => new AnnotationSource(null!, Tree, new ScriptedAssessor()))
            .Throws<ArgumentNullException>();

    [Test]
    public async Task Source_NullTree_Throws() =>
        await Assert
            .That(() => new AnnotationSource(new AnnotationTable(), null!, new ScriptedAssessor()))
            .Throws<ArgumentNullException>();

    [Test]
    public async Task Source_NullAssessor_Throws() =>
        await Assert
            .That(() => new AnnotationSource(new AnnotationTable(), Tree, null!))
            .Throws<ArgumentNullException>();
}
