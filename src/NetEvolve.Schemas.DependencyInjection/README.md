# NetEvolve.Schemas.DependencyInjection

Registers schema validators of the NetEvolve schema libraries with `Microsoft.Extensions.DependencyInjection`, plain or keyed, as lazily built singletons.

The package is the generic registration core. It depends on `NetEvolve.Schemas` and `Microsoft.Extensions.DependencyInjection.Abstractions` only, and references no format package. It adds two extension methods on `IServiceCollection`, declared in the namespace `Microsoft.Extensions.DependencyInjection`:

| Method | Registers |
| --- | --- |
| `AddSchemaValidator<TValidator>(factory)` | One validator of type `TValidator`. |
| `AddKeyedSchemaValidator<TValidator>(serviceKey, factory)` | One validator of type `TValidator` per service key. |

`TValidator` must be a class that implements `ISchemaValidator`, and it is also the registered service type. To resolve the interface, register it as `ISchemaValidator`. A keyed registration of a validator type other than `ISchemaValidator` also registers `ISchemaValidator` under the same key, and that registration forwards to the validator.

The registrations of a format ship with the package of that format, in the same namespace and built on the core above: `AddXsdValidator` and `AddKeyedXsdValidator` in `NetEvolve.Schemas.Xsd`, `AddSchematronValidator` and `AddKeyedSchematronValidator` in `NetEvolve.Schemas.Schematron`, and `AddDtdValidator` and `AddKeyedDtdValidator` in `NetEvolve.Schemas.Xml`. Reference the format package together with this package and the calls resolve without a further using directive. Referencing one format never brings another format's assembly.

The samples in this readme use `ForFake()`, a placeholder for the entry point of a real format. They need these namespaces:

```
using Microsoft.Extensions.DependencyInjection;
using NetEvolve.Schemas;
```

## Plain registration

The factory receives the `IServiceProvider` and returns the validator. Nothing is built at registration: the factory runs on the first resolve, and every later resolve returns the same singleton instance.

```csharp
var builds = 0;
var services = new ServiceCollection();
services.AddSchemaValidator<ISchemaValidator>(_ =>
{
    builds++;
    return SchemaValidator.ForFake().Build();
});

using var provider = services.BuildServiceProvider();
var notBuiltYet = builds == 0;

var first = provider.GetRequiredService<ISchemaValidator>();
var second = provider.GetRequiredService<ISchemaValidator>();
```

Here `notBuiltYet` is `true`, `builds` is `1`, and `second` is the same instance as `first`.

## Keyed registration

A key names exactly one validator, so several differently configured validators can live side by side. The factory receives the provider and the key.

```csharp
var services = new ServiceCollection();
services.AddKeyedSchemaValidator<ISchemaValidator>("strict", (_, _) =>
    SchemaValidator.ForFake().WithLimits(ValidationLimits.Default.WithMaxDepth(16)).Build()
);
services.AddKeyedSchemaValidator<ISchemaValidator>("default", (_, _) => SchemaValidator.ForFake().Build());

using var provider = services.BuildServiceProvider();
var strict = provider.GetRequiredKeyedService<ISchemaValidator>("strict");
var other = provider.GetRequiredKeyedService<ISchemaValidator>("default");
```

Here `strict` and `other` are different singletons, and a repeated lookup of the same key returns the same instance. A lookup without a key is independent of keyed registrations and finds nothing.

## Behavior

- **Lifetime.** Every registered validator is a singleton. Validators are immutable and thread-safe, so sharing one is safe.
- **Lazy compilation.** The factory builds the validator on the first resolve. A failure such as `SchemaCompilationException` surfaces unwrapped from that resolve and is not cached, so the next resolve runs the factory again.
- **Repeated registration.** Every method has `TryAdd` semantics. A repeated registration for the same service type (and key) is a silent no-op, and its factory is never invoked.

## Limitations

- **Keyed registration needs a keyed container.** The container must implement `IKeyedServiceProvider` (`Microsoft.Extensions.DependencyInjection` 8.0 or later). There is no fallback for other containers.
- **No wildcard key.** `KeyedService.AnyKey` is a resolution wildcard and is rejected as a key with an `ArgumentException`. A `null` key is rejected as well.
- **Placeholder samples.** The samples use a placeholder format because they exercise the registration only.
