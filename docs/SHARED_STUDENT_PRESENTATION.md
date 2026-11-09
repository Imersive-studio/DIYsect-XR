# Shared student presentation preparation

Issue #166; source dependency: PR #158. Base main: `65ab921`.

This source preparation reconciles the shared-role candidate with current phone controls, seated audience and immersive checklist. The saved `Professor_MR` scene remains the main version. It is not yet a composed or validated three-role scene.

## Scene owner steps

1. Open this checkout in Unity 6000.3.28f1, with its own Library. Resolve compilation/import problems before saving.
2. Run **Tools > DIYsect > Build Shared Scene**. It composes the lecture hall, operating light and 40 seated classmates around the existing shared body; it binds them only to the Student role. Repeating the operation reuses the room and audience. It does not copy another body or rebuild the current checklist and organ cards.
3. Inspect all three roles: student opaque VR, eye-height calibration, locomotion, quit menu and room discovery; professor passthrough and tools without student scenery; phone Join screen, invite, mute, raise hand, name tap, held question pin and reconnect banner. The student does not wait for the phone-only Join button.
4. Confirm one shared body, retained network object identities, checklist target references and organ cards. Review the saved scene diff, then commit the Unity-saved scene separately.
5. Run `SharedStudentCompositionTests` and `SharedSceneRoleTests`, then all EditMode and graphics-enabled PlayMode tests. Build all three APKs from that reviewed commit and rehearse on devices.

The composition command rejects more than 750,000 scene mesh triangles. This is a mesh-instance count, not device frame timing. Measure Quest performance separately.

## Evidence and limits

Source conflicts were reconciled; the current main scene was retained wholesale. Added tests cover repeat composition, audience count, unchanged network identities/checklist bindings and student-only activation. Unity is unavailable in this environment: compilation, test execution, scene composition, triangle count, APK builds and device acceptance remain pending. Earlier candidate results do not apply to this combined source.

Standalone practice is not wired into the shared role in this chunk. The existing fallback must first be coordinated with in-flight cloud joins under #166; adding it now could race the shared connection.
