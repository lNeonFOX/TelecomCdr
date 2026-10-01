# Assignment 2: Multithreaded Telecom Call Processing Pipeline

Course: IFP — Introduction to Functional Programming, Astana IT University
Platform: C# 12 / .NET (targeted at net10.0 SDK; language features used are net8.0-compatible)
Student: Torekhan Taimas

## 1. Build, run, test

```
dotnet build [TelecomCdr folder]
dotnet run --project src/TelecomCdr
dotnet test
```

## 2. Architecture

| File | Role |
|---|---|
| `src/TelecomCdr/CallRecord.cs` | Immutable `readonly record struct` with an explicit validating constructor |
| `src/TelecomCdr/CallPricing.cs` | `CalculateCost`: one switch expression, pure |
| `src/TelecomCdr/CallProcessing.cs` | `ProcessCallsSequential` (baseline) and `ProcessCallsParallel` (two raw threads) |
| `src/TelecomCdr/Program.cs` | Console demo (the only place with console I/O) |
| `tests/TelecomCdr.Tests` | xUnit tests: `TariffTests.cs` (Group A), `ValidationTests.cs` (Group B), `PipelineTests.cs` (Group C) |

### Immutability and validity (Task 1)

`CallRecord` is declared without positional syntax so that the constructor is written by hand and
validates every argument: blank/null/whitespace `RecordId` or `DestinationCountry`, and negative,
NaN, infinite or greater-than-10,000 `DurationMinutes` all throw `ArgumentException`. Properties are
get-only, so neither `with` expressions nor object initializers can produce an invalid instance after
construction.

`readonly` only guarantees that fields cannot change *after* construction — it does not guarantee the
constructor ever ran. `default(CallRecord)` (and any `new CallRecord[n]` array before it is filled)
produces an instance with null strings and a zero duration without calling the constructor at all.
For this reason, `CalculateCost` re-validates its input rather than trusting that every `CallRecord`
it receives is valid.

### Pricing (Task 2)

`CalculateCost` is implemented as a single switch expression, evaluated top to bottom, first match wins:

1. Blank `RecordId` → throw.
2. Blank `DestinationCountry` → throw.
3. `DurationMinutes` is `NaN` → throw (explicit defensive arm).
4. `DurationMinutes` is negative, infinite, or greater than 10,000 → throw.
5. Roaming + `KZ` + duration `< 1.0` → flat 50.00.
6. Non-roaming + `KZ` → 15.00 per minute.
7. Roaming + duration `>= 10.0` → 120.00 per minute.
8. `_` fallback (everything else valid) → 45.00 per minute.

The `double` → `decimal` cast appears only in arms 5–8, which are reachable only after every validation
arm has failed to match — i.e. only for finite, in-range values. This ordering matters because
`(decimal)double.NaN` and `(decimal)double.PositiveInfinity` throw `OverflowException`, the wrong
exception type, which would hide the real validation failure. `NaN` needs its own explicit arm because
every comparison involving `NaN` evaluates to `false`, so the range check `< 0 or > 10000` alone would
silently let it through into the tariff arms.

Each call's cost is rounded once, at the end, with `Math.Round(rawCost, 2, MidpointRounding.AwayFromZero)`.

## 3. The race (Task 3)

`globalCallCounter++` is not a single atomic operation — it is three separate steps: read the current
value, add 1, write the result back. When two threads run this concurrently, their steps can interleave
and one update can be lost.

Example, starting from `globalCallCounter = 5`:

| Step | Thread A | Thread B | Value in memory |
|---|---|---|---|
| 1 | reads 5 | | 5 |
| 2 | | reads 5 | 5 |
| 3 | computes 5+1=6, writes 6 | | 6 |
| 4 | | computes 5+1=6, writes 6 | 6 |

Both threads processed one call each (two calls total), but the counter only advanced by 1 instead of 2 —
one increment is lost.

A single sequential loop does not have this problem: there is only one thread, so there is no possible
interleaving with itself. The race only appears once two separate threads share and mutate the same
variable concurrently. This is exactly why `ProcessCallsParallel` (Task 4) avoids a shared counter
entirely and gives each worker its own isolated output array instead.

## 4. Partitioned pipeline (Task 4)

- Null input throws `ArgumentNullException`; an odd-length array throws `ArgumentException`; an empty
  array returns 0 without creating any threads.
- The input is split with `records[..mid]` and `records[mid..]` — C# array range syntax. Each expression
  produces a new array (a copy), not a view into the original, so both workers only ever touch their own
  data.
- Two independent `decimal[]` output arrays are allocated before either thread is started.
- Exactly two `new Thread(...)` instances are created. Each is wrapped around a private `Worker` instance
  that reads only its own input array and writes only its own output array.
- `Start()` is called on both threads, then `Join()` is called on both, and only after both joins does the
  method read anything from the output arrays.
- Each worker catches its own exception into a private `Error` field rather than letting it escape on a
  background thread (which would crash the process unhandled). After both joins: if both workers failed,
  the two exceptions are combined into an `AggregateException`; if exactly one failed, it is rethrown on
  the calling thread via `ExceptionDispatchInfo.Capture(...).Throw()`, which preserves the original
  exception type and stack trace. Totals are never read or returned if any worker failed.
- No `lock`, `Interlocked`, `Task`, or PLINQ is used anywhere in the pipeline.

**Why the isolated arrays are safe:** no memory location is ever written by more than one thread, and no
location is written by one thread while another thread reads it — each worker owns a disjoint slice of the
input and a disjoint output array. `Join()` guarantees that the worker thread has finished executing and
that its writes are visible to the calling thread before the totals are summed (a happens-before
relationship). Since every individual cost is already rounded to 2 decimal places before it is added,
decimal addition of the totals is exact and the order of summation cannot change the result.

## 5. Pure versus impure components

| Component | Pure? | Why |
|---|---|---|
| `CalculateCost` | Pure | Output depends only on its argument; no I/O, no globals, no mutation, deterministic |
| `CallRecord` | Immutable value | Cannot change state after construction (though `default` can still bypass validation) |
| `ProcessCallsSequential` | Pure in effect | Uses only a local accumulator; same input always gives the same total |
| Thread creation, `Start`/`Join`, writes into the output arrays | Impure | Side effects (thread lifecycle, array mutation), but isolated per worker so no shared state is affected |
| `Console.WriteLine` in `Program.cs` | Impure | I/O, deliberately kept out of the pricing/processing core |

Immutability, purity and synchronization solve different problems: immutability removes the need to
synchronize *reads* of shared data (an immutable value can never be observed mid-change); purity makes a
function safe to call from any thread without coordination; synchronization (locks, atomics) is only
needed when multiple threads share *mutable* state — a situation this design avoids entirely by giving
each worker its own private input slice and output array. Raw threads are not claimed to be faster than
the sequential baseline; see Limitations below.

## 6. Tests

- **Group A** (`TariffTests.cs`): the six tariff cases required by the spec (KZ non-roaming, KZ roaming
  under 1 min, US roaming 10 min, DE non-roaming, unknown country fallback, KZ roaming fallback), plus a
  rounding check (`AwayFromZero` on a `.5` boundary).
- **Group B** (`ValidationTests.cs`): constructor rejection of blank id/country and of negative, NaN,
  infinite, and out-of-range durations; rejection of `default(CallRecord)` by `CalculateCost`; zero-minute
  call costs (flat fee for roaming KZ, 0.00 otherwise); tariff boundaries at exactly 1.0 minute and exactly
  10.0 minutes.
- **Group C** (`PipelineTests.cs`): a deterministic 1,200-record dataset covering every tariff arm; the
  sequential total is computed once and the parallel processor is run 100 times, asserting exact decimal
  equality on every run; input records are asserted unchanged after processing; empty, odd-length, and
  null arrays are also tested.

### Test results

```
Restore completed.
  TelecomCdr        net10.0 succeeded → src\TelecomCdr\bin\Debug\net10.0\TelecomCdr.dll
  TelecomCdr.Tests  net10.0 succeeded → tests\TelecomCdr.Tests\bin\Debug\net10.0\TelecomCdr.Tests.dll
[xUnit.net] Discovering: TelecomCdr.Tests
[xUnit.net] Discovered:  TelecomCdr.Tests
[xUnit.net] Starting:    TelecomCdr.Tests
[xUnit.net] Finished:    TelecomCdr.Tests

Test summary: total: 27; failed: 0; succeeded: 27; skipped: 0
Build succeeded.
```

Repeated passing runs of Group C are evidence of correctness, not a formal proof of race-freedom. The
formal argument is the design itself (Section 4): each thread only ever touches memory no other thread
touches, so there is nothing left to race on.

## 7. Limitations

- Raw threads are not necessarily faster than the sequential baseline, especially for small arrays, where
  thread-creation overhead can exceed the cost of the pricing work itself. This assignment evaluates
  correctness, not performance, and no speed claim is made.
- The pipeline always uses exactly two worker threads and requires an even-length input array, as
  specified.
- Country codes are not checked against a real ISO list — any unknown non-empty two-letter-or-otherwise
  code simply falls into the 45.00/minute fallback tariff.
- The explicit NaN defensive arm in `CalculateCost` can never be reached through the public constructor
  (which already rejects NaN); it exists purely as a defense against a `CallRecord` built by bypassing the
  constructor (e.g. `default(CallRecord)` combined with reflection, or an uninitialized array element),
  and is exercised in tests via reflection for that reason.
- Splitting with `records[..mid]` / `records[mid..]` copies the input array, roughly doubling memory used
  during processing; an in-place partition would avoid this at the cost of more complex indexing.

## 8. AI assistance

Limited AI usage for: Architecture, Tests, Names of the functions, README writing and ExeptionThrowbacks naming
