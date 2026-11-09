# DIYsect

**One dissection, a whole class inside it, across mixed reality, VR, and phones.** A professor in Quest 3 mixed reality guides a student in a virtual lecture hall while classmates on laptops or Android phones pin questions to the same human anatomy. The demo follows a midline incision, opening the abdominal wall, lifting the omentum, and removing and replacing organs.

Built for the **Immersion Studio Hackathon, November 6–9, 2026**. Deadline: **Monday, November 9 at 12 PM** (confirm the event's timezone). Submit a **3–5 minute demo video**, including a code-explanation segment, and this repository link. Make small, frequent commits.

## Start Unity now

1. Install **Unity Hub** and a **Unity 6 LTS** editor (6000.0 or newer), including **Android Build Support, SDK/NDK tools and OpenJDK**. Use the same editor version across the team.
2. **MANUAL:** In Hub, create a project using **Mixed Reality Multiplayer Tabletop**. Create it in a temporary sibling folder, then copy its `Assets`, `Packages` and `ProjectSettings` into this existing clone, merging folders without replacing existing work. Keep this clone's `.git` folder. Add this clone to Hub and open it. Do not create another repository. See the [template documentation](https://docs.unity.cn/Packages/com.unity.template.mr-multiplayer@1.0/manual/index.html) and [VR multiplayer reference](https://docs.unity3d.com/Packages/com.unity.template.vr-multiplayer@2.1/manual/index.html).
3. Keep the template's XR Interaction Toolkit, Netcode for GameObjects, Unity Gaming Services and AR Foundation. In Project Settings > XR Plug-in Management, enable **OpenXR** for Android and desktop/Mac. Use OpenXR **1.13.0 or later**. Resolve Project Validation warnings and select Quest controller features. Keep passthrough setup in the professor scene.
4. Install and activate [Meta XR Simulator](https://developers.meta.com/vr/documentation/unity/xrsim-getting-started/) for Mac testing with **Metal or Vulkan**. Follow the current standalone-runtime instructions; older package-based instructions may differ. Simulator checks do not replace a Quest device test.
5. Prepare the asset tools from this repository's root:

   ```bash
   brew install git-lfs
   git lfs install --local
   python3.13 -m venv .venv
   .venv/bin/python -m pip install -r Tools/blender/requirements.txt
   bash Tools/blender/fetch_zanatomy.sh
   .venv/bin/python -I Tools/blender/extract_organs.py
   .venv/bin/python -I Tools/blender/make_skin.py
   .venv/bin/python -I Tools/blender/make_drapes.py
   .venv/bin/python -I Tools/blender/make_surgical_lamp.py
   .venv/bin/python -I Tools/blender/render_previews.py
   ```

   Raw downloads stay in ignored `SourceAssets/`. The Blender file's embedded scripts are disabled. The manifest records source object names, triangle counts and Blender world positions; GLBs retain anatomical placement and convert to Y-up. Do not center each organ separately. Measure scale against a meter cube before building the body prefab.
6. **MANUAL:** Sign into Sketchfab and download A2 and A3 below as GLB. Put the lecture hall in `SourceAssets/LectureHall/lecture_hall.glb` and the instrument set in `SourceAssets/Instruments/surgical_instrument.glb` (`SourceAssets/` is git-ignored; never commit the raw downloads). Then run:

   ```bash
   .venv/bin/python -I Tools/blender/simplify_room.py -- --input SourceAssets/LectureHall/lecture_hall.glb --budget 250000
   .venv/bin/python -I Tools/blender/extract_instruments.py
   ```

   `simplify_room.py` keeps small parts (walls, floor, screen, desks) intact and decimates only the parts above one shared per-object cap (the chair rows), then scales textures to 1024 px or less; the hall goes from about 1.21M to about 246k triangles. `extract_instruments.py` takes the thumb forceps and the scalpel handle out of the instrument set, models a No. 22 style blade into the handle, and writes `Assets/Models/Instruments/forceps.glb` and `scalpel.glb` with their origin on the working tip. A4 is an optional non-commercial backup, not a required download.
7. **MANUAL:** Verify GLB import support in the template before dragging models into scenes. Unity does not universally import GLB by default. If no compatible importer is installed, open the GLBs in Blender and export FBX while preserving transforms and materials; do not rename their extensions. TODO: record the team's chosen import method and version.
8. Run **Tools > DIYsect > Create Classroom Props** to create table, tray and screen prefabs. The command preserves existing prefabs. Place them in scenes derived from the working template rigs: `LectureHall_VR`, `Professor_MR`, and `Classmate_Desktop`, under `Assets/Scenes/`. Assign `CloseUp.renderTexture` to an overhead camera and exclude the screen from its culling mask. Render Texture output is local; each network client renders its own synchronized scene. Then run **Tools > DIYsect > Apply Realistic Look**: it re-measures the incision and flap hinges on the current skin, adds the cut line, the surgical drapes and wet-tissue materials to the body prefab, puts the lecture hall around the student's table in `LectureHall_VR` (replacing the room placeholder), hangs the operating light in `LectureHall_VR` and `Classmate_Desktop`, moves the instruments onto a stand beside the table, and gives the scalpel and forceps their real models. Rerun it after re-exporting the skin, drapes or lamp.
9. **MANUAL:** Configure the template's service/project credentials using Unity's UI, then test two connected clients. Build an ARM64 Android APK for Quest 3 and test on hardware. Never commit credentials. Check the [architecture and acceptance checklist](docs/ARCHITECTURE.md).

**Classmate phone APK (no XR):** run **Tools > DIYsect > Build Classmate Phone APK**, or in batch mode `Unity -batchmode -projectPath . -buildTarget Android -executeMethod DIYsect.Editor.BuildAndroidClassmate.BuildBatch -logFile build.log`. It builds the shared `Professor_MR` scene with the classmate role to `Builds/Android-Classmate/DIYsect-Classmate.apk` with XR off and the application id suffixed `.classmate`, so it installs next to the Quest app. The XR, id and name changes last only for the build and are restored afterwards (the shared Android options IL2CPP, ARM64 and APK are the same as the Quest build and stay set); the Quest build (`Build Quest APK`) is unchanged. The phone opens on a start screen with a Join the room button; a tap shows an organ's name, pressing and holding places a question pin, and the bottom row has Unmute and Raise hand; dragging turns the camera around the body and the scroll wheel or a two-finger pinch zooms (`ClassmateOrbitCamera`).

**Quest on Mac:** [Meta Horizon Link](https://developers.meta.com/vr/documentation/unity/unity-link/) is Windows only. Build a standalone APK from Mac; use the simulator for iteration or a Windows PC with Link for a PC-streamed demo.

## Assets and licenses

| ID | Asset | Source | License | Acquisition |
|---|---|---|---|---|
| A1 | Human anatomy, organs and body wall | [Z-Anatomy](https://github.com/Z-Anatomy/The-blend) | CC BY-SA 4.0 | Fetch and processing scripts; source archive stays local |
| A2 | University Lecture Hall — Heat Of Fusion | [Sketchfab](https://sketchfab.com/3d-models/university-lecture-hall-452cc01ebd1a47de9925bd43f44d7da4) | CC BY 4.0 | Manual login/download; `simplify_room.py` reduces it to about 246k triangles |
| A3 | SURGICAL INSTRUMENT — ferofluid | [Sketchfab](https://sketchfab.com/3d-models/surgical-instrument-2971a44d334b45ff926792e92a6bcee7) | CC BY 4.0 | Manual login/download; `extract_instruments.py` keeps the forceps and scalpel handle and adds a project-authored blade |
| A4 | Charité operating room — ChrisRE | [Sketchfab](https://sketchfab.com/3d-models/charite-university-hospital-operating-room-9ec46c4d615a4581a235eebfb162f574) | CC BY-NC 4.0 | Optional non-commercial backup; about 335k triangles before simplification |
| A5 | EzySlice — David Arayan | [Source](https://github.com/DavidArayan/ezy-slice) | MIT | Included source; optional whole-mesh planar cutting |
| A6 | Table, tray, screen | Unity primitives | Project-authored | Editor menu builds prefabs |
| A7 | Surgical drapes | `Tools/blender/make_drapes.py` (cloth simulation around the anatomy) | Project-authored | Committed GLB; rerun the script after changing the anatomy |
| A8 | Surgical lamp | `Tools/blender/make_surgical_lamp.py` | Project-authored | Committed GLB |

See [full credits](docs/CREDITS.md). Derived anatomy and its previews remain CC BY-SA 4.0. The source has colored materials and no texture images. Unsupported stylized shader groups are converted to simple export materials using their source color controls; this preserves the color palette, not the original shader effects. The skin flaps are a simplified prototype, not a supplied anatomical skin layer. Do not use paid frog packs, the NIH frog kit, or an unverified laboratory pack.

## Anatomy previews

These previews show the exported organ set with its source material color palette, rendered using Cycles CPU at 48 samples. Inspect synthetic skin coverage separately in Unity.

![Front organ preview](docs/previews/front.png)
![Three-quarter organ preview](docs/previews/three_quarter.png)

The Unity scene previews show `LectureHall_VR` from where the student stands: intact under the operating light, then with the skin, rectus, transversus and omentum opened. Refresh them with **Tools > DIYsect > Capture Scene Previews** (batch mode needs graphics, not `-nographics`). The wide view shows the table in the front area of the lecture hall, and the close view shows the real scalpel and forceps on their stand.

![Intact body on the draped table](docs/previews/scene_intact.png)
![Body wall opened down to the organs](docs/previews/scene_opened.png)
![Lecture hall seen from behind the student](docs/previews/scene_hall.png)
![Scalpel and forceps on the instrument stand](docs/previews/scene_instruments.png)

See the [student controls and seated audience setup](docs/VR_CONTROLS.md) for the lecture hall.

## Performance

Keep the whole visible scene below approximately **750,000 triangles**. Individual limits are ceilings, not allocations: organs 20k, heart chambers/lung lobes 30k, body-wall pieces 20k, skin flaps 15k each, room 300k, instruments 10k each. The extraction script allocates at most **330k across anatomy surfaces**, leaving room for skin, tools, avatars and props. Inspect transparent and double-sided materials, draw calls and frame timing on Quest as well as triangle counts.

## Team and repository

| Role | Owner | Responsibility |
|---|---|---|
| A | Aaditya | Networking, body state, organ ownership, guiding-hand depth control |
| B | Saurav | Student VR, lecture hall, incision, flaps, organ grab and socket quiz |
| C | Arjun | Professor MR, desktop/Android classmate, question pins, close-up screen and demo |

[GitHub issues](https://github.com/Imersive-studio/DIYsect-XR/issues) track implementation. [Starter checklist](docs/UNITY_BACKLOG.md) defines the 16 Unity tasks and role ownership.

See [deployment and OpenXR notes](docs/DEPLOYMENT.md) to build, install and run on Quest 3, and the [student VR controls](docs/VR_CONTROLS.md) for what a tester should try.

`Assets/` holds Unity content. `Tools/blender/` holds reproducible asset processing. `docs/` is the existing documentation directory (keep this casing on every platform). `SourceAssets/` is local only. The template must supply `Packages/` and `ProjectSettings/` before this is a runnable Unity project; the scaffold alone is not a working multiplayer demo.

The earlier frog browser prototype remains in `server/`, `shared/`, `vr/`, `audience/` and `public/`. Run it with `npm ci`, `npm start`, and `npm test`. Its WebSocket/WebXR implementation is separate from the Unity Netcode/OpenXR work. Other older documents describe that prototype; this README and the Unity architecture/checklist describe the current direction.

Follow the [task branch and commit workflow](docs/WORKFLOW.md) for every change.

### Immersive dissection checklist

For the headset guidance upgrade, run **Tools > DIYsect > Add Dissection Checklist** in Unity 6000.3.28f1. It rebuilds the local card, progress list and body cues in `LectureHall_VR` and `Professor_MR`; the desktop scene stays unchanged. Run it after updating scenes from main, including the seated audience work. Setup, controls, validation and outstanding device checks are in [the checklist guide](docs/IMMERSIVE_CHECKLIST.md).

Failed host/client network starts now initiate lobby cleanup before reporting failure. See [failed-start cleanup and remaining integration checks](docs/DEPLOYMENT.md#failed-network-start-cleanup-166).
