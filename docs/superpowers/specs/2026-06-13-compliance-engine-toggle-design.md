# Design — Compliance Engine Toggle (experimental, 3-way)

Date: 2026-06-13
Status: proposed (awaiting review)
Repo: `ScoutsReporter/` (WPF app, v1.5.2)

## Goal

Let testers compare three ways of producing the DBS + training/dashboard compliance data, chosen
at runtime, so we can decide (with real data, at real scale) whether to adopt the Scouts backend
compliance engine or simply speed up our own. The **report columns, flags and dashboard tiles must
be identical across engines** so testers compare like-for-like — only the *data source / fetch
strategy* changes.

## The three engines

1. **Standard** — today's validated engine: discover members, then fetch each member's disclosures
   (SAS + Azure Table `Disclosures`) and training (`GetLmsDetailsAsync`) **sequentially**, then
   apply the POR `RoleRequirements` mapping. The known-good baseline (issue #2 fix, v1.5.2).
2. **Parallel** — identical logic and results to Standard, but the per-contact disclosure/training
   fetches run **concurrently** (bounded pool). Pure speed; numbers must match Standard exactly.
3. **Scouts backend** *(experimental)* — fetch per-member disclosure + training status in bulk from
   `DisclosureComplianceDashboardView` / `LearningComplianceDashboardView` (scoped by
   `teamId`+`unitId`, `isHierarchy:false`), mapped into the same record shape the report builders
   already consume.

## Architecture

Introduce one seam — **data acquisition** — behind an interface; everything downstream is unchanged.

```
IComplianceSource
  Task<ComplianceData> FetchAsync(IReadOnlyDictionary<string,Member> members,
                                  IProgress<string>? progress, CancellationToken ct)

ComplianceData {
  Dictionary<string,List<JsonElement>> Disclosures;   // keyed by contactId (today's shape)
  Dictionary<string,List<JsonElement>> Training;       // keyed by contactId
}
```

Implementations:
- `PerContactComplianceSource(bool parallel)` — wraps the existing `DisclosureService.FetchAll…`
  and `TrainingService.FetchAll…`. `parallel:false` → Standard; `parallel:true` → Parallel
  (bounded `SemaphoreSlim`, e.g. 8 concurrent; keep the periodic token refresh).
- `BackendDashboardComplianceSource` — queries the two compliance views, maps each row to the
  existing `Disclosures`/LMS record shape (CertificateReference/ExpiryDate/Status… and
  title/expiryDate/currentLevel). Where a field has no backend equivalent, leave blank and note it.

`DisclosureService.BuildReport`, `TrainingService.BuildReport`, `RoleRequirements`, and
`DashboardViewModel` are **unchanged**. RoleRequirements still runs for all three engines so the
N/A / Exempt logic is consistent (the backend already excludes non-required roles, so this is
belt-and-braces, not double-counting).

## Setting & UI

- Add `ComplianceEngine` (enum `Standard | Parallel | ScoutsBackend`, default `Standard`) to the
  existing `SettingsService` (persisted).
- Surface as a small **labelled "Experimental"** selector near the Dashboard / report run controls
  (a segmented control or dropdown). Show the active engine + a one-line "for testing" note.
- The dashboard/report run path resolves the chosen `IComplianceSource` via the setting.

## Feasibility spike (prerequisite for engine #3)

The backend views currently accept the query (`err:None`) but returned **0 rows** for the
team/unit I tried — scoping unresolved. **Step 1 is a spike** (read-only) to find the
`teamId`/`unitId` (+ "include all below hierarchy") combination that yields real per-member rows,
and to confirm the status→flag mapping. **Fallback:** if the spike can't get usable data, ship the
toggle with only **Standard** + **Parallel**, and hide/disable **Scouts backend** with a "not yet
available" note. No fragile half-working path ships.

## Testing

- Unit: `PerContactComplianceSource(parallel)` returns the same `ComplianceData` as sequential for a
  fixed fake API (results-equality test). Backend mapper: view-row → record-shape mapping tests.
- Manual/tester: run all three on the same unit; compare flags, %s, and runtime. Standard and
  Parallel must match exactly.

## Build order

1. Spike the backend queries (read-only) — decide if engine #3 is viable.
2. Extract `IComplianceSource` + `PerContactComplianceSource` (no behaviour change at `Standard`).
3. Add the parallel fetch path; verify results identical to Standard.
4. Add `ComplianceEngine` setting + experimental UI selector (Standard/Parallel only first).
5. If spike succeeded: add `BackendDashboardComplianceSource` + enable the third option.
6. Ship to testers (default Standard).

## Risks / open questions

- Backend views are finicky/flaky (history of transient `error_GetResultsAsync`); need retry +
  graceful fallback per run.
- Backend data may **lag** real-time Table Storage; testers should know the freshness caveat.
- Backend status vocabulary differs (`FirstAid`/`SafeGuarding` vs LMS `First Response`/`Safeguarding`);
  mapping must be explicit.
- Parallel fetch must respect token refresh / rate limits (bounded concurrency).
