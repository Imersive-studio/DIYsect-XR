# Classroom integration ledger

Owner: Prajwal. Scope: [issue #166](https://github.com/Imersive-studio/DIYsect-XR/issues/166), with room acceptance from #125 and classroom/demo acceptance from #126.

## Checked baseline

Main `65ab921`, checked 9 November 2026. Shared-scene candidate `d04618f` remains separate in PR #158. Its historical 421 EditMode / 160 PlayMode report is candidate evidence, not a result for the eventual combined branch.

| Area | Current condition | Next action |
|---|---|---|
| Audience #159 / #163 | On main at `abcf364` | Preserve during shared-scene composition; measure full scene and Quest frame timing |
| Checklist #161 / #165 | On main at `65ab921`, including saved scenes/material | Preserve latest wiring and verify final shared-role behavior |
| Phone controls #155 / #164 | On main at `c16a807` | Preserve Join, voice, hand, name/pin and safe-area setup during role integration |
| Build orchestration #148 | On main at `d13b748` | Reuse; build all three actual APKs from the final commit |
| Media #154 | On main at `77304bb` | Keep editor-rendered/staged labels; do not count as device rehearsal |
| Shared-scene candidate #158 | Open, `integ/shared-scene-combined` at `d04618f` | Reconcile with current main and preserve student presentation before integrating |
| Practice/cloud coordination | Main still has the timer-based practice fallback | Reproduce races, add explicit lifecycle rules and fake-service coverage, then fix |
| Device acceptance | No new combined result recorded here | Run final suites, builds and five device rehearsals |

## Modular task order

1. **Workflow and demo correction:** publish the current ledger and move depth guidance into the incision segment. Documentation-only, no runtime behavior claim.
2. **Shared scene and student presentation:** reconcile #158, retain newer phone hooks, add editor composition for hall/audience/guidance, and check one shared body plus role-specific objects. Publish source preparation separately if Unity scene saving is unavailable; keep that PR draft.
3. **Room lifecycle:** failing reproductions first; coordinate cloud search/join/cancel/practice through explicit states and one writer. Cover stale results, retries, invalid invites, failed host cleanup and compatible discovery using a fake service boundary.
4. **Combined interaction coverage:** verify professor authority, simultaneous tools, recenter/alignment, local question pins, phone gesture exclusions and client reconnect ownership. Extend real behavior tests.
5. **Final integration evidence:** run EditMode then PlayMode with graphics, build the three APKs from one commit, validate the real devices and update deployment instructions to the observed contract.

Each task is published before starting the next dependent task. Task PRs target main when independent or their prerequisite branch when dependent. Reviews and device blockers remain visible; a source-preparation PR is not a completed feature.

## Ownership and checkout boundaries

Prajwal owns this integration effort. Issue assignments identify Aaditya for room lifecycle, Arjun for professor/phone UI and Saurav for student/headset validation. These assignments are not proof that their current edits have stopped. Confirm overlapping file ownership before changing their active branches or saving shared scenes.

The original Saurav checkout and the uncommitted `/Users/saurav/Desktop/files/DIYsect-reliability` preparation are not used by this effort. Work uses isolated Windows checkouts under the project's ignored Temp directory. Do not share Library directories. Record the exact active branch and base in each task PR.

## Evidence gates

- [ ] Shared build scene lists and network identities verified.
- [ ] Student hall, audience, tools, checklist, eye height and practice retained; professor passthrough and phone UI retained.
- [ ] State defects reproduced before fixes; deterministic service tests pass.
- [ ] Final combined EditMode and graphics-enabled PlayMode suites pass.
- [ ] Three APKs built from one commit, with output paths and sizes recorded.
- [ ] Two Quests and a phone complete dissection, depth guidance, questions and Answered.
- [ ] Client rejoin restores shared state and releases stale ownership.
- [ ] Host restart behavior documented and checked separately; no lesson-persistence promise without evidence.
- [ ] Quest input, readability, passthrough, whole-scene triangle budget and frame timing verified.
- [ ] Five rehearsal records complete; unresolved failures have linked issues.

Unity was unavailable in the standard locations on the current Windows computer. No new Unity tests, APK builds or device results are claimed by this documentation update.

## Five rehearsal records

Run the complete demo in each rehearsal, adding the listed recovery scenario. Record the final commit and all three APK paths/sizes, headset and phone models, tester, observed result and linked failure issues. Blank entries are pending, never passes.

| Run | Required start/recovery scenario | Commit and build record | Devices / tester | Observed result / issue links |
|---|---|---|---|---|
| 1 | Professor first, student and phone join | Pending | Pending | Pending |
| 2 | Student starts before professor; no practice takeover of a late cloud join | Pending | Pending | Pending |
| 3 | Phone starts first; invalid invite twice, then valid invite | Pending | Pending | Pending |
| 4 | Client drops and rejoins during the lesson | Pending | Pending | Pending |
| 5 | Host restarts; verify the documented fresh-lesson or recovery behavior | Pending | Pending | Pending |
