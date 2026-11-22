namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Assembly;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.Xsd.Validation.Assembly;

public class SchemaSourceGuardTests
{
    [Test]
    public async Task FromStream_NullStream_Throws() =>
        await Assert.That(() => SchemaSource.FromStream(null!, null)).Throws<ArgumentNullException>();
}
