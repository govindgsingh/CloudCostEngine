# Cloud Storage Cost Engine

A CLI that loads cloud provider pricing plans from JSON and reports the total monthly cost
of a given storage usage across all of them, cheapest first.

## Build & run

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download).

```bash
# from the repository root
dotnet build
dotnet run --project src/CloudCostEngine.Cli
```

Then, at the `>` prompt:

```
> input sample-data/cloud_plans.json
Loaded 3 plan(s) from 'sample-data/cloud_plans.json'.
> monthly_cost 1200
OrbitalStorage, TieredSaver, $53.90
StratoByte, PayAsYouGo, $62.04
NimbusCloud, EnterpriseHybrid, $64.35
> exit
```

## Run the tests

```bash
dotnet test
```

## Project layout

```
src/
  CloudCostEngine.Domain/         entities & invariants — no dependencies on anything
  CloudCostEngine.Application/    pricing math, validation, use cases — depends on Domain only
  CloudCostEngine.Infrastructure/ JSON file I/O — depends on Application
  CloudCostEngine.Cli/            console loop & composition root — depends on Infrastructure
tests/
  CloudCostEngine.Tests/          xUnit tests for the layers above
sample-data/
  cloud_plans.json                example input file, includes the spec's worked example
```

Dependencies only point inward (Cli → Infrastructure → Application → Domain). See
`interview-qa-guide.md` for the full reasoning behind this structure, the design patterns
used, and the alternatives that were considered and rejected.

## Assumptions made where the spec was silent

1. **Last-tier threshold, if present, is ignored.** Spec §3.2 says the last tier "has no
   threshold and applies without limit." If an input file's last tier does include a
   threshold anyway, it's treated as unlimited rather than rejected — malformed-but-usable
   data shouldn't block an otherwise valid plan. This is documented in `PricingSchedule`.
2. **`base_fee` is required, not merely defaulted to 0.** The schema doesn't list it in
   `required`, but the pricing formula in §3.3 has no defined behavior without it, so a
   missing value is treated as a validation error rather than silently assumed to be zero.
3. **Partial-file loading.** If a JSON file parses but contains one invalid plan among
   several valid ones, the valid plans are still loaded and a warning is printed for the
   invalid one, rather than rejecting the whole file. This seemed more useful for a
   business comparing many providers than an all-or-nothing failure.
4. **Rounding.** "Round half up" (§6.2) is implemented as `MidpointRounding.AwayFromZero`
   applied once, to the final dollar total — not to intermediate cent amounts — since the
   spec's worked example only defines the final rounding point.
5. **CLI argument parsing.** `input` re-joins all text after the command word as the file
   path (so Windows paths with spaces work); `monthly_cost` requires exactly one numeric
   argument and rejects negative values with a message rather than crashing.
6. **Money type.** All monetary values use `decimal`, never `double`/`float`, to avoid
   binary floating-point rounding error in currency math — standard practice in .NET for
   money.
