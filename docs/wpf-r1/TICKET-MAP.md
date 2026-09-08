# WPF R1 acceptance register

WPF R1 is accepted with documented limitations. This register preserves final outcomes; detailed historical tickets and local audit records are not shipped. It is not a dispatch queue.

| Record | Accepted outcome |
|---|---|
| QF-WPF-001 | Native WPF shell — BOSS PASS |
| QF-WPF-002 | Civic Plus Full — BOSS PASS |
| QF-WPF-003 | Civic Pro Full — BOSS PASS |
| QF-WPF-004 / 004A | Orb and 10-segment indicators — BOSS PASS |
| QF-WPF-005 | Orb/Full transitions — BOSS PASS |
| QF-WPF-006 | Topmost, edge and work-area behavior — BOSS PASS |
| QF-WPF-007 | Settings and tray safety recovery — BOSS PASS |
| QF-WPF-008 | Chinese/English state coverage — BOSS PASS |
| QF-WPF-009 | Data/refresh/sync implementation accepted; external acceptance deferred |
| QF-WPF-009E | DEFERRED / NOT VERIFIED — OUT OF R1 ACCEPTANCE SCOPE |
| QF-WPF-010 | Discrete scale and native 96 DPI matrix — BOSS PASS |
| QF-WPF-011 | Bounded performance/lifecycle acceptance — BOSS PASS |
| QF-WPF-012F | Settings resource fix, English safe states and Minimal Refreshing — BOSS PASS |
| QF-WPF-012 | Final native acceptance with limitations — BOSS PASS |
| PUB-WPF-001 | WPF-owned shared sources; independent build/test and 96 DPI smoke — BOSS PASS |

PUB-WPF-001 supersedes the earlier candidate DLL without changing the copied logic or UI contract. [Candidate manifest](CANDIDATE-MANIFEST.json) identifies the current files. Detailed provenance, reuse limits and test results are in [Acceptance summary](ACCEPTANCE-SUMMARY.md).

1. Same real Pro account `Fresh -> SignedOut -> Fresh` — `NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE`.
2. Isolated Codex `Present -> close -> three absence confirmations -> Quote Float response -> restart/recovery` — `NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE`.
3. Native Windows 125% / 120 DPI and 150% / 144 DPI runtime acceptance — `NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE`.

Native verified baseline: Windows 100% / 96 DPI.

- Relevant code changed -> rerun the relevant heavy gate.
- Relevant code unchanged -> reuse accepted evidence plus lightweight smoke.

Reuse requires source/candidate hashes and changed-file analysis that covers dependencies, packaging and runtime configuration.

This documentation cleanup is awaiting independent review and authorizes no publication action.
