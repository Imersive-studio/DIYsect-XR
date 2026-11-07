# Table alignment in Professor_MR

The professor lines the virtual table, and the body on it, up with a real table in Quest 3 passthrough. This is what the code does and the steps to try it on a headset. Nothing here has been run on a headset yet.

## How it works

`TableAlignment` (assembly `DIYsect.MixedReality`) moves the **DIYsect root** of the Professor_MR scene on the professor's headset only. The table, tray, screen, close-up camera and the body move together, so the body keeps its place on the table.

Why moving the root is safe for the shared session:

- The body is one NetworkObject. Only the organs carry a NetworkTransform, in **local space** (relative to the body), the scalpel's NetworkTransform is also in local space under the body, and pins are stored as organ-local points. The root has no NetworkTransform, so its pose is never sent.
- Each client therefore places its own copy of the scene wherever its table is, and every client still sees the same body. PlayMode tests prove this with each peer re-rooted to a different place.
- Do not add a NetworkTransform to the root or to the body root. That would make the alignment fight the network.

Template flow: the Tabletop template's `TableManipulator` grabs a virtual table and moves the player rig the other way, which needs its seat system and shifts the networked avatar. It also has no plane detection. It is left untouched and its table system stays off in Professor_MR. The new handle uses the same idea (grab a table-shaped thing and carry it onto the real table) but moves the content instead of the player. The template does ship the `ARPlane` prefab and the scene-permission component, and both are reused.

Two ways to place:

1. **Detected table.** When the headset reports a horizontal plane labelled Table (from Space Setup), the root is placed on the nearest one once. The virtual table is then hidden, so only the body shows on the real table.
2. **Alignment handle.** A small cyan disc floats beside the near edge of the virtual table. Grab it and carry the virtual table over the real one. While held, the virtual table and the detected planes are visible. On release:
   - the table top snaps to a detected level surface within 25 cm above or below it. With no detected plane it stays at the height you carried it to, so put the disc down on the real surface to set the height;
   - the table turns to face you, unless you twisted the disc by more than 15 degrees, in which case your heading is kept;
   - the virtual table hides, and the disc parks beside the table again.

The root only slides across the floor plane and turns about the up axis, so it always stays upright. The poses come from `TableAlignmentMath`, which has EditMode tests. Nothing is saved between runs.

## Before a headset session

1. Build the scene content once: Tools > DIYsect > Create Scenes, then Add Table Alignment. The second is safe to repeat.
2. On the Quest, run **Space Setup** (Settings > Environment setup) and mark your real table, so the headset has a Table plane. Without it only the handle works.
3. Build and install the Professor_MR scene as in the README. The first launch asks for the scene permission; accept it.

## Device test (about 5 minutes)

1. Start Professor_MR in passthrough. Expected: the room is visible, the virtual table and body appear in front of you. If the detected-table flow worked, the body is already on your real table and the virtual table is hidden.
2. Look for the cyan disc beside the virtual table, if the table is still visible. Pinch or grip it. Expected: the virtual table and the plane overlays appear and follow your hand across the floor.
3. Carry the table over the real one and let go. Expected: the table top sits flush on the real surface, the virtual table disappears, and the body lies on the real table facing you.
4. Check the height: touch the real table next to the body. Expected: the body rests on the surface, not above or inside it. If it is off by more than 2 cm, note the number.
5. Twist the disc by about 45 degrees and release. Expected: the body keeps the new heading instead of turning back to face you.
6. Grab an organ, move it, release it, and place a question pin with a second headset or the desktop client. Expected: all of it works the same after moving the table, and the other client sees the organ in the same place on the body.
7. Move the table again, then walk around the real table. Expected: the body stays still in the room.

## What to report

For each step, note pass or fail, and for any fail:

- the step number and what you saw instead;
- whether Space Setup was done and the table was marked;
- whether the plane overlays appeared;
- the height error in centimetres and the heading error in degrees, if any;
- the Quest software version and a short video if possible.

Post this list as a comment on issue 74. Do not close the issue until steps 3, 4 and 6 pass.

## Not verified yet

- The scene permission flow and plane detection on a Quest 3. Planes are only switched on after the permission is granted, from the app regaining focus. If no planes ever appear, the handle still works by hand.
- That Meta labels the table plane as Table and that a raycast down through the table top hits it (the code relies on AR Foundation's fallback ray cast against detected planes).
- Hand pinch and controller grab on the disc. Batch tests drive it through the interaction manager, not a real hand.
- Comfort and legibility of the disc and overlays in passthrough.
