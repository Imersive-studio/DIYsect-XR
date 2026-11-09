# Immersive dissection checklist

Issue #161 upgrades headset presentation. `ChecklistTracker` still owns learning progress through the unchanged `ChecklistProgress` and `ChecklistRule`. Nothing new goes over the network. Desktop and phone HUD code and scenes are unchanged.

## Apply the upgrade

1. Open this branch in Unity **6000.3.28f1** and let it import and compile.
2. Save any work in open scenes. Run **Tools > DIYsect > Add Dissection Checklist**. The command opens and saves `Professor_MR` and `LectureHall_VR`. Missing targets produce an error and retain that scene's old checklist. The material is created at `Assets/Models/Environment/ChecklistGuidance.mat`.
3. Run the command again. Each scene must still contain one `DIYsect/DissectionChecklist`, with no duplicate cues. The command only replaces this presentation subtree, preserving the audience and existing network objects.
4. The issue mentions **Add Stable Network Ids**, but that command is absent from main at `31295a7`. Do not invent IDs or edit scene text. `ChecklistSceneSetupTests` checks that rebuilding does not change existing network objects or their serialized values. If that command returns on a newer main, run it as the issue requests and review its diff.
5. Commit the two saved scenes and new material and metadata after the checks below. If another branch changes a scene, use its latest main version and rerun the editor command. Do not merge scene text manually.

The editor step creates objects on the Default layer, removes primitive colliders and disables shadows. It measures the panel position beside the table and keeps it at least 1.1 m from the starting rig position. It adjusts initial size for that distance and then leaves the panel anchored to the table root. Walking never attaches the panel to the head. Headset testing must still verify comfort and reach from the actual standing position.

## What the student sees

| Step | Cue | Instruction |
|---|---|---|
| Trace incision | Pulsing copy of the measured incision path and a small start ring | Hold grip on the scalpel. Draw the tip down the line |
| Open skin | Midline edge cues follow the two flap transforms | Skin opening |
| Open rectus | Ring on the rectus touch zone | Touch the muscle with the forceps tip |
| Lift omentum | Ring on the omentum touch zone | Touch the omentum with the forceps tip |
| Lift organ | Gentle rings around available organ bounds | Hold grip on an organ to lift it out |
| Inspect organ | Ring fills around the tracked organ using the tracker's inspection clock | Keep holding to inspect it |
| Put organ back | Existing socket ghost while held, plus a home ring | Bring it back to its place |
| Complete | Brief completion card, then the compact ticked list | All steps done. |

These are training cues, not new surgical mechanics. Forceps already request both layer actions through `LayerTouchTrigger`. Skin opening is automatic. Existing socket verdict feedback stays owned by `OrganGrabPresenter`.

## Files and behavior

- `ChecklistLabels` holds all wording, including short instructions.
- `ChecklistTransitionRule` maps old/new snapshots to Snap, Advance, Revert, Reset or Complete. A first refresh always snaps.
- `ChecklistPanel` coalesces events in one frame, reads the final tracker state and owns only the presentation transition. A new event interrupts and replaces the old transition. Ticks draw for 0.2 seconds, rows settle for 0.25 seconds and the new card fades in for 0.25 seconds. Revert and reset use calm fades. The complete card collapses after a short pause; the list remains.
- `ChecklistEscalationRule` selects explicit guidance after 12 seconds. `ChecklistBodyCues` resets that clock on a step change or increased incision progress. Explicit guidance has a stronger slow pulse and a tool-to-target arrow. It never moves the camera or requests gameplay actions.
- `ChecklistTracker` exposes read-only active-organ and inspection-fraction properties. The fraction uses the same start time and configured hold duration as its existing inspection coroutine. A `StateSynchronized` notification lets the panel snap when the initial network snapshot arrives after enable. The step rules and network behavior are unchanged.
- `ChecklistPresentationBuilder` wires real scene references, including the serialized incision path, flap transforms, measured touch boxes and ordered shared organs. Bounds rings do not copy anatomy meshes. `ChecklistBacking` restores its dark tint on load because Unity does not serialize property blocks.

One shared unlit material serves the backing and cues; TextMeshPro uses the existing shared font material. Property blocks tint cues. A conservative editor estimate includes all cue paths, ticks, panels and text allowance and rejects more than 5,000 added triangles. Cues have no realtime lights, shadows, colliders or network components. Animation reuses point storage and property blocks. Runtime allocation and GPU costs still need profiling on Quest.

## Validation

Run **EditMode**, then **PlayMode with graphics enabled**. Never use `-nographics` for this project's PlayMode suite.

New tests cover first display, duplicate states, skipped steps, revert/reset/complete, the 12-second boundary, tool wording, two same-frame step changes, interrupted transitions and re-enabled panels. Preview-scene setup tests rebuild both headset scenes twice and check object counts, layers, colliders, text type and preservation of network objects. They do not save the preview scenes; the shared material may be created if absent.

Build with **Tools > DIYsect > Build Student APK**. Install `Builds/Android-Student/DIYsect-Student.apk` with `adb install -r`. Open the app alone and wait 15 seconds for practice mode. Follow [VR controls](VR_CONTROLS.md).

Record before/after headset screenshots and a short transition clip. Verify all seven steps, readable text from the table, correct cue placement, the 12-second hint, an early organ drop, reset, completion, late joining and disable/re-enable during inspection. Check frame rate and allocations during transitions. The existing phone, desktop and professor builds must remain healthy.

## Validation status for this branch

- Passed outside Unity: 121 compiled C# assertions across all 64 transition pairs, first-display snapshots, duplicate states, escalation boundaries and instructions.
- Not run here: Unity compilation, EditMode, PlayMode, the editor scene upgrade, either headset build or device profiling. No Unity installation was available in the standard locations on the development computer.
- Pending: saved scene/material changes, screenshots, transition clip and Quest acceptance. This work must remain draft until those checks are recorded.
