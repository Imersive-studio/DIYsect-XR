# Screenshots and animations

Every file here is an **Editor render**, not a capture from a headset or a phone. They are drawn in the Unity editor from the shared scene (`Professor_MR`) with a local host, one fixed camera (1280 x 720, 45 degree field of view, same sky and light) and one caption tag in the bottom left. The pointer markers and labels use the pointer HUD's own shapes, colours and wording, redrawn on a canvas because the HUD itself cannot be drawn into an offscreen image. The dissection stage, pointers and pins are set through the same server code the app uses. The one exception is the guiding hand, where the ownership values are written on the server directly, because there is no second player in the editor.

Classmate-view images are marked "Classmate view" in the tag, professor-side images "Professor side". To refresh them run `Tools > DIYsect > Capture Submission Media`, or in batch mode (needs graphics) `Unity -batchmode -projectPath . -executeMethod DIYsect.Editor.Media.CaptureSubmissionMedia.RunBatch`. The tool writes the PNGs here and the frames of the animations to the folder in `DIYSECT_MEDIA_FRAMES`; the frames are joined into looping GIFs with Pillow.

## Screenshots

| File | Caption |
| --- | --- |
| `incision_line.png` | Editor render, classmate view. The midline incision drawn on the skin before it is opened. |
| `skin_flaps_open.png` | Editor render, classmate view. The two skin flaps open after the incision is complete. |
| `classmate_pointer_organ.png` | Editor render, classmate view. The professor points at the liver: the organ pulses in the professor's orange and the label reads "Professor: Liver". The liver was lifted clear of the ribs for this image so that it can be seen from above. |
| `classmate_pointer_edge_arrow.png` | Editor render, classmate view. The same pointer with the liver off screen: an orange edge arrow and the label point toward it. |
| `classmate_question_pins.png` | Editor render, classmate view. Five open questions on the liver form one glowing yellow cluster marked "5". A single open question is blue. A question the professor has answered has a grey label with "[x]". |
| `guiding_hand_depth.png` | Editor render, classmate view. The guiding hand: the depth ring on the scalpel is orange and the label reads "Depth: Professor" while the professor controls the depth. The scalpel is shown on its tray. |
| `professor_table_overview.png` | Editor render, professor side. The body on the table with the headset-only objects switched on: the student view window (left) and the roster (right). The camera is a free editor camera, not a headset view. |
| `professor_student_view.png` | Editor render, professor side. The student view window shows what a classmate saw when they asked "Where next?", with the "Answered" button under it. |

## Animations

| File | Caption |
| --- | --- |
| `skin_flaps_opening.gif` | Editor render, classmate view. The skin flaps swinging open once the incision is done (2.4 seconds, looping). |
| `classmate_pointer_pulse.gif` | Editor render, classmate view. The professor's pointer on the liver: the ring and the organ tint pulse once every 1.6 seconds (looping). |
| `question_pins_orbit.gif` | Editor render, classmate view. A full turn around the liver showing the glowing cluster, a single question and an answered question (looping). |

Not shown: the student's cyan pointer (diamond and chevron) and the scalpel being held, which need a second player.
