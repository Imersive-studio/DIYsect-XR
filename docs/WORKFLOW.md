# Task branches and commits

Use one parent branch for a feature or setup milestone, and a small branch for each issue or coherent subtask. The current parent is `epic/unity-setup`; its PR targets `main`.

- Branch an independent task from the parent. Branch dependent work from its prerequisite and explain that dependency in the PR.
- Target task PRs at the parent or prerequisite branch. Merge dependent PRs into their prerequisite before merging that prerequisite into the parent, or retarget them after the prerequisite lands.
- Prefer one focused commit for a small issue. Split larger issues into independently understandable changes; keep code and its required tests together. Avoid arbitrary one-file commits and unrelated cleanup.
- Use `type(area): imperative summary`. Reference the actual issue in the PR. Use `Refs` for partial progress; use `Closes` only when every acceptance criterion is complete.
- Keep binary imports separate from processing logic. Group anatomy assets by anatomy system or layer; an intact third-party source import is one coherent change even if it contains several files.
- Preserve reviewed task commits when merging into the parent. Do not squash the entire parent milestone into one commit if that would erase the task history.
- Review the staged diff, run relevant checks, and verify the configured author before each commit. Never rewrite published history without coordinating with the team.

## Current review hierarchy

| Branch | PR base | Scope / checklist task |
|---|---|---|
| `task/repository-conventions` | `epic/unity-setup` | Local-file exclusions and LFS rules; setup prerequisite |
| `task/unity-folders` | `epic/unity-setup` | Folder and assembly scaffold; partial task 1 |
| `task/anatomy-tools` | `epic/unity-setup` | Fetch, extraction, skin, previews and validation; task 3 tooling |
| `task/anatomy-assets` | `task/anatomy-tools` | Processed anatomy, provenance and credits; task 3 outputs |
| `task/room-processing` | `task/anatomy-tools` | Room simplification and synthetic test; partial task 4 |
| `task/mesh-slicing` | `epic/unity-setup` | Optional planar slicing source; task 9 prerequisite |
| `task/classroom-props` | `task/unity-folders` | Primitive prop creation; partial tasks 8 and 15 |
| `task/unity-onboarding` | `epic/unity-setup` | Setup instructions, architecture and acceptance checklist |

`review/unity-setup` is a local integration preview containing all task branches for immediate setup and validation. It is not a delivery branch or an additional PR. Actual integration proceeds through the task PRs and then the parent PR.

Checklist task numbers refer to `UNITY_BACKLOG.md`, not GitHub issue numbers. The PRs link the corresponding GitHub issues. Manual template creation, device checks and login-required downloads stay open until their acceptance criteria are met.
