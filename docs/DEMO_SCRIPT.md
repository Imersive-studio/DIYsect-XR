# Demo script (3 to 5 minutes) and checklists

The demo shows one real class: the **professor** in a Quest 3 mixed-reality headset hosting the room, and
**classmates** on phones and laptops who join by a code or link and pin questions on the shared body.

## The demo, with the code segment

| Time | What happens | What you say |
|---|---|---|
| 0:00 to 0:20 | Professor puts on the Quest. The app opens in the room and looks for a class. | "In a headset the teacher can't see the class, and students can't say which part they mean." |
| 0:20 to 0:50 | The body appears on the table in front of the professor. The roster panel shows the **join code** and `diysect://join` link. | "The professor hosts the room. Students join with a code, like a video call." |
| 0:50 to 1:20 | A teammate types the code (or taps the link) on a phone. The status line reads "Connected · 2 in the room". The roster shows the student and "phone/laptop". | "No install steps for the student beyond the app, no menus, one code." |
| 1:20 to 2:00 | The professor takes the skin and muscle layers off and walks around the table (thumbstick). The phone's body updates. The phone orbits its own camera. | "One shared body, but every student has a private camera." |
| 2:00 to 2:40 | The student taps the liver. The professor sees a question pin on the liver and the roster shows "1 question". | "The teacher sees what was asked, and where." |
| 2:40 to 3:00 | Professor answers the quiz round for the class (QuizPanel). | "The same room runs a quiz." |
| 3:00 to 4:00 | **Code segment** on screen (see below). | |
| 4:00 to 4:20 | Close. | "Shared state, private views, and the interface that connects them." |

Say the contribution out loud: **techniques for keeping a headset user and phone users aware of each other**,
not "frog dissection".

## Code segment (about one minute, show these files)

1. `ClassroomRole.cs`: one shared scene, `Professor_MR`. The role decides which objects and behaviours are on, so
   every device loads the same scene and Netcode for GameObjects can sync it.
2. `ProfessorAutoHost.cs` and `RoomJoiner.cs`: the headset looks for a room, otherwise creates one through Unity
   Gaming Services (Lobby and Relay).
3. `RoomInvite.cs`: the join code and `diysect://join?code=` link, with tests in `RoomInviteTests.cs`.
4. `QuestionBoard.cs`: a pin is placed by the student, checked by the host and shown to everyone.

## Before every demo
- [ ] Quest and phone APKs rebuilt from `main` (`docs/DEPLOYMENT.md` section 4) and installed
- [ ] Professor APK is not a development build (a development build crashes hosting)
- [ ] Quest charged, guardian area set, controllers charged, room lit, a table in front of the headset
- [ ] Phones signed in and showing "Looking for the professor's room…" or ready to type the code
- [ ] Professor's app already showing the join code
- [ ] Backup video on the laptop and on a phone
- [ ] Both test suites pass on the commit being demoed (EditMode, then PlayMode)
- [ ] Someone stands next to the headset user (safety, and to hand over the controllers)

## Backup video checklist
- [ ] Headset view recorded (Quest recording or casting)
- [ ] Phone views recorded at the same moment
- [ ] Show the sequence host, join with code, cut, ask, answer
- [ ] Names and affiliations on screen, 3 to 5 minutes
- [ ] The code segment recorded with the files above

## If something breaks
| Problem | Do this |
|---|---|
| Phone does not join | Check the phone is on the same app version as the headset, type the code, wait 15 seconds |
| Code is rejected ("No room found") | The room closed or the code is wrong; read the code again from the professor's panel |
| Headset can't host | Host from the Mac editor (`Tools > DIYsect > Host Test Lobby`) and show the headset video |
| Wi-Fi is bad | Use a phone hotspot for both devices, or play the backup video |
