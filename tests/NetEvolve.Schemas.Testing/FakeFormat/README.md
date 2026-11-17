# FakeFormat

Test-only format that shows the shape of every format entry point.

## Folder convention

Each real format adds exactly one folder next to this one and keeps its entry point in a single public static extension class named `<Format>SchemaValidatorExtensions` in the namespace `NetEvolve.Schemas` (design #103 AP-2):

- `Dtd/`
- `Schematron/`
- `Xsd/`

## Limitations

- With a compiler older than C# 14, `SchemaValidator.For<Format>` is not available; callers use the named extension class.
- The fake validator returns `ValidationResult.Valid` for every call and records the call for assertions.
