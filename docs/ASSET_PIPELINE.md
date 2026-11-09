# Rebuilding the anatomy and environment assets

The repository already contains the processed Unity assets. **You do not need
these steps to open the project or build an APK.** Run them when changing the
source anatomy, environment or instruments.

Sources and licensing are recorded in [CREDITS.md](CREDITS.md). Keep raw downloads
in ignored `SourceAssets/`; do not commit them or raw `.blend` files. The scripts
live in [Tools/blender](../Tools/blender).


## Anatomy and procedural assets

Run these from the repository root when rebuilding assets:

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

## Lecture hall and instruments

Sign into Sketchfab and download the lecture hall and surgical instrument set listed in [the credits](CREDITS.md) as GLB. Put the lecture hall in `SourceAssets/LectureHall/lecture_hall.glb` and the instrument set in `SourceAssets/Instruments/surgical_instrument.glb` (`SourceAssets/` is git-ignored; never commit the raw downloads). Then run:

```bash
.venv/bin/python -I Tools/blender/simplify_room.py -- --input SourceAssets/LectureHall/lecture_hall.glb --budget 250000
.venv/bin/python -I Tools/blender/extract_instruments.py
```

`simplify_room.py` keeps small parts (walls, floor, screen, desks) intact and decimates only the parts above one shared per-object cap (the chair rows), then scales textures to 1024 px or less; the hall goes from about 1.21M to about 246k triangles. `extract_instruments.py` takes the thumb forceps and the scalpel handle out of the instrument set, models a No. 22 style blade into the handle, and writes `Assets/Models/Instruments/forceps.glb` and `scalpel.glb` with their origin on the working tip. The Charité operating room is an optional non-commercial backup, not a required download.

## Import and scale

The project includes Unity glTFast (`com.unity.cloud.gltfast`) for GLB import.
Open the existing Unity project and allow its packages and models to import.
Keep each part's relative transform; do not center the organs independently.
Check scale against a meter cube before rebuilding the anatomy prefab.


## Props and scene appearance

Run **Tools > DIYsect > Create Classroom Props** to create table, tray and screen prefabs. The command preserves existing prefabs. Place them in scenes derived from the working template rigs: `LectureHall_VR`, `Professor_MR`, and `Classmate_Desktop`, under `Assets/Scenes/`. Assign `CloseUp.renderTexture` to an overhead camera and exclude the screen from its culling mask. Render Texture output is local; each network client renders its own synchronized scene. Then run **Tools > DIYsect > Apply Realistic Look**: it re-measures the incision and flap hinges on the current skin, adds the cut line, the surgical drapes and wet-tissue materials to the body prefab, puts the lecture hall around the student's table in `LectureHall_VR` (replacing the room placeholder), hangs the operating light in `LectureHall_VR` and `Classmate_Desktop`, moves the instruments onto a stand beside the table, and gives the scalpel and forceps their real models. Rerun it after re-exporting the skin, drapes or lamp.
