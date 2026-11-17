namespace NetEvolve.Schemas.Xml.Tests.Unit.Writer;

/// <summary>An attribute for the script helpers.</summary>
internal readonly record struct Att(string Local, string Value, string Prefix = "", string Uri = "");
