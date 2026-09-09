# Task003 managed contracts

Run from the repository root:

```sh
python3 Tests/Task001/run.py
python3 Tests/Task003/run.py
python3 Tests/Task002/run.py
```

Requires Python 3 and Mono (`csc`, `mono`). Generated sources and binaries exist
only in a temporary directory. The sampler baseline reads commit `7bec5ca` without
changing the checkout; keep that history reachable.

- Task001 connects the actual Upgrade core, Tower instance/state, immutable reward,
  token and investment observation to the actual Pending collection. Its 30 cases
  preserve required-refresh failure, evidence flush, Debug isolation and reentrancy,
  and cover an expired consumption ticket and equal numeric IDs from different owners.
- Task003 compiles the actual collection and extracts production Initial selection,
  grant, rebuild, current-view predicate and sampling methods. It executes them
  against explicit native presentation boundaries. Cases cover preparation failure,
  lifecycle cancellation, atomic batches, rebuild preservation, stale/hidden views,
  read-only terminal state, expired identity and duplicate consumption.
- The earlier post-preflight investment segment tests were superseded by Task004's
  complete Submission tests. Deployment/Level Up lifecycle callback release now runs
  through actual preflight and collection ownership in `Tests/Task004/run.py`.
- 300 Draft traces compare baseline/current reservation counts, displayed order and
  next RNG value: 100 seeds across empty Pending, held Basic + Elemental, and consumed
  Basic with Elemental still held. Candidate eligibility uses a controlled three-Tower
  boundary; production weighting, reservation, sampling and shuffle code executes.

These are managed contract checks, not Unity Edit Mode/Play Mode, physics, prefab,
rendering or fresh Recorder export evidence. Native acceptance is recorded separately
in `Doc/Task/Task003_PendingDraftStateOwnership.md`.
