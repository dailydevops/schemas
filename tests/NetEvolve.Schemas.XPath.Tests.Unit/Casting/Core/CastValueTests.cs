namespace NetEvolve.Schemas.XPath.Tests.Unit.Casting.Core;

using static CastFixture;

/// <summary>The value of every allowed cast among the seven types; the expected text is the type and the value (<see cref="CastFixture.Render"/>).</summary>
public class CastValueTests
{
    // xs:string source: the lexical form goes through the datatype of the target; whitespace collapses for every target but the two string types.
    [Test]
    [Arguments("'abc' cast as xs:string", "string:abc")]
    [Arguments("' a  b ' cast as xs:string", "string: a  b ")]
    [Arguments("' a  b ' cast as xs:untypedAtomic", "untyped: a  b ")]
    [Arguments("' 1.5E0 ' cast as xs:float", "float:1.5")]
    [Arguments("'-INF' cast as xs:float", "float:-INF")]
    [Arguments("' 1.5 ' cast as xs:double", "double:1.5")]
    [Arguments("'NaN' cast as xs:double", "double:NaN")]
    [Arguments("'-0' cast as xs:double", "double:-0")]
    [Arguments("'1.50' cast as xs:decimal", "decimal:1.5")]
    [Arguments("'.5' cast as xs:decimal", "decimal:0.5")]
    [Arguments("'5.' cast as xs:decimal", "decimal:5")]
    [Arguments("' +12 ' cast as xs:integer", "integer:12")]
    [Arguments("'-0' cast as xs:integer", "integer:0")]
    [Arguments("'007' cast as xs:integer", "integer:7")]
    [Arguments("'1' cast as xs:boolean", "boolean:true")]
    [Arguments("'0' cast as xs:boolean", "boolean:false")]
    [Arguments("' true ' cast as xs:boolean", "boolean:true")]
    [Arguments("'false' cast as xs:boolean", "boolean:false")]
    public async Task FromString_ProducesTheValue(string expression, string expected) =>
        await Assert.That(EvaluateRendered(expression)).IsEqualTo(expected);

    // xs:untypedAtomic source behaves like xs:string.
    [Test]
    [Arguments("xs:untypedAtomic(' a ') cast as xs:string", "string: a ")]
    [Arguments("xs:untypedAtomic(' a ') cast as xs:untypedAtomic", "untyped: a ")]
    [Arguments("xs:untypedAtomic('1.5') cast as xs:float", "float:1.5")]
    [Arguments("xs:untypedAtomic('1.5') cast as xs:double", "double:1.5")]
    [Arguments("xs:untypedAtomic('1.5') cast as xs:decimal", "decimal:1.5")]
    [Arguments("xs:untypedAtomic(' 12 ') cast as xs:integer", "integer:12")]
    [Arguments("xs:untypedAtomic('true') cast as xs:boolean", "boolean:true")]
    public async Task FromUntypedAtomic_ProducesTheValue(string expression, string expected) =>
        await Assert.That(EvaluateRendered(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("xs:float('1.5') cast as xs:string", "string:1.5")]
    [Arguments("xs:float('1.5') cast as xs:untypedAtomic", "untyped:1.5")]
    [Arguments("xs:float('1.5') cast as xs:float", "float:1.5")]
    [Arguments("xs:float('0.1') cast as xs:double", "double:0.10000000149011612")]
    [Arguments("xs:float('1.5') cast as xs:decimal", "decimal:1.5")]
    [Arguments("xs:float('1.9') cast as xs:integer", "integer:1")]
    [Arguments("xs:float('-1.9') cast as xs:integer", "integer:-1")]
    [Arguments("xs:float('1.5') cast as xs:boolean", "boolean:true")]
    [Arguments("xs:float('0') cast as xs:boolean", "boolean:false")]
    [Arguments("xs:float('-0') cast as xs:boolean", "boolean:false")]
    [Arguments("xs:float('NaN') cast as xs:boolean", "boolean:false")]
    [Arguments("xs:float('INF') cast as xs:boolean", "boolean:true")]
    [Arguments("xs:float('-0') cast as xs:double", "double:-0")]
    [Arguments("xs:float('-0') cast as xs:decimal", "decimal:0")]
    [Arguments("xs:float('1e10') cast as xs:integer", "integer:10000000000")]
    public async Task FromFloat_ProducesTheValue(string expression, string expected) =>
        await Assert.That(EvaluateRendered(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("xs:double('1.5') cast as xs:string", "string:1.5")]
    [Arguments("xs:double('1.5') cast as xs:untypedAtomic", "untyped:1.5")]
    [Arguments("xs:double('1e300') cast as xs:float", "float:INF")]
    [Arguments("xs:double('-1e300') cast as xs:float", "float:-INF")]
    [Arguments("xs:double('1e-300') cast as xs:float", "float:0")]
    [Arguments("xs:double('0.1') cast as xs:float", "float:0.1")]
    [Arguments("xs:double('1.5') cast as xs:double", "double:1.5")]
    [Arguments("xs:double('1.5') cast as xs:decimal", "decimal:1.5")]
    [Arguments("xs:double('1.9') cast as xs:integer", "integer:1")]
    [Arguments("xs:double('-1.9') cast as xs:integer", "integer:-1")]
    [Arguments("xs:double('-0.5') cast as xs:integer", "integer:0")]
    [Arguments("xs:double('1.5') cast as xs:boolean", "boolean:true")]
    [Arguments("xs:double('0') cast as xs:boolean", "boolean:false")]
    [Arguments("xs:double('NaN') cast as xs:boolean", "boolean:false")]
    [Arguments("xs:double('-INF') cast as xs:boolean", "boolean:true")]
    [Arguments("xs:double('-9.223372036854775808E18') cast as xs:integer", "integer:-9223372036854775808")]
    [Arguments("xs:double('9.2233720368547748E18') cast as xs:integer", "integer:9223372036854774784")]
    public async Task FromDouble_ProducesTheValue(string expression, string expected) =>
        await Assert.That(EvaluateRendered(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("1.25 cast as xs:string", "string:1.25")]
    [Arguments("1.25 cast as xs:untypedAtomic", "untyped:1.25")]
    [Arguments("1.25 cast as xs:float", "float:1.25")]
    [Arguments("1.25 cast as xs:double", "double:1.25")]
    [Arguments("1.25 cast as xs:decimal", "decimal:1.25")]
    [Arguments("1.25 cast as xs:integer", "integer:1")]
    [Arguments("2.99 cast as xs:integer", "integer:2")]
    [Arguments("xs:decimal('-2.99') cast as xs:integer", "integer:-2")]
    [Arguments("xs:decimal('9223372036854775807') cast as xs:integer", "integer:9223372036854775807")]
    [Arguments("1.25 cast as xs:boolean", "boolean:true")]
    [Arguments("0.0 cast as xs:boolean", "boolean:false")]
    public async Task FromDecimal_ProducesTheValue(string expression, string expected) =>
        await Assert.That(EvaluateRendered(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("12 cast as xs:string", "string:12")]
    [Arguments("12 cast as xs:untypedAtomic", "untyped:12")]
    [Arguments("12 cast as xs:float", "float:12")]
    [Arguments("12 cast as xs:double", "double:12")]
    [Arguments("12 cast as xs:decimal", "decimal:12")]
    [Arguments("12 cast as xs:integer", "integer:12")]
    [Arguments("12 cast as xs:boolean", "boolean:true")]
    [Arguments("0 cast as xs:boolean", "boolean:false")]
    [Arguments("9007199254740993 cast as xs:double", "double:9007199254740992")]
    [Arguments("16777217 cast as xs:float", "float:16777216")]
    public async Task FromInteger_ProducesTheValue(string expression, string expected) =>
        await Assert.That(EvaluateRendered(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("xs:boolean('true') cast as xs:string", "string:true")]
    [Arguments("xs:boolean('false') cast as xs:string", "string:false")]
    [Arguments("xs:boolean('true') cast as xs:untypedAtomic", "untyped:true")]
    [Arguments("xs:boolean('true') cast as xs:float", "float:1")]
    [Arguments("xs:boolean('false') cast as xs:float", "float:0")]
    [Arguments("xs:boolean('true') cast as xs:double", "double:1")]
    [Arguments("xs:boolean('false') cast as xs:double", "double:0")]
    [Arguments("xs:boolean('true') cast as xs:decimal", "decimal:1")]
    [Arguments("xs:boolean('false') cast as xs:decimal", "decimal:0")]
    [Arguments("xs:boolean('true') cast as xs:integer", "integer:1")]
    [Arguments("xs:boolean('false') cast as xs:integer", "integer:0")]
    [Arguments("xs:boolean('true') cast as xs:boolean", "boolean:true")]
    public async Task FromBoolean_ProducesTheValue(string expression, string expected) =>
        await Assert.That(EvaluateRendered(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("() cast as xs:integer?", "count:0")]
    [Arguments("'1' cast as xs:integer?", "integer:1")]
    [Arguments("(1) cast as xs:string?", "string:1")]
    public async Task OptionalType_AcceptsTheEmptySequence(string expression, string expected) =>
        await Assert.That(EvaluateRendered(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("12 cast as xs:string", "xs:string(12)")]
    [Arguments("'1.5' cast as xs:double", "xs:double('1.5')")]
    [Arguments("1.25 cast as xs:integer", "xs:integer(1.25)")]
    public async Task CastExpression_AndConstructorFunction_GiveTheSameResult(string cast, string constructor) =>
        await Assert.That(EvaluateRendered(constructor)).IsEqualTo(EvaluateRendered(cast));
}
