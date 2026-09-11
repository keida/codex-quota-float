# Growth Plan

## Purpose

Make the project discoverable as **Codex Quota Float for Windows**, understandable in a few seconds, and safe to try without overstating what v1.1.2 proves.

This document records recommendations only. The visual assets listed below were produced locally for review; no repository settings, Social Preview upload, or external channel has been changed.

## Current growth blockers

1. **Brand ambiguity:** “Quote Float” is easy to confuse with other quota widgets. Public copy should lead with “Codex Quota Float for Windows”, then clarify native Windows WPF, Codex-only, Full/Orb, English/中文, and local-first behavior.
2. **Visual proof:** local review assets now include coherent English and Chinese overviews, a real Full → edge snap → Orb → hover → language-switch demo GIF, and a 1280 × 640 Social Preview candidate. They use real WGC captures of the current WPF demo mode and remain local-only until separately reviewed and uploaded.
3. **Windows trust friction:** the current EXE is unsigned, so SmartScreen may warn on first run. The safe path is official Release → SHA-256 verification → follow the user's Windows or organization policy; never ask users to bypass a warning blindly.
4. **Install-channel discoverability:** the official Latest Release is clear, but Scoop and WinGet are not supported. Do not list them as available until a real package and maintenance path exist.
5. **Validation boundary:** only Windows 100% / 96 DPI is verified. Native 125% / 150% DPI and the two real lifecycle sequences remain explicitly unverified.
6. **Small public footprint:** discovery should depend on clear search language and trustworthy artifacts, not claims of popularity, endorsement, or unperformed security testing.

## Recommended repository metadata

### Description recommendation

> Codex Quota Float for Windows — native WPF Full/Orb quota widget for Codex Plus/Pro, local-first, English/中文.

This is a recommendation only. It keeps the searchable product phrase at the front and avoids the generic “Quote Float” ambiguity.

### Topic recommendation

Keep the useful current topics and consider adding only terms that describe the actual product:

- `codex-quota`
- `codex-desktop`
- `windows-widget`
- `local-first`

Current topics already include `codex`, `csharp`, `desktop-widget`, `quota-monitor`, `windows`, `dotnet`, `dotnet-8`, and `wpf`. Do not add `cross-platform`, `auto-update`, `signed-app`, `winget`, or `scoop` until those claims are true.

## Brand and search language

Use these phrases consistently in the README, issue forms, release-linked material, and future visuals:

- Primary: **Codex Quota Float for Windows**
- Product type: **native Windows WPF quota widget**
- Scope: **Codex-only, Plus/Pro, Full/Orb, English/中文, local-first**
- Installation: **self-contained Windows x64 single-file EXE**

Avoid leading with “Quote Float” alone. Do not rename the repository, EXE, assembly, namespace, or internal identities in this work.

## Demo GIF storyboard — 10–15 seconds

Local review asset: `docs/assets/codex-quota-float-demo.gif`.

1. **0–2s:** show the Full view with clearly labeled illustrative quota values and the “Codex Quota Float for Windows” title card.
2. **2–5s:** drag or move the widget to the screen edge and show the accepted edge snap.
3. **5–7s:** show the compact Orb view.
4. **7–10s:** hover to expand temporarily back to Full.
5. **10–13s:** open Settings and switch English → 简体中文.
6. **13–15s:** show Settings switching to 简体中文 and end on the localized Full result.

Keep the capture on Windows x64 at the verified 100% / 96 DPI baseline. Use sample values only; do not expose accounts, tokens, raw responses, or machine-local paths. The local candidate is 8 frames, 14.41 seconds, and 496,028 bytes. Caption the GIF with “Illustrative quota values; Windows 100% / 96 DPI baseline.”

## GitHub Social Preview specification

Local review asset: `docs/assets/codex-quota-float-social-preview.png`.

- Output: PNG, JPG, or GIF under 1 MB.
- Canvas: 1280 × 640 preferred; at least 640 × 320.
- Use a solid dark background for predictable contrast.
- Lead with “Codex Quota Float for Windows”.
- Show one coherent Full/Orb/Settings composition from current real WPF captures.
- Supporting line: “Real WPF states · edge snap · local preferences”.
- Do not show real account data, tokens, raw API responses, or claims such as signed, cross-platform, or DPI-complete.

The corrected Chinese overview, demo GIF, and Social Preview candidate are local outputs for review. They have not been uploaded, posted, or used to change repository settings.

## Prioritized future ticket map

| Priority | Ticket | Status | Outcome |
| --- | --- | --- | --- |
| P0 | Corrected Chinese product overview asset | Ready for review locally | Replaced the malformed visual with a real, readable WPF capture sheet. |
| P1 | Demo GIF — Full → edge snap → Orb → hover → ZH/EN | Ready for review locally | 8 real WGC-captured frames, 14.41 seconds, under 8 MB. |
| P1 | Social Preview — 1280 × 640, under 1 MB | Ready for review locally | 1280 × 640 PNG, 144,013 bytes, with real product captures. |
| P1 | Code signing evaluation | Under consideration | Reduce SmartScreen friction with a supportable signing process. |
| P2 | Scoop / WinGet packaging | Under consideration | Add a real package, owner, and update path before claiming support. |
| P2 | Automatic update and rollback design | Under consideration | Define trust, rollback, and release-channel behavior first. |
| P1 | Windows 125% / 150% native DPI acceptance | Planned | Extend validation beyond the current 100% / 96 DPI baseline. |
| P1 | Same-account Fresh → SignedOut → Fresh | Planned | Close the current real-Pro-account lifecycle evidence gap. |
| P1 | Isolated Codex close/restart lifecycle | Planned | Validate absence, response, restart, and recovery in isolation. |

## Measurement without overclaiming

Track future evidence such as release-link clicks, issue-template completion quality, first-run failures, and repeated installation questions. Do not add product telemetry merely to measure growth; the current source has no analytics endpoint, and privacy remains part of the product trust story.
