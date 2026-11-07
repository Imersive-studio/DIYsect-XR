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
   .venv/bin/python -I Tools/blender/render_previews.py
   ```

   Raw downloads stay in ignored `SourceAssets/`. The Blender file's embedded scripts are disabled. The manifest records source object names, triangle counts and Blender world positions; GLBs retain anatomical placement and convert to Y-up. Do not center each organ separately. Measure scale against a meter cube before building the body prefab.
6. **MANUAL:** Sign into Sketchfab and download A2 and A3 below as glTF/GLB. Put the lecture hall and its external textures together in `SourceAssets/LectureHall/`; put instruments in `SourceAssets/Instruments/`. Run:

   ```bash
   .venv/bin/python -I Tools/blender/simplify_room.py -- --input SourceAssets/LectureHall/room.glb
   ```

   Substitute the actual filename. In Blender, separate the instruments and simplify each to **10,000 triangles maximum**, then export them to `Assets/Models/Instruments/`. Keep textures beside `.gltf` input files. A4 is an optional non-commercial backup, not a required download.
7. **MANUAL:** Verify GLB import support in the template before dragging models into scenes. Unity does not universally import GLB by default. If no compatible importer is installed, open the GLBs in Blender and export FBX while preserving transforms and materials; do not rename their extensions. TODO: record the team's chosen import method and version.
8. Run **Tools > DIYsect > Create Classroom Props** to create table, tray and screen prefabs. The command preserves existing prefabs. Place them in scenes derived from the working template rigs: `LectureHall_VR`, `Professor_MR`, and `Classmate_Desktop`, under `Assets/Scenes/`. Assign `CloseUp.renderTexture` to an overhead camera and exclude the screen from its culling mask. Render Texture output is local; each network client renders its own synchronized scene.
9. **MANUAL:** Configure the template's service/project credentials using Unity's UI, then test two connected clients. Build an ARM64 Android APK for Quest 3 and test on hardware. Never commit credentials. Check the [architecture and acceptance checklist](docs/ARCHITECTURE.md).

**Quest on Mac:** [Meta Horizon Link](https://developers.meta.com/vr/documentation/unity/unity-link/) is Windows only. Build a standalone APK from Mac; use the simulator for iteration or a Windows PC with Link for a PC-streamed demo.

## Assets and licenses

| ID | Asset | Source | License | Acquisition |
|---|---|---|---|---|
| A1 | Human anatomy, organs and body wall | [Z-Anatomy](https://github.com/Z-Anatomy/The-blend) | CC BY-SA 4.0 | Fetch and processing scripts; source archive stays local |
| A2 | University Lecture Hall — Heat Of Fusion | [Sketchfab](https://sketchfab.com/3d-models/university-lecture-hall-452cc01ebd1a47de9925bd43f44d7da4) | CC BY 4.0 | Manual login/download; simplify from about 1.2M triangles |
| A3 | SURGICAL INSTRUMENT — ferofluid | [Sketchfab](https://sketchfab.com/3d-models/surgical-instrument-2971a44d334b45ff926792e92a6bcee7) | CC BY 4.0 | Manual login/download |
| A4 | Charité operating room — ChrisRE | [Sketchfab](https://sketchfab.com/3d-models/charite-university-hospital-operating-room-9ec46c4d615a4581a235eebfb162f574) | CC BY-NC 4.0 | Optional non-commercial backup; about 335k triangles before simplification |
| A5 | EzySlice — David Arayan | [Source](https://github.com/DavidArayan/ezy-slice) | MIT | Included source; optional whole-mesh planar cutting |
| A6 | Table, tray, screen | Unity primitives | Project-authored | Editor menu builds prefabs |

See [full credits](docs/CREDITS.md). Derived anatomy and its previews remain CC BY-SA 4.0. The source has colored materials and no texture images. Unsupported stylized shader groups are converted to simple export materials using their source color controls; this preserves the color palette, not the original shader effects. The skin flaps are a simplified prototype, not a supplied anatomical skin layer. Do not use paid frog packs, the NIH frog kit, or an unverified laboratory pack.

## Anatomy previews

These previews show the exported organ set with its source material color palette, rendered using Cycles CPU at 48 samples. Inspect synthetic skin coverage separately in Unity.

![Front organ preview](docs/previews/front.png)
![Three-quarter organ preview](docs/previews/three_quarter.png)

## Performance

Keep the whole visible scene below approximately **750,000 triangles**. Individual limits are ceilings, not allocations: organs 20k, heart chambers/lung lobes 30k, body-wall pieces 20k, skin flaps 15k each, room 300k, instruments 10k each. The extraction script allocates at most **330k across anatomy surfaces**, leaving room for skin, tools, avatars and props. Inspect transparent and double-sided materials, draw calls and frame timing on Quest as well as triangle counts.

## Team and repository

| Role | Owner | Responsibility |
|---|---|---|
| A | Aaditya | Networking, body state, organ ownership, guiding-hand depth control |
| B | Saurav | Student VR, lecture hall, incision, flaps, organ grab and socket quiz |
| C | Arjun | Professor MR, desktop/Android classmate, question pins, close-up screen and demo |

[GitHub issues](https://github.com/Imersive-studio/DIYsect-XR/issues) track implementation. [Starter checklist](docs/UNITY_BACKLOG.md) defines the 16 Unity tasks and role ownership.

`Assets/` holds Unity content. `Tools/blender/` holds reproducible asset processing. `docs/` is the existing documentation directory (keep this casing on every platform). `SourceAssets/` is local only. The template must supply `Packages/` and `ProjectSettings/` before this is a runnable Unity project; the scaffold alone is not a working multiplayer demo.

The earlier frog browser prototype remains in `server/`, `shared/`, `vr/`, `audience/` and `public/`. Run it with `npm ci`, `npm start`, and `npm test`. Its WebSocket/WebXR implementation is separate from the Unity Netcode/OpenXR work. Other older documents describe that prototype; this README and the Unity architecture/checklist describe the current direction.
