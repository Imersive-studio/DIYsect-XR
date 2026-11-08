# Deployment and OpenXR notes

How to get the project running, build it for Quest 3 and install it. Written for teammates who are new to Unity and for contest judges. Every fact comes from a file in this repository. Where the repository says nothing, this page says "unverified" or "usual method" so nobody mistakes a guess for a tested step.

Last checked against the repository on 7 November 2026.

## 1. What you need

| Item | Detail | Source |
|---|---|---|
| Unity editor | **6000.3.28f1** exactly (revision 8f082d2fe476). Use the same version as the team. | `ProjectSettings/ProjectVersion.txt` |
| Unity Hub | Needed to install and open the editor. | README step 1 |
| Editor modules | **Android Build Support**, with **SDK/NDK tools** and **OpenJDK**. | README step 1 |
| Quest headset | Meta Quest 3 for the Professor_MR and LectureHall_VR scenes. | README, `docs/SETUP_STATUS.md` |
| USB-C cable | Connects the Quest to the computer for installing. | Usual method, unverified in the repo |
| Meta Quest developer mode | Must be switched on for the headset before an APK can be installed. | Usual method, unverified in the repo. The repository has no steps for it. Ask the team to confirm the exact account and phone app steps. |
| Git and Git LFS | Models and images are stored with Git LFS. | `.gitattributes`, README step 5 |
| Python 3.13 | Only for rebuilding assets (section 6). | README step 5 |

The Android platform settings in the project are: minimum and target API level 34, ARM64, IL2CPP (`ProjectSettings/ProjectSettings.asset`, `Assets/Editor/BuildAndroid.cs`).

## 2. Get the project

1. Install Git LFS. On a Mac: `brew install git-lfs`.
2. Clone the repository: `https://github.com/Imersive-studio/DIYsect-XR` (the issues link in the README points to this repository).
3. Inside the cloned folder run `git lfs install --local`, then `git lfs pull`. The `git lfs pull` command is the usual way to fetch the large files; the README only lists `git lfs install --local`.
4. Open Unity Hub, choose **Add**, and pick the cloned folder.
5. Install editor **6000.3.28f1** when Hub offers it, with the modules from section 1.
6. Open the project. The first import takes a long time because Unity converts every model and package. Wait until the progress bar finishes.
7. Check the Console window for red errors. There should be none (this is the first acceptance gate in `docs/ARCHITECTURE.md`).

Never commit credentials. `SourceAssets/` and raw `.blend` files are not in the repository.

## 3. Run in the editor

The three scenes are in `Assets/Scenes/`. They are also in the build list (`ProjectSettings/EditorBuildSettings.asset`), together with `SampleScene` and the template's `SlicesChess` scene.

| Scene | Role | Device it is for |
|---|---|---|
| `LectureHall_VR` | Student, in the virtual lecture hall, holds the scalpel and forceps | VR headset |
| `Professor_MR` | Professor, mixed reality with passthrough, lines the virtual table up with a real one (see `docs/MR_TABLE_ALIGNMENT.md`) | Quest 3 |
| `Classmate_Desktop` | Classmate, asks questions by pinning them to the anatomy | Laptop (the README also names Android phones; see section 8) |

To try a scene:

1. Double-click the scene in the Project window.
2. Press the Play button at the top.

Meta XR Simulator notes from the README:

- The simulator is for testing on a Mac with **Metal or Vulkan**. It does not replace a test on a real Quest.
- Follow Meta's current standalone-runtime instructions at the link in README step 4. Older package-based instructions may differ.
- `docs/SETUP_STATUS.md` records that the simulator is **not installed yet** on the setup machine. Its Homebrew formula (`brew tap Oculus-VR/tap`, `brew install meta-xr-simulator`) asks you to accept Meta's license and needs `sudo`, so the owner must do it. No simulator run exists yet.
- Separately, the project has an "XR Simulation" loader on desktop (see section 7). That is Unity's own editor simulation, not the Meta XR Simulator.

Menu items for checking the XR setup: **Tools > DIYsect > Report XR Validation** and **Tools > DIYsect > Fix XR Validation Issues** (`Assets/Editor/XrValidationReport.cs`).

## 4. Build and install on Quest 3

### Build

From the editor:

1. Open the project.
2. Choose **Tools > DIYsect > Build Quest APK**.
3. Wait. The setup machine needed about 8 minutes, including the switch to the Android platform (`docs/SETUP_STATUS.md`).

The menu item builds every scene that is ticked in the build list (currently five scenes), as an ARM64, IL2CPP **development** build.

From a terminal, with the editor closed (batch method in `Assets/Editor/BuildAndroid.cs`):

```bash
<path to Unity 6000.3.28f1>/Unity -batchmode -quit -projectPath . -executeMethod DIYsect.Editor.BuildAndroid.BuildBatch
```

The method exits with code 0 on success and 1 on failure. The `-batchmode -quit -projectPath` flags are standard Unity command line options and are not recorded in the repository; the Unity binary path depends on your install. The APK build was run in batch mode before (`docs/SETUP_STATUS.md`), but the exact command line used is not recorded. Treat the line above as the usual method and check it on your machine.

### Where the APK lands

`Builds/Android/DIYsect.apk` inside the project folder. The first build (7 November 2026) was 90 MB. The build of 8 November 2026 from `main` is **158 MB** (the realistic body, drapes and the 246k-triangle lecture hall are in it now). The classmate phone APK, `Builds/Android-Classmate/DIYsect-Classmate.apk`, is **84 MB**. Both are ARM64 IL2CPP builds; the Quest one is a development build and the phone one is not (`docs/SETUP_STATUS.md`).

### Install on the headset

The repository does not describe the install step and it has not been done yet. The usual method is:

1. Turn on developer mode for the Quest and connect it with the USB-C cable. Accept the "Allow USB debugging" prompt inside the headset.
2. Run `adb install -r Builds/Android/DIYsect.apk`. (Usual method. `adb` comes with the Android SDK that Unity installs; Meta Quest Developer Hub can do the same job but is not mentioned in this repository.)
3. Start the app from **Library > Unknown Sources** in the Quest. (Usual Quest behaviour, unverified here.)
4. For the Professor_MR scene, first run **Space Setup** on the Quest and mark your real table, then accept the scene permission on first launch (`docs/MR_TABLE_ALIGNMENT.md`).

Mac note from the README: Meta Horizon Link is Windows only. From a Mac, build a standalone APK and install it, as above.

## 5. Joining a session with several devices

How it works (checked in the code and in a run on 8 November 2026):

- The scenes use the Mixed Reality Multiplayer Tabletop template's Unity Gaming Services flow: sign in anonymously, then **create** or **join** a lobby, and the players connect through Relay with Netcode for GameObjects. One client is the host and acts as the server (it validates dissection steps, ownership and question pins).
- **All three scenes share one lobby.** The template filtered lobbies by the active scene's name, so a professor in `Professor_MR`, a student in `LectureHall_VR` and a classmate in `Classmate_Desktop` could never see each other. `LobbyManager.k_SharedSceneKey` ("DIYsect") is now stored in every lobby and used in every filter. The build version must still match (both sides built from the same project settings), and editor-created lobbies are visible to builds.
- **Room size is 8 players** (`XRINetworkGameManager.maxPlayers`; the template had 4).
- **The classmate never hosts.** `ClassmateAutoJoin` (on the network manager in `Classmate_Desktop`) looks for an existing lobby every 5 seconds once signed in and joins the first one. It never creates a lobby, so the professor's headset stays the host. The phone has no menu; a line in the corner shows the state (`ClassmateStatusLabel`: "Signing in…", "Looking for the professor's room…", "Joining the room…", "Connected to … · N in the room").
- Maximum players, the lobby key and the auto join are in `Assets/XRMP/Scripts/Network/NetworkManagers/`.

Unity Cloud project (linked in `ProjectSettings`, not a secret): project **DIYsect-XR**, organization `arjungyawali001`, id `15cf1a93-d87c-41f9-ba01-1b3c6768a81c`. To use it in the editor, open **Edit > Project Settings > Services** and link that project; if it is missing from the list, the organization owner must add your account as a member. Authentication, Lobby and Relay worked from the editor and from an Android phone on 8 November 2026. Vivox voice chat also logged in and joined its channel from the phone once the project's Vivox settings (server, domain and issuer; the token key is empty) were saved in `ProjectSettings/Packages/com.unity.services.vivox/Settings.json`. Never commit a token key.

### Host a room without a headset (Mac editor)

The headset scenes only offer a VR menu to create a lobby, which the editor cannot click, so use the editor menu instead:

1. Open the project with Unity 6000.3.28f1 and check the project is linked (above).
2. Open `Assets/Scenes/Classmate_Desktop.unity` and press **Play**. (It runs without a headset. `Professor_MR` needs an XR runtime and has not been tried on a Mac.)
3. Wait until the Console shows "Initializing Voice Chat" (signed in; the Vivox warnings are expected without voice setup).
4. Choose **Tools > DIYsect > Host Test Lobby (Play Mode)**. The item is greyed out until the scene has signed in. The log shows `Created Lobby with Join Code: …` and `Connected to game session. Lobby: DIYsect test room.`
5. Open **DIYsect Classmate** on the phone and keep it on screen. Within about 5 seconds it logs `[ClassmateAutoJoin] Joining lobby DIYsect test room.` and the editor server logs `Pending Client-1 connection approved!`.

A phone that goes to the background loses its connection (the host logs a protocol timeout), so keep the app open during a test.

### Classmate controls (phone and laptop)

- Drag one finger (or hold the mouse button) to orbit the body; pinch (or scroll) to zoom.
- **Tap** an organ to pin a question on it. The pin is placed when the finger lifts, and only if it did not move, so a drag never drops a pin. A second finger cancels the tap.

### The professor's class roster

`Professor_MR` has a **ClassRoster** panel on the professor's right (opposite the student view window). It lists every student in the room: name, "headset" or "phone/laptop", and how many questions they have pinned. A student's name can show a moment after they join. Rebuild it with **Tools > DIYsect > Add Class Roster**. It shows who is connected and what they asked; it does not show where students are looking from.

What is **still unverified**:

- A pin placed by a finger on the phone reaching the host and appearing in the professor's view (an `adb` tap did nothing; a real tap has not been checked).
- The roster and the student view window on a real Quest.
- Joining from a Quest. The headset scenes use the same lobby key, but no headset has joined a room.

For a multi-device test, use the checklist in `docs/ARCHITECTURE.md` ("Two clients agree on organ transforms, ownership and dissection layer state") and the device test in `docs/MR_TABLE_ALIGNMENT.md`.

## 6. Rebuilding the assets

You only need this if you change the anatomy, skin, drapes, lamp, room or instruments. The finished files are already in the repository.

- README step 5: install the Python environment and run the Blender scripts for organs, skin, drapes, lamp and previews.
- README step 6: download the lecture hall and instrument set from Sketchfab by hand (login needed), then run `simplify_room.py` and `extract_instruments.py`.
- README step 8: run **Tools > DIYsect > Create Classroom Props**, then **Tools > DIYsect > Apply Realistic Look**. Rerun Apply Realistic Look after re-exporting the skin, drapes or lamp.
- To refresh the preview pictures, use **Tools > DIYsect > Capture Scene Previews** (needs graphics, so do not use `-nographics`).

Source licenses are in `docs/CREDITS.md`.

## 7. OpenXR statement

DIYsect uses the OpenXR standard through Unity's OpenXR plug-in **1.17.1** (`com.unity.xr.openxr`, with Meta OpenXR **2.5.1**, XR Management **4.6.1**, XR Interaction Toolkit **3.5.1** and AR Foundation resolved to **6.5.0**). It targets two things: Quest 3 as a standalone Android app (ARM64, IL2CPP, Vulkan, API level 34) and a desktop/Mac editor and build with OpenXR plus Unity's XR Simulation loader (Metal). On Android the loader is OpenXR only, with the features Meta Quest support, the Oculus Touch and Meta Quest Touch Plus controller profiles, hand tracking with Meta hand tracking aim, boundary visibility, composition layers, display utilities, and the AR Foundation session, camera, plane, raycast, anchor, bounding box, mesh and occlusion features; the Touch Pro profile is off. On desktop the enabled OpenXR features are the Oculus Touch controller profile, hand tracking with Meta hand tracking aim, composition layers, boundary visibility and the AR Foundation session, camera and anchor features. The only vendor-specific layer beyond core OpenXR is Unity's Meta OpenXR package (`com.unity.xr.meta-openxr`), which supplies the Meta Quest features above; the repository contains no Meta XR SDK, Oculus Integration or other vendor SDK in the manifest. Passthrough, plane detection and anchors come through AR Foundation and the Mixed Reality Multiplayer Tabletop template (template version 2.0.0), not through custom runtime code. Limits: other OpenXR headsets are untested and not a goal, and the project turns on Meta-specific features, so a non-Meta runtime may lack passthrough, planes or hand tracking; the Android XR packages (`com.unity.xr.androidxr-openxr`) are installed from the template but their features are off; the Meta XR Simulator, a real Quest 3 and multi-device play have not been tested (section 8); and plane detection and the scene permission flow on a real Quest are unverified (`docs/MR_TABLE_ALIGNMENT.md`).

Sources: `Packages/manifest.json`, `Packages/packages-lock.json`, `Assets/XR/XRGeneralSettingsPerBuildTarget.asset`, `Assets/XR/Settings/OpenXR Package Settings.asset`, `docs/SETUP_STATUS.md`.

Package versions in use (`Packages/manifest.json`, with the lock file checked):

| Package | Version |
|---|---|
| `com.unity.xr.openxr` | 1.17.1 |
| `com.unity.xr.meta-openxr` | 2.5.1 |
| `com.unity.xr.management` | 4.6.1 |
| `com.unity.xr.interaction.toolkit` | 3.5.1 |
| `com.unity.xr.arfoundation` | 6.4.3 in the manifest, 6.5.0 resolved in the lock file |
| `com.unity.netcode.gameobjects` | 2.13.3 |
| `com.unity.cloud.gltfast` | 6.20.0 |
| `com.unity.services.core` | 1.18.0 |
| `com.unity.services.authentication` | 3.8.1 |
| `com.unity.services.multiplayer` | 2.4.1 |
| `com.unity.services.vivox` | 16.11.0 |
| `com.unity.multiplayer.playmode` | 2.0.2 |

## 8. Verification status

| Check | Status | Evidence |
|---|---|---|
| Editor batch tests | Passed | EditMode 113 of 113 and PlayMode 93 of 93 passed in batch mode on Unity 6000.3.28f1 on 8 November 2026. Run EditMode and then PlayMode back to back; a PlayMode run on its own sometimes aborts with "Playmode tests were aborted because the player was stopped" (Unity's tutorial window stops play mode) and passes when run again. No test report file is stored in the repository. |
| XR validation (editor) | Passed | 0 OpenXR Project Validation issues and 0 XR Core Utils issues for Android and desktop, from **Tools > DIYsect > Report XR Validation** (`docs/SETUP_STATUS.md`). |
| Quest APK build | Built, not installed | ARM64 IL2CPP development build, 158 MB on 8 November 2026 (90 MB on 7 November), built in batch mode (`docs/SETUP_STATUS.md`). Never installed on a headset. |
| Meta XR Simulator | Not run | Not installed on the setup machine; no run and no screenshot (`docs/SETUP_STATUS.md`). |
| Quest 3 hardware | Not tested | No hardware test yet. The device test in `docs/MR_TABLE_ALIGNMENT.md` is written but not done; the acceptance gate in `docs/ARCHITECTURE.md` is still open. |
| Two-device multiplayer | Partly tested | 8 November 2026: the Mac editor hosted a lobby and an Android phone joined it over Relay (both logs agree: `Pending Client-1 connection approved!` on the host, `Connected to game session` on the phone). Authentication, Lobby, Relay and Vivox worked. A pin from the phone and any headset in the room are not tested. |
| Android phone classmate | Partly tested | `Tools > DIYsect > Build Classmate Phone APK` (84 MB, minimum Android 10) installed and ran on an LG LM-G900TM with Android 12 on 8 November 2026: it signs in, joins the room and joins the voice channel. Touch orbit and the status line were added afterwards and have not been run on the phone. Pins from a finger: unchecked. |

Read this table as: the code compiles, builds and passes its automated tests; nothing has yet been seen running on a headset or across two devices.

## Open questions for the team

1. Exact steps for Quest developer mode and install (and whether Meta Quest Developer Hub is used).
2. (Answered, see section 5.) Which headset hosts: the join code is created by whoever makes the lobby first; a headset has to create it from the template's VR menu, which has not been tried.
3. `.gitignore` already ignores the `Builds/` folder (`[Bb]uilds/`), so the APK is never committed. Keep it that way: the README forbids files over 100 MB.
4. (Answered.) The phone APK is built with `Tools > DIYsect > Build Classmate Phone APK` and installed with `adb install -r Builds/Android-Classmate/DIYsect-Classmate.apk` (USB debugging on; the Unity-bundled `adb` is under `PlaybackEngines/AndroidPlayer/SDK/platform-tools`).
