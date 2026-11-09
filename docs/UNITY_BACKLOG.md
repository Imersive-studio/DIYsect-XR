# Unity starter checklist

Role A: Aaditya. Role B: Saurav. Role C: Arjun. Setup tasks belong to all three. These tasks track the Unity human-anatomy work separately from the earlier browser backlog.

## 1. Create Unity 6 project from MR Multiplayer template and push to repo

Owner: all. Labels: unity-setup, role:all, setup.

Done when: Template Assets, Packages and ProjectSettings are committed; Unity opens without compile errors.

## 2. Enable OpenXR for Android and Mac; verify Meta XR Simulator runs on Mac

Owner: all. Labels: unity-setup, role:all, setup.

Done when: Both platform validations pass; document simulator and editor versions and attach a successful run screenshot.

## 3. Run Z-Anatomy fetch and organ extraction; commit GLBs via LFS

Owner: all. Labels: unity-setup, role:all, assets.

Done when: 22 organs, body wall, two skin flaps, manifest and previews exist; LFS pointers and triangle budgets pass validation.

## 4. MANUAL: download lecture hall and instruments from Sketchfab; run room simplification

Owner: all. Labels: unity-setup, role:all, assets.

Done when: Attributed room is at most 300k triangles; each instrument is at most 10k; source downloads stay untracked.

## 5. Two clients see the same body and the same organ positions

Owner: A. Labels: unity-setup, role:a, networking.

Done when: Two clients join one Netcode session and observe identical organ transforms after movement and reconnect.

## 6. Organ grab with network ownership transfer

Owner: A. Labels: unity-setup, role:a, networking, interaction.

Done when: Only one client can grab an organ at once; release and disconnect recover ownership.

## 7. Guiding hand: professor takes one DOF of the student's scalpel

Owner: A. Labels: unity-setup, role:a, interaction, core.

Done when: Professor controls cut depth while student steers; a visible indicator shows ownership and release restores control.

## 8. Lecture hall scene with dissection table, tray, instruments at correct scale

Owner: B. Labels: unity-setup, role:b, environment.

Done when: Student scene retains the template rig, room and props at verified meter scale; measured total scene remains below 750k triangles.

## 9. Midline incision: scalpel traces line, skin flaps swing open

Owner: B. Labels: unity-setup, role:b, dissection.

Done when: Incision progress follows the midline; both flaps open only after completion and state syncs to peers.

How a cut is judged (`IncisionRule`): the belly arches up to about 4 cm above the straight line between the cut's ends, and the glowing guide follows the skin. A blade point counts when it is within 4 cm of the midline sideways and its height lies between 4 cm below the straight line and 4 cm above the skin, so tracing the guide or pressing into the body both work. The skin points are stored on `DissectionProgress` and the trigger box is sized to hold the whole guide; both come from **Tools > DIYsect > Add Body Dissection**.

## 10. Open muscle wall; lift omentum with forceps

Owner: B. Labels: unity-setup, role:b, dissection.

Done when: Left/right rectus open after skin; forceps lift omentum; invalid layer-order actions are rejected.

## 11. Organ pick-up, label panel on grab, socket snap-back with ghost outline

Owner: B. Labels: unity-setup, role:b, interaction, quiz.

Done when: Grab displays the correct name; matching socket accepts the organ and nonmatching sockets reject it.

## 12. Professor MR scene: body on real table via passthrough

Owner: C. Labels: unity-setup, role:c, mr.

Done when: Quest 3 passthrough shows a correctly placed shared body with a usable table alignment workflow.

## 13. Classmate client: live view and question pins on desktop and Android

Owner: C. Labels: unity-setup, role:c, client.

Done when: Desktop and Android join the session; tapping an organ creates an organ-local question anchor.

## 14. Question pins sync to all devices; clusters glow

Owner: C. Labels: unity-setup, role:c, classroom, core.

Done when: Pins stay anchored to moving organs on every client; nearby pins cluster and reflect question count.

## 15. Live close-up camera to big screen via Render Texture

Owner: C. Labels: unity-setup, role:c, classroom.

Done when: Each client sees the synchronized body on its local screen without recursive rendering.

## 16. Demo video script, recording plan and code-explanation segment

Owner: C. Labels: unity-setup, role:c, video.

Done when: A 3–5 minute recording covers all three roles, guiding hand, pins and relevant code; backup footage and repo link are ready before Nov 9 noon.
