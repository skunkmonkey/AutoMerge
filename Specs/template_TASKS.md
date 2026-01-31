# <Feature Name> – Tasks
Tasks-Version: 1.0
Spec: skt/Docs/Specs/<feature>_SPEC.md
PRD:  skt/Docs/Specs/<feature>_PRD.md

## Conventions
- States: 
  - `[ ]` = not started
  - `[>]` = locked/in-progress (include timestamp and actor)
  - `[x]` = done (include timestamp, commit)
  - `[!]` = blocked (include reason)
- Each task must reference at least one `S-###` and one `AT-###`.
- Task Abbreviations:
  - S = Spec requirement
  - AT = Acceptance test
  - R = PRD requirement
  - N = PRD non-functional requirement
  - C = PRD constraint
  - Perf = Performance requirement

## Phase 1 – Models & Validation
- [ ] **T-010 Implement AutoRoof model & validation**
  - S: S-101, S-102
  - AT: AT-020, AT-021
  - R: R-001
  - N: NFR-004
  - Scope: `skt/Data/*.cs`, `/skt/Data.UnitTests/*.cs`
  - Out-of-Scope: UI, persistence
  - Perf: `<0.5ms p95 per validation`
  - Telemetry: `validation.autoroof.failures{reason}`

- [ ] **T-014 Implement AutoRoof validation service**
  - S: S-103
  - AT: AT-022
  - R: R-001
  - C: CON-003
  - Scope: `skt/Busi/*.cs`, `skt/Data/*.cs` `/skt/Busi.UnitTests/*.cs`
  - Out-of-Scope: persistence

## Phase 2 – Services
- [ ] **T-030 AutoRoof User Interface**
  - S: S-201, S-202
  - AT: AT-040, AT-041
  - R: R-010, R-011
  - Scope: `xm8/sketch/UI/*.cs`, `xm8/sketch/UI.Controls/*.cs`

<!-- Continue with T-### items; same schema -->
