# Demo script (about 90 seconds) and checklists

One shared human body on a table. A professor in a Quest 3 (mixed reality), a student in a second Quest 3 (virtual lecture hall) and classmates on phones or a laptop. Everyone sees the same dissection, and each person keeps their own view.

## Who is who

| Person | Device | App | What they do |
|---|---|---|---|
| Professor (a judge) | Quest 3 | DIYsect Professor (`Professor_MR`) | Hosts the room, sees the class roster, points, draws a guide line, controls the cut depth, opens the student's view |
| Student (a teammate) | Quest 3 | DIYsect Student (the shared `Professor_MR` scene with the `.student` role: lecture hall, VR rig) | Walks, picks up the scalpel and forceps, cuts, lifts organs |
| Classmates (teammates) | Phones or a laptop | DIYsect Classmate | Press Join the room, orbit and zoom the body, tap an organ for its name, press and hold an organ to pin a question, raise a hand, unmute |

Student controls are in `docs/VR_CONTROLS.md`. This is the target rehearsal sequence, not evidence that the combined demo has passed. Check [integration status](INTEGRATION_166.md) before installing builds: the shared-scene compatibility work must land before the three-device rehearsal.

## The 90-second demo

| Time | What happens | What you say |
|---|---|---|
| 0:00 to 0:10 | The judge puts on the professor Quest. The student is already in the lecture hall. Two phones are already in the room, and the roster shows them. | "In a headset the teacher cannot see the class, and students cannot say which part they mean." |
| 0:10 to 0:30 | The professor draws a short guide line. The student holds the scalpel with grip and begins the incision while the professor adjusts depth with the depth handle. The phones show the same cut. | "One shared body, but everyone has a private camera." |
| 0:30 to 0:50 | The skin opens automatically. The student touches the rectus and omentum with the forceps tip, then lifts an organ, holds it for inspection and puts it back. The checklist follows each step. | "The pointer shows up in each classmate's own view." |
| 0:50 to 1:10 | A classmate presses and holds on the liver (a quick tap only shows its name). The judge sees a question pin, the roster counts it, and a raised hand shows next to the classmate's name until the judge lowers it. | "The teacher sees what was asked, and where." |
| 1:10 to 1:25 | The judge opens the student's view in the floating window, presses **Answered** (the pin turns grey with `[x]` on every phone), and answers the classmate. Depth guidance was already demonstrated during the incision. | "No guessing which part they meant, and everyone sees it was answered." |
| 1:25 to 1:30 | Close. | "Shared state, private views, and the interface that connects them." |

Say the contribution out loud: **techniques for keeping a headset user and phone users aware of each other**, not "human anatomy".

## Before every demo

Builds (make them the evening before, from the editor menu so a graphics device is present):

- [ ] One command for all three: **Tools > DIYsect > Build All Demo APKs** (stops at the first failing build and names it; writes `Builds/DemoBuilds.txt` with the commit, date, and APK sizes). Or build them one by one:
- [ ] **Tools > DIYsect > Build Professor APK** (`Builds/Android-Professor/DIYsect-Professor.apk`)
- [ ] **Tools > DIYsect > Build Student APK** (`Builds/Android-Student/DIYsect-Student.apk`)
- [ ] **Tools > DIYsect > Build Classmate Phone APK** (`Builds/Android-Classmate/DIYsect-Classmate.apk`)
- [ ] Install each with `adb install -r <apk>` (the Unity-bundled `adb` is under `PlaybackEngines/AndroidPlayer/SDK/platform-tools`)
- [ ] Both headsets and the phones were built from the same commit (`Builds/DemoBuilds.txt` shows it when you used Build All)

Room and devices:

- [ ] Both Quests charged, controllers charged, guardian area set, room lit, space to walk
- [ ] Wear the headset when launching. A headset that is not worn shows the lock screen and the app will not start
- [ ] Do not leave a USB cable in while testing: the Meta Link prompt takes over the headset
- [ ] Open the **professor app first**. It hosts the room. Then the student app, then the phones
- [ ] The professor's roster panel shows the **join code** and the `diysect://join` link. A phone opens on a start screen: press **Join the room**, or type the code on that screen and press Join. A phone that opened the invite link joins straight away
- [ ] Phones show "Connected" in the corner line, and the professor roster lists them
- [ ] Phones stay on screen (a phone in the background loses its connection)
- [ ] Wi-Fi: all devices on one network with internet (the room is made through Unity Gaming Services, not the local network)
- [ ] Someone stands next to each headset user (safety, and to hand over the controllers)
- [ ] Laptop open on `Classmate_Desktop` as a fallback classmate
- [ ] Backup video on the laptop and on a phone

Code health:

- [ ] EditMode and PlayMode tests pass on the commit you built (run EditMode and then PlayMode, back to back)

## Backup video checklist

- [ ] Professor headset view recorded (Quest recording or casting)
- [ ] Student headset view recorded at the same moment
- [ ] Phone views recorded at the same moment
- [ ] Show the sequence cut, point, ask, answer
- [ ] Captions for each technique
- [ ] 3 to 5 minutes, names and affiliations on screen (contest requirement), under 300 MB

## If something breaks

| Problem | Do this |
|---|---|
| The app opens to a lock screen or does nothing | Put the headset on, wake it, launch again from Library > Unknown Sources |
| The student cannot find the room | Relaunch the professor app first, wait 10 seconds, then relaunch the student |
| A phone does not join | Keep the app open and in front. Wait for the corner line to read "Connected", or type the join code from the professor's roster panel |
| "No room found with that code" | The room closed or the code was misread. Read it again from the roster panel |
| A tool will not grab | Point at it (the ray ends in a ball) and squeeze the grip, or press A (right) or X (left). The trigger does not grab. It turns yellow when it can be grabbed |
| The student is stuck in the app | Left menu button (or the right B button), then Quit, then Yes, quit |
| A classmate has lost the body on screen | Tap **Reset view** (top right), or press Home or R on a laptop |
| The judge drew a guide line and clicked a thumbstick (recenter) | That ends the stroke on purpose (the table moved). Draw it again |
| Wi-Fi is bad | Use a phone hotspot for every device, or play the backup video |
| Everything fails | Play the backup video and run the laptop classmate beside it |

## Still unverified (as of 9 November 2026)

- Two Quests together in one room. The professor and student apps have been built, but not yet run side by side.
- A pin placed by a real finger on a phone reaching the professor's view.
- The join code box and the `diysect://join` link on a real phone (the APK manifest contains the link alias; no device test yet).
- The **Reset view** button tapped by a real finger, and the **Answered** button pressed in a real headset (both pass the editor tests only).
- On a real phone: the start screen, the microphone permission dialog and hearing a classmate through Vivox after Unmute, and how the half-second hold feels under a finger.
- Bare hand pinch grab on the student headset. Controllers are the safe path for the demo.
- Organ cards and the immersive checklist are placed in the headset scenes on main. Readability, correct target cues and the complete flow still need verification on the final shared-scene build.

Update this list after each real test. Record all five rehearsals in [the integration ledger](INTEGRATION_166.md), including exact commit, APKs, devices and failures. A client rejoin and a host restart are different checks: do not claim that restarting the host restores the previous lesson.
