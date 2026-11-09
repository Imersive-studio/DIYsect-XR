# Task branches and commits

Issue #166 coordinates the current classroom integration. The old `epic/unity-setup` hierarchy is historical; do not branch new demo work from it.

## Current delivery

- Start from freshly fetched main and use an isolated checkout for each writable branch. Never reset, stash, clean or switch another person's checkout. Each Unity project owns its own Library directory.
- The historical coordination parent `integ/demo-reliability-166` was based on main `65ab921`; it is not the current main baseline or a claim that tests or devices passed. See the ledger for the checked main SHA and remaining gates.
- Publish one coherent change at a time. Independent fixes can target main. A change requiring an open PR targets its prerequisite branch and names that dependency explicitly.
- PR #158, including the #169 hand-reach fix, has merged to main. The earlier candidate is historical; verify the merged scene against any remaining work rather than targeting that closed prerequisite branch.
- Reuse work already on main. At the checked baseline `58a2817`, main includes build orchestration (#148), phone controls (#164), media (#154), audience (#163), checklist (#165), shared-scene composition (#158, including #169), and the eye-height guard restoration (#174). Squash merges mean an old branch head need not be an ancestor of main; inspect actual files and main commits before replaying work.
- Review each task PR before integration. Publish the next task separately so teammates can review incrementally. Do not merge an incomplete shared scene or declare device acceptance from source-only checks.
- One integration owner saves the shared scene through the editor steps. Resolve scene conflicts by retaining the latest main scene and rerunning setup, never by merging scene text or copying a second network body.

## Handoff record

Each PR records branch, base commit, owner, changed areas, prerequisite PRs, exact checks run and remaining checks. Keep [the integration ledger](INTEGRATION_166.md) current as task PRs are published and reviewed. Issue #166 stays open until required device acceptance is recorded.

Use small conventional commits (`type(area): imperative summary`) with the repository's existing human identity. Keep code and required tests together. Use `Refs #166` for partial work; use `Closes` only when every acceptance criterion is met. Preserve task history, never rewrite a teammate's published branch without coordination, and review staged files before every commit.

Keep secrets, raw source assets, build outputs and local logs out of commits. Use Git LFS for applicable binary assets. Distinguish source checks, Unity tests, APK builds and device evidence in every handoff.
