# Setup verification

Local preparation completed on November 7, 2026:

- Unity Hub installed. The Unity editor, license activation and template project remain manual.
- Git LFS installed and configured locally; binary asset extensions tracked.
- Python 3.13 environment with `bpy==5.2.3` installed.
- Listed Z-Anatomy archive downloaded; source revision and SHA-256 recorded in `Assets/Models/Anatomy/SOURCE.json`.
- 86 GLBs: 22 organs, 62 body-wall/chest surfaces and two prototype skin flaps. Total: **310,416 triangles**.
- GLB headers, actual triangle indices, manifest counts, per-piece limits and combined budget validated with `python3 Tools/blender/validate_assets.py`.
- Synthetic mesh test passed: transform retention, GLB export and room simplification to 98 triangles under a 100-triangle test budget.
- Front and three-quarter previews rendered on CPU and visually inspected. Export materials preserve source color controls with simplified shaders.
- Browser regression suite: **15/15 passed**.

## OpenXR, simulator and Android build (November 7, 2026)

Editor: Unity 6000.3.28f1. Template: MR Multiplayer Tabletop 2.0.0. OpenXR plugin 1.17.1, Meta OpenXR 2.5.1, XR Management 4.6.1.

Batch-mode editor validation (`Tools/DIYsect/Report XR Validation`, run with `-batchmode -nographics`):

- Android loader: OpenXR. Features include Meta Quest support, Oculus Touch and Meta Quest Touch Plus controller profiles, hand tracking and the AR Foundation camera, plane, raycast, anchor and mesh features.
- Standalone (Mac) loaders: OpenXR and XR Simulation. Graphics API pinned to Metal. Android uses Vulkan, ARM64 and IL2CPP, minimum API level 34.
- Result: 0 OpenXR Project Validation issues and 0 XR Core Utils issues for both targets. Before the change, the only error was the XR Simulation rule requiring the editor preference "Script Changes While Playing" to be "Recompile After Finished Playing"; this is a per-user editor preference, fixed with the validator's own action and not stored in the project.
- Touch Plus profile enabled for Quest 3 on Android; the Touch Pro profile stays off.

Meta XR Simulator on Mac: not installed. The official Homebrew formula (`brew tap Oculus-VR/tap`, `brew install meta-xr-simulator`) prints a notice that installing it accepts the Meta Platform Technologies SDK license, and its post-install step needs `sudo`. Both need the owner. No simulator run and no screenshot yet.

Android build (`Tools/DIYsect/Build Quest APK`, batch method `DIYsect.Editor.BuildAndroid.BuildBatch`): succeeded for the five scenes in the build list, ARM64, IL2CPP, development build. Output `Builds/Android/DIYsect.apk`, 90 MB, about 8 minutes including the switch to the Android platform. The build reported 5 warnings (TextMesh Pro compile-size notices among them) and logged a licensing handshake error; the build still completed.

Classmate phone build (`Tools/DIYsect/Build Classmate Phone APK`, batch method `DIYsect.Editor.BuildAndroidClassmate.BuildBatch`, run with `-buildTarget Android`): builds only `Classmate_Desktop` to `Builds/Android-Classmate/DIYsect-Classmate.apk` (88 MB, about 9 minutes, 14 warnings). For the build only, XR Initialize on Startup is off, the Android XR loaders are removed, the application id gets the `.classmate` suffix and the product name is "DIYsect Classmate"; these four are restored afterwards, so `Assets/XR/XRGeneralSettingsPerBuildTarget.asset` is unchanged and the Quest build is unaffected. The shared Android options (IL2CPP, ARM64, APK) are the same as the Quest build and stay set. Build Profiles are not used: the project has none. Not tested: installing on a phone. Gap: the classmate camera is fixed (no orbit by mouse or touch); a tap already places a pin because `PinPlacementInput` binds `<Pointer>`.

Close-up screen: all three scenes (LectureHall_VR, Professor_MR, Classmate_Desktop) already hold one `CloseUpScreen` and one local `CloseUpCamera` (1024x1024 `CloseUp.renderTexture`, not a NetworkObject, culling mask excludes the `CloseUpScreen` layer). Edit tests check this in each saved scene; a PlayMode test with a host and a client checks that moving an organ on the host moves it in a close-up-style view on the client (a stand-in camera with the same mounting). It would render every frame at 1024x1024 on Quest 3; not run on a device, and frame cost is not measured.

Not tested: a real Quest 3, the Meta XR Simulator, multiplayer and the Unity Gaming Services project link.

Not yet verified earlier in setup: performance and the Sketchfab room and instrument downloads, which need a human login. Room simplification passed the synthetic test; it has not been run on the lecture-hall asset.

Use the README's numbered setup and `ARCHITECTURE.md` acceptance checklist to complete these steps. Follow `WORKFLOW.md` for the parent branch, task branches and PR dependencies.
