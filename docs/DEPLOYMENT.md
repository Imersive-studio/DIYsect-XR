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

`Builds/Android/DIYsect.apk` inside the project folder. The earlier build was 90 MB, with 5 warnings and a licensing handshake error in the log, and it still completed (`docs/SETUP_STATUS.md`).

### Install on the headset

The repository does not describe the install step and it has not been done yet. The usual method is:

1. Turn on developer mode for the Quest and connect it with the USB-C cable. Accept the "Allow USB debugging" prompt inside the headset.
2. Run `adb install -r Builds/Android/DIYsect.apk`. (Usual method. `adb` comes with the Android SDK that Unity installs; Meta Quest Developer Hub can do the same job but is not mentioned in this repository.)
3. Start the app from **Library > Unknown Sources** in the Quest. (Usual Quest behaviour, unverified here.)
4. For the Professor_MR scene, first run **Space Setup** on the Quest and mark your real table, then accept the scene permission on first launch (`docs/MR_TABLE_ALIGNMENT.md`).

Mac note from the README: Meta Horizon Link is Windows only. From a Mac, build a standalone APK and install it, as above.

## 5. Joining a session with several devices

What the repository says:

- The project keeps the template's Netcode for GameObjects and Unity Gaming Services packages (README step 3). The installed packages include Authentication, Core, Multiplayer Services and Vivox (see section 7).
- The scenes come from the Mixed Reality Multiplayer Tabletop template. The intent is to use the template's session flow (one client creates a session, the others join it); the repository does not confirm how this is wired in the scenes. `docs/ARCHITECTURE.md` says the server validates dissection transitions and ownership requests and that organ poses and layer state are shared.
- Unity project credentials must be set up by a person in Unity's own UI and never committed (README step 9).

What is **unverified or not written down**:

- Whether the Unity Gaming Services project is linked on your machine. `docs/SETUP_STATUS.md` lists "the Unity Gaming Services project link" as not tested. The project file has a cloud project id, but the repository does not say whether it is the team's own project or the template's leftover.
- The exact join steps (session name, join code, or a list shown in the app). The repository has no click-by-click steps. Ask Aaditya (networking owner) to add them here.
- Any test with two or more devices. None has been done.

When someone does a multi-device test, use the checklist in `docs/ARCHITECTURE.md` ("Two clients agree on organ transforms, ownership and dissection layer state") and the device test in `docs/MR_TABLE_ALIGNMENT.md`.

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
| Editor batch tests | Passed | PlayMode 60 of 60 and EditMode 54 of 54 passed in batch mode on Unity 6000.3.28f1 on 7 November 2026 (result supplied by the team; no test report file is stored in the repository). |
| XR validation (editor) | Passed | 0 OpenXR Project Validation issues and 0 XR Core Utils issues for Android and desktop, from **Tools > DIYsect > Report XR Validation** (`docs/SETUP_STATUS.md`). |
| Quest APK build | Built, not installed | ARM64 IL2CPP development build, 90 MB, built in batch mode (`docs/SETUP_STATUS.md`). Never installed on a headset. |
| Meta XR Simulator | Not run | Not installed on the setup machine; no run and no screenshot (`docs/SETUP_STATUS.md`). |
| Quest 3 hardware | Not tested | No hardware test yet. The device test in `docs/MR_TABLE_ALIGNMENT.md` is written but not done; the acceptance gate in `docs/ARCHITECTURE.md` is still open. |
| Two-device multiplayer | Not tested | No multi-device session test; the Unity Gaming Services project link is also untested (`docs/SETUP_STATUS.md`). |
| Android phone classmate | Not tested | The README names Android phones as classmate devices, but the repository has no Android phone build steps, and the only Android build is the Quest APK. Unverified. |

Read this table as: the code compiles, builds and passes its automated tests; nothing has yet been seen running on a headset or across two devices.

## Open questions for the team

1. Exact steps for Quest developer mode and install (and whether Meta Quest Developer Hub is used).
2. Join steps for a session and which Unity Gaming Services project is linked.
3. `.gitignore` already ignores the `Builds/` folder (`[Bb]uilds/`), so the APK is never committed. Keep it that way: the README forbids files over 100 MB.
4. How an Android phone classmate is built and installed, if at all.
