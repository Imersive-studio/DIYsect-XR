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

Not yet verified: Unity compilation, prefab menu execution, template scenes, multiplayer, OpenXR runtime, Quest builds and performance. Sketchfab room and instrument downloads require a human login. Room simplification passed the synthetic test; it has not been run on the lecture-hall asset.

Use the README's numbered setup and `ARCHITECTURE.md` acceptance checklist to complete these steps. Follow `WORKFLOW.md` for the parent branch, task branches and PR dependencies.
