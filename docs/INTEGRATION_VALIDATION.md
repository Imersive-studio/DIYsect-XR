# Shared classroom integration validation

Branch: `fix/shared-classroom-integration`

Reviewed on November 8, 2026 with Unity 6000.3.28f1.

## Corrected behavior

- Professor, student and phone APKs include the same `Professor_MR` scene. The `.student` identifier selects the VR rig with fixed 1.6 m eye height, locomotion and quit menu; professor panels and the phone camera stay off.
- The professor creates the room instead of joining another host. Existing host-only quiz and Answered permissions therefore apply to the professor. Failed room creation retries at the configured interval.
- Submitting an invite code suppresses public-room discovery, including after an invalid code or failed join. A code arriving during a public-room search takes precedence over its result.
- Reset-view tests assert that UI presses never emit pin-placement taps, independently of whether the question board has spawned.
- Incoming teleport states cannot overwrite a held object's local pose. The latest server state remains buffered for release. The regression test forces this path rather than relying on intermittent spawn timing.
- The camera-view pin test excludes the other in-process peers' overlapping colliders and asserts that its ray hits its own peer.

The existing scene object IDs were retained. The quit menu uses the existing input actions and menu builder; no packages or networking framework were added.

## Executed checks

| Check | Result |
|---|---|
| Baseline EditMode | 401 passed |
| Final EditMode | 404 passed, 0 failed |
| Final PlayMode | 160 passed, 0 failed |
| Deterministic teleport and raycast reproductions | Both failed before their fixes; both passed afterward |
| Browser regression suite | 15 passed |
| Anatomy validation | 86 GLBs passed; 310,078 triangles |
| Student Android ARM64 IL2CPP release build | Succeeded in 299 seconds; about 72 MB APK; 14 warnings |

Unity XML results and build logs are in the ignored `Logs/integration/` folder of the validation worktree. The student APK is `Builds/Android-Student/DIYsect-Student.apk`; its manifest confirms the `.student` package, ARM64 and Android API level 34. The final test reports are `isolated-edit.xml` and `verified-play.xml`.

## Required device rehearsal

1. Build all three apps from this branch. Launch the professor first and confirm it hosts and displays its room code.
2. Join with the student headset and the classmate phone. Confirm that all three display the same body and dissection state.
3. On the student, verify fixed eye height, opaque VR, locomotion, grabbing, release convergence and the quit menu. On the professor, verify passthrough, recenter, the depth handle and guide pen.
4. On the phone, test a rejected code, a corrected code, an organ pin and Reset view. Confirm that Reset view emits no pin and that the professor can mark the pin answered and manage the quiz.
5. Disconnect and rejoin a client, then verify body state, pins and answered status. Measure Quest frame timing with all views visible.

No new headset or phone installation, live cloud-room rehearsal, visual inspection on a device, or Quest performance measurement was performed during this fix pass.
