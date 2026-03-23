# Mindbox.Data.Linq

A fork of Microsoft's `System.Data.Linq` (LINQ to SQL) that enables multi-assembly extensibility and compatibility with modern .NET.

## Why this fork?

The original `System.Data.Linq` ships as a single sealed assembly. This fork splits the internals across multiple DLLs, making it possible to extend the query pipeline — custom SQL translators, mapping providers, and diagnostics hooks — without reflection hacks.

## Target framework

`net8.0`

## Installation

```
dotnet add package Mindbox.Data.Linq
```

## Usage

Drop-in replacement for `System.Data.Linq`. Replace the namespace import and use `DataContext` as usual:

```csharp
using System.Data.Linq;

using var context = new DataContext(connectionString, mappingSource);
var results = context.GetTable<Order>()
    .Where(o => o.CustomerId == customerId)
    .ToList();
```

## Key differences from System.Data.Linq

| Feature | System.Data.Linq | Mindbox.Data.Linq |
|---|---|---|
| Multi-assembly extensibility | ✗ | ✓ |
| .NET 10 `array.Contains()` in queries | ✗ | ✓ |
| Target framework | netstandard2.0 | net8.0 |

## Building

```bash
dotnet restore
dotnet build --configuration Release
dotnet test
```

## License

MIT — see [LICENSE.txt](LICENSE.txt).
