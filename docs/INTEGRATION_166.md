# Classroom integration ledger

Owner: Prajwal. Scope: [issue #166](https://github.com/Imersive-studio/DIYsect-XR/issues/166), with room acceptance from #125 and classroom/demo acceptance from #126.

## Checked baseline

Main `58a2817` (9 November 2026), including PR #158 and the subsequent PR #174 fix. PR #158 merged the shared-scene composition at `99d0d65`; its history includes #169 (`ae53a80`), which lets the hands reach the organs. The earlier 421 EditMode / 160 PlayMode report belongs to that candidate before merge and is not evidence for the current main branch or a final device build.

| Area | Current condition | Next action |
|---|---|---|
| Audience #159 / #163 | On main at `abcf364` | Verify final shared-role behavior; measure full scene and Quest frame timing |
| Checklist #161 / #165 | On main at `65ab921`, including saved scenes/material | Verify final shared-role behavior |
| Phone controls #155 / #164 | On main at `c16a807` | Preserve Join, voice, hand, name/pin and safe-area setup during role integration |
| Build orchestration #148 | On main at `d13b748` | Reuse; build all three actual APKs from the final commit |
| Media #154 | On main at `77304bb` | Keep editor-rendered/staged labels; do not count as device rehearsal |
| Shared-scene composition #158 / hand reach #169 | Merged to main in #158 at `99d0d65`; #169's `ae53a80` change is included | Verify the merged scene and hand reach in the final combined build |
| Practice/cloud coordination | Shared-scene work added a timed practice fallback; later lifecycle drafts remain separate | Reproduce races, connect explicit lifecycle rules to live paths, and add fake-service coverage |
| Device acceptance | No new combined result recorded here | Run final suites, builds and five device rehearsals |

## Modular task order

1. **Workflow and demo correction:** publish the current ledger and move depth guidance into the incision segment. Documentation-only, no runtime behavior claim.
2. **Shared scene and student presentation:** #158 has merged, including the student join, practice and input changes. Verify the merged composition retains current phone hooks, one shared body and role-specific objects; save and validate any remaining scene changes in Unity.
3. **Room lifecycle:** failing reproductions first; coordinate cloud search/join/cancel/practice through explicit states and one writer. Cover stale results, retries, invalid invites, failed host cleanup and compatible discovery using a fake service boundary.
4. **Combined interaction coverage:** verify professor authority, simultaneous tools, recenter/alignment, local question pins, phone gesture exclusions and client reconnect ownership. Extend real behavior tests.
5. **Final integration evidence:** run EditMode then PlayMode with graphics, build the three APKs from one commit, validate the real devices and update deployment instructions to the observed contract.

Each task is published before starting the next dependent task. Task PRs target main when independent or their prerequisite branch when dependent. Reviews and device blockers remain visible; a source-preparation PR is not a completed feature.


## Published increments and remaining work

| PR | Branch / base | Published scope | Remaining gate |
|---|---|---|---|
| #167 | docs/integration-166-plan / main | Workflow, demo timing and this ledger | Review |
| #168 | feat/shared-student-presentation / integ/demo-reliability-166 | Reusable student hall, audience and light composition; preserve one body and role wiring | Its source branch predates merged #158; reconcile with current main before reuse, then save and test the scene in Unity |
| #170 | fix/failed-room-cleanup / integ/demo-reliability-166 | Capture old room during deletion; start failed-host cleanup before retry notification | Unity suites and service/device checks |
| #171 | fix/room-operation-cancellation / fix/failed-room-cleanup | Explicit cancellable queue; wait for service completion and cleanup; reject superseded drains | Connect live room/search/practice paths; Unity suites |

The integration parent recorded during the original work was
`integ/demo-reliability-166` at `7d199b6`, based on main `65ab921` and the
earlier shared-scene candidate. This is historical coordination context, not
the current main baseline. It was not a scene-validated release.

At the checked baseline, main is `58a2817`, which includes the #158 shared-scene
merge and #174's eye-height guard restoration. The #158 merge includes #169's
hand-reach fix. Their earlier candidate and task test reports remain historical;
they do not validate the eventual combined build or device behavior.

The #171 queue passed six standalone asynchronous C# checks. The superseded-drain
case was reproduced failing before the correction and passed afterwards. These
checks do not establish that live cloud operations use the queue, check
cancellation after service awaits, or release their own resources. Discovery,
invalid-invite retry, disconnect and practice handoff remain runtime work.

No runtime draft above is merged or declared complete by this ledger.
## Ownership and checkout boundaries

Prajwal owns this integration effort. Issue assignments identify Aaditya for room lifecycle, Arjun for professor/phone UI and Saurav for student/headset validation. These assignments are not proof that their current edits have stopped. Confirm overlapping file ownership before changing their active branches or saving shared scenes.

The original Saurav checkout and the uncommitted `/Users/saurav/Desktop/files/DIYsect-reliability` preparation were not used by this effort; these paths and checkout notes are historical machine-specific context. Work used isolated Windows checkouts under the project's ignored Temp directory. Do not share Library directories. Record the exact active branch and base in each task PR.

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

At the original documentation check, Unity was unavailable in the standard locations on that Windows computer. That is historical machine-specific evidence. No Unity tests, APK builds or device results are claimed by this documentation update; all listed device gates remain pending until recorded against a specific commit and hardware.

## Five rehearsal records

Run the complete demo in each rehearsal, adding the listed recovery scenario. Record the final commit and all three APK paths/sizes, headset and phone models, tester, observed result and linked failure issues. Blank entries are pending, never passes.

| Run | Required start/recovery scenario | Commit and build record | Devices / tester | Observed result / issue links |
|---|---|---|---|---|
| 1 | Professor first, student and phone join | Pending | Pending | Pending |
| 2 | Student starts before professor; no practice takeover of a late cloud join | Pending | Pending | Pending |
| 3 | Phone starts first; invalid invite twice, then valid invite | Pending | Pending | Pending |
| 4 | Client drops and rejoins during the lesson | Pending | Pending | Pending |
| 5 | Host restarts; verify the documented fresh-lesson or recovery behavior | Pending | Pending | Pending |
