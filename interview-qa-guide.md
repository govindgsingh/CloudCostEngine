# Cloud Storage Cost Engine — Interview Q&A Guide

This is a walkthrough you can use to explain (and defend) every decision in this solution.
It's written so a beginner can follow it end to end — each answer starts with the plain-English
idea before it gets technical, and every "why this, not that" question also covers the road not
taken.

---

## Part 1 — The big picture

### Q: In one sentence, what does this program do?

It reads a list of cloud pricing plans from a JSON file, and for a given amount of storage
(in GB), tells you the total monthly cost — tax included — of every plan, cheapest first.

### Q: Walk me through what happens when I type `monthly_cost 1200`.

1. The CLI (`CommandLoop`) reads the line, splits it into `monthly_cost` and `1200`.
2. It looks up the `MonthlyCostCommand` and hands it the argument.
3. The command parses `1200` into a number and, for **each** loaded plan, asks a
   `MonthlyCostCalculator` to price it.
4. The calculator: divides storage by 10 to get compute hours (§3.1 of the spec), prices
   storage and compute separately through the shared tiered-pricing math, adds the base
   fee, multiplies the whole thing by 1.10 for tax, and rounds to the nearest cent.
5. The command sorts the results by price and prints one line per plan.

### Q: Why did you split this into four projects instead of one?

Because the four projects represent four different *kinds* of code that change for different
*reasons* — and separating them by "reason to change" is the whole point of layered
architecture:

| Project | What lives here | Changes when... |
|---|---|---|
| `Domain` | The core business concepts: a plan, a pricing tier | The pricing *rules* change |
| `Application` | The use cases: "calculate a monthly cost," "validate a plan" | We add a new feature (e.g. budget optimization) |
| `Infrastructure` | JSON file reading | We change *where data comes from* (a database, an API) |
| `Cli` | The console loop, wiring everything together | We change *how the user interacts* with it (add a GUI, a web API) |

If everything were in one project, a change to "how we read JSON" could accidentally touch
"how we calculate cost," because nothing stops one class from reaching into another. Splitting
them into projects makes that a **compiler error**, not just a code-review nitpick — Cli can't
accidentally start doing math, because it can't even see `TieredPricingCalculator` unless it
references `Application`.

### Q: Isn't that over-engineered for a 1-2 hour take-home?

Fair challenge, and worth answering head-on rather than dodging it. Two things justify it here:

1. **The brief itself says the interview will extend this program** ("adding budget-based
   optimization... structure it for change"). That's a direct signal that the *structure* is
   being evaluated, not just whether today's two commands work.
2. The extra structure cost very little — four small projects, one interface per seam. It's
   not hexagonal-architecture-with-CQRS-and-event-sourcing; it's the minimum layering needed
   to keep "business rules" and "I/O" from tangling together. A single-project solution would
   have been *faster* to write, but slower to extend live in an interview, which is exactly
   the moment this needs to be fast.

---

## Part 2 — Tech stack justification

### Q: Why C# / .NET 8, and not something else?

The spec mandates it (§10: "target C# / .NET (.NET 8+ LTS)"), so this isn't really a choice —
but here's why it's a *good* fit even without that constraint:

- **`decimal` type.** Money math must never use binary floating point (`double`/`float`),
  because values like 0.1 can't be represented exactly in binary, and errors compound across
  many additions. C#'s `decimal` is a base-10 floating-point type built for exactly this —
  it's the standard choice for financial calculations in .NET, the way `BigDecimal` is in Java
  or `Decimal` is in Python.
- **Strong typing + nullable reference types.** Enabling `<Nullable>enable</Nullable>` means
  the compiler forces you to think about "can this be missing?" at every step — which matters
  a lot when the whole point of §6.1 is validating messy external JSON input.
- **`System.Text.Json` is built in.** No third-party dependency needed just to parse a file.

### Q: Why not Java or Python or Node for this kind of problem?

All three are perfectly capable of solving it — the reason to *not* use them here is purely
the spec's requirement, not a technical limitation. If I were to compare on technical merits
alone:

- **Python** would read as more concise but has no compile-time type safety, so a typo in a
  field name (`theshold` vs `threshold`) would surface as a runtime bug instead of a build
  error — risky for pricing logic where a silent bug means a customer is billed wrong.
- **Java** is architecturally very similar to C# (same OOP model, same layering options), so
  this design would translate almost 1:1. The main difference is verbosity and JVM startup
  time for a CLI tool.
- **Node/TypeScript** has no native decimal type — you'd need a third-party library (e.g.
  `decimal.js`) just to get correct money math, which C# gives you for free.

### Q: Why `System.Text.Json` instead of Newtonsoft.Json (Json.NET)?

`System.Text.Json` has shipped in the box since .NET Core 3.0 and is now the default choice
for new projects: no extra NuGet dependency, better performance, and it covers everything this
schema needs (attribute-based property mapping, case-insensitive matching). Newtonsoft.Json is
still common in older/legacy codebases and has a few extra features (e.g. more flexible custom
converters), but pulling in a third-party package for a straightforward DTO mapping would be
adding a dependency with no real benefit here.

### Q: Why xUnit instead of MSTest or NUnit?

All three would work fine — this is genuinely a coin-flip decision in most C# shops. xUnit was
picked because it's the modern .NET default (it's what `dotnet new` scaffolds, it's used
throughout the ASP.NET Core codebase itself), has a slightly cleaner API (`[Fact]`/`[Theory]`
vs MSTest's `[TestMethod]`/`[DataRow]`), and creates a fresh test class instance per test by
default, which avoids a whole class of shared-state bugs between tests.

### Q: Why no dependency injection container (no `Microsoft.Extensions.DependencyInjection`)?

Because the entire object graph is **five objects**: a repository, a calculator, three
commands. `Program.cs` constructs them by hand with `new`:

```csharp
var repository = new JsonPlanRepository();
var calculator = new MonthlyCostCalculator();
var context = new CliContext(repository, calculator);
```

A DI container earns its cost when you have dozens of services with complex, overlapping
lifetimes (web request scopes, singletons, background services). Here it would add a NuGet
dependency, a `ServiceCollection` setup block, and a layer of indirection — for zero actual
benefit, because there's nothing complex to manage. This is a real trade-off, not just "less
code is always better": if this program grew into something with 20+ services, I'd reach for
DI immediately. The interfaces (`ICommand`, `IPlanRepository`, `ICostCalculator`) are exactly
the same either way — swapping in a container later, if the interview extension needs it,
would be a five-minute change to `Program.cs` and nothing else, precisely *because* the rest of
the code already depends on interfaces, not concrete classes.

### Q: Why manual composition instead of using the .NET `Generic Host` (`IHostBuilder`)?

The Generic Host is designed for long-running services with things like configuration
binding, structured logging providers, and graceful shutdown handling across multiple
background services. This is a single-threaded interactive CLI reading from stdin — `Host`
would add ceremony (configuration files, logging providers) that nothing in the spec asks for.
If this were evolving into a web API or a service with multiple background jobs, `Host` would
be the right call.

---

## Part 3 — Domain modelling & design patterns

### Q: Walk me through the domain model.

- **`PricingTier`** — one marginal-rate band: a rate and an optional threshold. It's a
  `record` because tiers are pure data with value equality — two tiers with the same rate and
  threshold *are* the same tier, there's no identity beyond their values.
- **`PricingSchedule`** — an ordered, *already-validated* list of tiers. You can't construct
  one that breaks the rules in §3.2 (missing thresholds on non-final tiers, decreasing
  thresholds, etc.) — the `Create` factory method checks every rule and throws a clear message
  the moment it's violated. Once you're holding a `PricingSchedule`, you know it's correct;
  nothing downstream has to re-check it.
- **`CloudPlan`** — the aggregate root: a provider name, plan name, base fee, and two
  `PricingSchedule`s (storage, compute).

This is the core idea of **"make invalid states unrepresentable."** Rather than a `CloudPlan`
that *might* have bad data and needs checking before every use, we make it structurally
impossible to end up with an invalid one in the first place.

### Q: Why is the Storage/Compute coupling (§3.1) *not* stored on `CloudPlan`?

Because it's not a property of a plan — it's a rule about how to *use* a plan (derive compute
hours from storage). It lives as a single line in `MonthlyCostCalculator`
(`storageGb / 10`). Putting business *rules* in the Application layer and business *data* in
the Domain layer is exactly the separation the four-project structure is protecting.

### Q: What design patterns did you use, and why each one specifically (not just "because patterns are good")?

| Pattern | Where | The actual problem it solves here |
|---|---|---|
| **Strategy** | `ICostCalculator` | The spec's own "what's next" hint is budget-based optimization — a *second* way to evaluate a plan (find max storage under a budget, instead of cost for a given storage). Strategy means that's a new class implementing the same interface, not a rewrite of `MonthlyCostCalculator`. |
| **Factory** | `PlanFactory.TryBuild` | Building a `CloudPlan` from untrusted JSON isn't a simple constructor call — it involves collecting *multiple* independent validation errors and reporting all of them at once. That branching logic doesn't belong inside `CloudPlan`'s constructor (which should be able to assume its inputs are already good). |
| **Repository** | `IPlanRepository` / `JsonPlanRepository` | Everything above Infrastructure asks for plans through an interface and has no idea they come from a JSON file. Swap to a database later, and only `Infrastructure` changes. |
| **Command** | `ICommand` / `InputCommand`, `MonthlyCostCommand`, `ExitCommand` | Each CLI verb is self-contained: its own argument parsing, its own error messages. Adding a `budget` command at the pairing session means writing one new class and adding it to the array in `Program.cs` — nothing else changes. |
| **Value Object** | `PricingTier`, `CostResult` (both `record` types) | These are compared and passed around by value, never mutated after creation — which eliminates a whole class of "who changed this and when" bugs. |

### Q: Why is `PricingSchedule.Create` a static factory method instead of a constructor?

Two reasons:
1. **A constructor should not fail for "expected" reasons.** If invalid tier data is a normal,
   anticipated situation (which it is — we're parsing external JSON), a named method that can
   fail is clearer intent than a constructor throwing exceptions the caller may not expect.
2. It also reads better at the call site: `PricingSchedule.Create(tiers, "storage_prices")`
   tells you *what* you're validating, which becomes part of the error message.

### Q: How does storage vs. compute pricing avoid duplicated logic, given they're separately specified in the spec?

Spec §3.2 describes storage and compute pricing with *identical structure* — tiers, thresholds,
marginal charging — just applied to two different quantities. Rather than writing
`CalculateStorageCost` and `CalculateComputeCost` as two near-identical methods (which would
inevitably drift apart over time as one gets a bug fix the other doesn't), there's a single
`TieredPricingCalculator.CalculateCost(quantity, schedule)` used for both. This is the
**Don't Repeat Yourself (DRY)** principle applied at the right level: the *duplication* here
was in the domain rule, not just the code, so removing it structurally (one method, two
call sites) is more robust than remembering to keep two methods in sync.

### Q: Why round only once, at the very end, instead of rounding storage cost and compute cost separately?

Rounding early and re-summing rounded parts is a classic source of off-by-a-cent bugs (it's
why retail receipts sometimes show a total that looks "wrong" by a cent versus adding the
lines yourself). The spec's worked example only defines *one* rounding point — the final
dollar total — so that's the only place rounding happens. Everything before that
(`decimal`, unrounded) carries full precision.

### Q: Why `MidpointRounding.AwayFromZero` and not the .NET default?

.NET's `Math.Round` defaults to **banker's rounding** (round-half-to-even — 2.5 rounds to 2,
3.5 rounds to 4), which reduces statistical bias across *many* roundings but is not what most
people mean by "round half up." Spec §6.2 explicitly says "round half up," so
`MidpointRounding.AwayFromZero` was chosen deliberately — for non-negative numbers (which
money always is here), "away from zero" and "round half up" are the same thing.

---

## Part 4 — Validation & error handling

### Q: How does the program avoid crashing on bad input, per §6.1?

Three layers, each catching a different *kind* of problem:

1. **File/JSON level** (`JsonPlanRepository`): missing file, permission errors, malformed
   JSON — all caught and turned into a clear message via `PlanLoadResult.Failed(...)`.
2. **Field level** (`PlanFactory`): missing required fields, empty tier arrays — collected as
   a list of human-readable strings, *all* of them, not just the first one found.
3. **Domain rule level** (`PricingSchedule.Create`): a non-final tier missing its threshold,
   decreasing thresholds, negative rates.

None of these ever let an exception escape uncaught to crash the process. `CommandLoop` also
has one final `try/catch` around every command as a safety net — not the primary error-handling
mechanism, but insurance against something truly unexpected.

### Q: Why report *all* validation errors at once instead of stopping at the first one?

Imagine fixing a malformed JSON file one error at a time, re-running the program after each
fix, and discovering a *new* error each time — that's a frustrating debug loop. Collecting
every problem in one pass (missing provider name **and** empty storage tiers **and** a
negative base fee, all reported together) means you fix the file once.

### Q: What happens if one plan in the file is broken but the others are fine?

The good plans still load. `PlanLoadResult` separates **fatal errors** (the file itself
couldn't be parsed — nothing loads) from **plan warnings** (one entry was invalid — it's
skipped, everything else loads normally, and a warning is printed explaining why). This
mirrors how a real data-import pipeline should behave: one bad row shouldn't sink an entire
batch.

### Q: How would you test the "missing threshold on a non-final tier" edge case, specifically?

There's a dedicated test for exactly this
(`PlanFactoryTests.NonFinalTierMissingThreshold_ProducesValidationError`): build a
`RawPlanInput` where the *first* of two storage tiers has `Threshold = null`, call
`PlanFactory.TryBuild`, and assert the returned plan is `null` and the error list mentions
both `storage_prices` and `threshold`.

---

## Part 5 — Testing strategy

### Q: What's your overall testing philosophy here?

Test *behavior*, not *implementation* — each test asserts an outcome a domain expert would
recognize as correct (a specific dollar total, a specific validation message), not internal
call counts or private state. Four test classes map directly onto the four kinds of risk in
this program:

- `TieredPricingCalculatorTests` — the marginal-tier math itself, isolated from everything else
- `MonthlyCostCalculatorTests` — the full pipeline, anchored on the spec's own worked example
- `PlanFactoryTests` — every validation rule from §6.1, one test per rule
- `JsonPlanRepositoryTests` — file/JSON-level failure modes, plus the partial-load behavior

### Q: Why is the spec's worked example ($64.35 for 1,200 GB on NimbusCloud) its own dedicated test?

Because it's the one number in the entire spec that's independently verifiable — if this test
passes, it's strong evidence the core formula (tiering → base fee → 10% tax → round-half-up)
is implemented correctly end to end, not just correct in isolated pieces.

### Q: What edge cases did you specifically think to test, beyond the happy path?

- Zero storage (should still charge the base fee plus tax, and derive 0 compute hours)
- Consumption landing *exactly* on a tier threshold (must not spill into the next tier)
- Fractional storage (1,205 GB → 120.5 compute hours, per the spec's own example)
- Negative storage input (should be rejected, not silently treated as zero)
- A missing file, and malformed JSON (should report clearly, not throw)
- One invalid plan mixed in with valid ones (only the bad one should be skipped)

### Q: What would you add next if the interviewer said "keep going"?

- **Property-based tests** for the tiered calculator (e.g. using FsCheck) — asserting that for
  *any* valid schedule and quantity, cost is monotonically non-decreasing as quantity
  increases, and that summing tier-by-tier always equals the total.
- **Golden-file tests**: feed the exact JSON from the spec's §4 example through the CLI itself
  (not just the calculator) and assert on stdout, to catch formatting regressions.
- Tests for the CLI command loop itself (e.g. `monthly_cost` before `input` prints the right
  message) using an injected `TextReader`/`TextWriter` instead of `Console` directly.

---

## Part 6 — Extending it live (the interview pairing session)

### Q: The spec says you'll pair on adding budget-based optimization. How would that fit into this structure?

1. Add `ICostCalculator`'s sibling, e.g. `IBudgetOptimizer` with a method like
   `FindMaxStorage(CloudPlan plan, decimal budgetDollars)` in `Application/Interfaces`.
2. Implement it in `Application/Services` — likely a binary search or closed-form inversion of
   `MonthlyCostCalculator`'s math, reusing `TieredPricingCalculator` as-is.
3. Add a `BudgetCommand : ICommand` in `Cli/Commands` for a new verb, e.g. `budget <dollars>`.
4. Register it in the `commands` array in `Program.cs`.

No existing class needs to change — this is the Open/Closed Principle in action ("open for
extension, closed for modification"), and it's the direct payoff of the layering discussed in
Part 1.

### Q: If this had to become a web API instead of a CLI, what would change?

Only the `Cli` project would be replaced (with, say, an ASP.NET Core minimal API project).
`Domain`, `Application`, and `Infrastructure` would be untouched — they have no idea whether
they're being driven by a console loop or an HTTP endpoint. That's the practical payoff of
"Cli depends on everything, nothing depends on Cli."

### Q: What's the biggest weakness in this design, if you're being self-critical?

Two honest ones:

1. **No persistence/caching** — every `input` command re-reads and re-parses the whole file.
   Fine for a CLI take-home; would need addressing for a long-running service with large plan
   catalogs.
2. **`PlanFactory`'s error collection is "mostly" complete, not fully complete** — if a plan is
   missing *both* its storage tiers array and has a malformed tier deeper in a valid array,
   only the first problem in that particular field is surfaced (the `for` loop over tiers
   returns on the first missing `rate`). That's a reasonable trade-off for readability, but a
   more exhaustive validator would collect every tier-level problem, not just the first per
   field.
