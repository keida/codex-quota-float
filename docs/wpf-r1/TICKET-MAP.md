# WPF R1 Ticket Map

Status: **simplification and final ownership fix accepted — Boss PASS**. Publication: **HOLD**.

| Work item | Current status |
| --- | --- |
| QF-WPF-006 | Topmost, edge, work-area and restore behavior — BOSS PASS |
| QF-WPF-009 | Presence, single-instance and lifecycle coordination — BOSS PASS for deterministic R1 scope |
| QF-WPF-010 | Historical Product Scale work — superseded and removed from R1 |
| QF-WPF-012 | Settings, safe states, refresh semantics and interaction — BOSS PASS |
| WPF-R1-SIMPLIFICATION | Settings/footer/state/corner simplification — BOSS PASS |
| WPF-R1-SIMPLIFICATION-FINAL-FIX | ApplyState ownership correction — BOSS PASS |
| WPF-R1-SIMPLIFICATION-FREEZE | Candidate docs, manifest and V3 allowlist — this packet |
| PUBLICATION | HOLD; requires exact staging review and separate external authorization |

The existing tag and Draft Release are historical pre-simplification artifacts. They must not be moved, reused, or treated as the simplified R1 publication.

R1 scope exclusions remain:

1. Same real Pro account `Fresh -> SignedOut -> Fresh` — `NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE`.
2. Isolated Codex close/absence/response/restart recovery — `NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE`.
3. Native Windows 125% / 150% DPI runtime acceptance — `NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE`.

See [`ACCEPTANCE-SUMMARY.md`](ACCEPTANCE-SUMMARY.md) and [`PUBLICATION-ALLOWLIST-V3.md`](PUBLICATION-ALLOWLIST-V3.md) for the evidence and exact publication boundary.
