# Quote Float WPF R1 — release notes

Publication-preparation notes for a locally accepted candidate with documented limitations. R1 is an acceptance label; no semantic release version, installer or uploaded binary is asserted.

## Included

- Windows x64 .NET 8 WPF implementation with Civic Wayfinding Full/Orb and Chinese/English.
- Server-returned supported quota windows, reset-credit count and future expiry; unknown values remain unknown.
- Discrete 40% Orb-first, 70% Compact Full and 100% Full layouts.
- Settings, topmost, edge/hover behavior, tray recovery and single-instance coordination.
- Loading without prior quota; Refreshing / 刷新中 while retaining accepted quota.
- WPF-owned Shared source copies remove the native compilation dependency.

Real quota mode reads local Codex authentication and makes authenticated HTTPS requests to chatgpt.com. Billing opens the usage page; the widget does not purchase or redeem credits. Low-quota-alert preference storage is present, but notification delivery is not claimed.

## Acceptance

Earlier final native acceptance was followed by PUB-WPF-001 source detachment: byte-identical copies, Release builds with zero warnings/errors, 29 deterministic fixtures in both current and independent source views, and native 96 DPI Full smoke with normal/automatic clean exits. Unchanged UI/stress evidence was reused. See [acceptance summary](ACCEPTANCE-SUMMARY.md) and [current manifest](CANDIDATE-MANIFEST.json).

Current candidate SHA-256:

- EXE: `77A2E2D531C56E6B5FE2A0E69A4203DD1E667ED61D88BB7FFF80A952E3C11A40`
- DLL: `CADA7E944A418F080EA43658B6C76ACCE68E05824C050AA7A66C9F0F122256E1`

The current DLL supersedes the earlier final-recheck candidate. These notes were prepared without a new build or runtime test.

## Mandatory limitations

1. Same real Pro account `Fresh -> SignedOut -> Fresh` — `NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE`.
2. Isolated Codex `Present -> close -> three absence confirmations -> Quote Float response -> restart/recovery` — `NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE`.
3. Native Windows 125% / 120 DPI and 150% / 144 DPI runtime acceptance — `NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE`.

Native verified baseline: Windows 100% / 96 DPI.

## Maintenance rule

- Relevant code changed -> rerun the relevant heavy gate.
- Relevant code unchanged -> reuse accepted evidence plus lightweight smoke.

Reuse requires source/candidate hashes and changed-file analysis that covers dependencies, packaging and runtime configuration.

Static design references retain historical control details overridden by later scale/Refreshing contracts. They are not final native screenshots. Binary distribution, signing, version assignment and publication require their own approval.
