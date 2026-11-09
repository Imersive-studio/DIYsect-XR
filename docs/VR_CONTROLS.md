# Student VR controls (Quest)

## Before you start
Wear the headset standing. The eyes are fixed at 1.6 m, so you do not need to crouch or stretch. The height is set a couple of seconds after you put the headset on, and again after you recenter it (hold the Meta button). Controllers or bare hands both work.

## Move
The left thumbstick walks. The right thumbstick snap turns left and right. Push the right thumbstick up to raise your eyes and down to lower them. Crouching and standing up for real also move the view.

## See where you point
Every controller and hand shows a bold ray, at least 0.4 m long, with a small ball at its tip. A tool turns yellow when you point at it. (Far casting is switched on in the rig; the Mixed Reality template ships it off, and with it off no ray is drawn at all.)

## Pick up a tool
Point near the scalpel, forceps or depth handle (a hand's width is enough) and squeeze the grip or press **A** (right controller) or **X** (left controller), or pinch with bare hands. The trigger presses buttons on menus and does not grab. You feel a small tick when you point at a tool and a firmer pulse when you grab it.

## Menu and quit
Press the left menu button, the right secondary button (B), or Escape in the editor. A panel appears in front of you with Resume and Quit. Press Quit, then Yes, quit to leave. Press Stay to go back.

## If something looks wrong
Say what you see (no rays, no hands, tool will not grab) and which hand or controller you used, and write it in epic #124 on GitHub.

## Seated audience

In Unity, choose **Tools > DIYsect > Add Seated Audience** to place 40 classmates in the front rows of `LectureHall_VR`. Run the menu step after rebuilding or repositioning the hall, and save and commit the scene and `Assets/Materials/Audience` with its metadata. Running the step again on the same layout leaves the scene alone. The figures are local scenery and have no network identities.

For the demo check, start the student scene, stand at the table, and turn toward the front rows. Capture this view in Play Mode so the motion is visible. Walk around to check that the cutouts keep facing you. On Quest, check frame timing and walk past the front rows to confirm they stay clear of the table and aisles.
