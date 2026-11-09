# Task branches and commits

Issue #166 coordinates the current classroom integration. The old `epic/unity-setup` hierarchy is historical; do not branch new demo work from it.

## Current delivery

- Start from freshly fetched main and use an isolated checkout for each writable branch. Never reset, stash, clean or switch another person's checkout. Each Unity project owns its own Library directory.
- Prajwal coordinates `integ/demo-reliability-166`, initially based on main `65ab921`. This parent is an integration boundary, not a claim that tests or devices have passed.
- Publish one coherent change at a time. Independent fixes can target main. A change requiring an open PR targets its prerequisite branch and names that dependency explicitly.
- Shared-scene follow-up work targets `integ/shared-scene-combined` while #158 remains open. Preserve newer main phone controls, checklist and audience when reconciling that candidate.
- Reuse work already on main. Build orchestration (#148), phone controls (#164), media (#154), audience (#163) and checklist (#165) are already present at this snapshot. Squash merges mean an old branch head need not be an ancestor of main; inspect actual files and main commits before replaying work.
- Review each task PR before integration. Publish the next task separately so teammates can review incrementally. Do not merge an incomplete shared scene or declare device acceptance from source-only checks.
- One integration owner saves the shared scene through the editor steps. Resolve scene conflicts by retaining the latest main scene and rerunning setup, never by merging scene text or copying a second network body.

## Handoff record

Each PR records branch, base commit, owner, changed areas, prerequisite PRs, exact checks run and remaining checks. Keep [the integration ledger](INTEGRATION_166.md) current as task PRs are published and reviewed. Issue #166 stays open until required device acceptance is recorded.

Use small conventional commits (`type(area): imperative summary`) with the repository's existing human identity. Keep code and required tests together. Use `Refs #166` for partial work; use `Closes` only when every acceptance criterion is met. Preserve task history, never rewrite a teammate's published branch without coordination, and review staged files before every commit.

Keep secrets, raw source assets, build outputs and local logs out of commits. Use Git LFS for applicable binary assets. Distinguish source checks, Unity tests, APK builds and device evidence in every handoff.
