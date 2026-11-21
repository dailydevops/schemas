# Pipeline

The composition of the stages that turn a Schematron schema into a validator (SCH-55, #311; design #137 AP-26 and #150 EP-14). This is the
first point where Schematron validation runs end to end on the Xml and XPath engines.

```text
source ──> Parse ──> Resolve ──> Expand ──> Compile ──> CompiledProgram        (Build, BuildAsync)
           tree        include     abstract   binding      │
           xi:include  extends     rules and  phase        ▼
           model       library     patterns   scopes     ValidationRun          (Validate, ValidateAsync)
           edition                            rules       tree ─> evaluate ─> events ─> issues and caller sink
```

| File                          | Role                                                                                                         |
| ----------------------------- | ------------------------------------------------------------------------------------------------------------ |
| `SchematronPipeline.cs`       | `ISchematronPipeline`: runs the four stages in order; a stage with problems stops the later ones.            |
| `SchemaSourceReader.cs`       | Reads a stream source (sync or async), bounded by `MaxResourceSize`, and keeps its bytes for a second build. |
| `SchematronModelParser.cs`    | Stage Parse: Xml tree, `xi:include`, object model, edition.                                                  |
| `ModelExpander.cs`            | Stage Expand: abstract rules, then abstract patterns.                                                        |
| `SchematronSchemaCompiler.cs` | Stage Compile: binding, parameters, phase, scopes, rule sets.                                                |
| `PipelineBindings.cs`         | The registry of a build: the bindings with the XSLT 2.0 functions for the XSLT family.                       |
| `CompiledProgram.cs`          | The immutable result of the build; read by every run.                                                        |
| `ValidationRun.cs`            | One validation call: instance tree, run of the binding, evaluation, events.                                  |
| `InstanceReader.cs`           | Reads an instance through the loader of the Xml package, for every input kind.                               |
| `XmlIssues.cs`                | Reader options from `ValidationLimits`, and the issue of a failed read.                                      |
| `BuildProblems.cs`            | The problems of a build as the issues of one `SchemaCompilationException`.                                   |

The instance is read only through the readers of the Xml package (an architecture test pins it), so there is no second parse path; a
`Stream` is read into a pooled buffer by the base of the validator.

## Limitations

- **XSLT declarations are not compiled.** `xsl:function`, `xsl:key`, `xsl:variable`, `xsl:param`, `xsl:decimal-format`, `xsl:include`, and
  `xsl:import` in a schema of the XSLT family are reported as `NES3204` at `Build`; the modules are not linked and compiled (#832). The
  functions `current`, `system-property`, `function-available`, `format-number`, and `generate-id` are available; `key`, `regex-group`, and
  stylesheet functions are not (#250, #287, #628). A run starts with an empty set of decimal formats, so a named format is unknown.
- **Locations of expression problems are approximate.** The binding seam hands the binding the text of an expression but not the location of
  the construct that holds it, so a problem is located by the base URI of the schema and by the line and column within the expression text;
  a dynamic problem has no line and column.
- **The schema is bounded by `MaxResourceSize`** (provisional, design #137 OQ-6; the alternative is `MaxInputSize`), and the Xml reader
  keeps its default limits for the schema; the nesting limit applies after inclusion.
- **No compile-once cache and no thread-safety proof** (SCH-71, #340); every `Build` compiles the schema, and a test pins concurrent
  validations on one validator.
- **`last()` in a predicate without a known size** equals `position()`.
