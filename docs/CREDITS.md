# Asset credits

| Asset | Creator and source | License | Changes / use |
|---|---|---|---|
| The libre 3D atlas of anatomy | Z-Anatomy, Gauthier Kervyn and Marcin Zielinski; https://github.com/Z-Anatomy/The-blend | CC BY-SA 4.0 | Extracted surfaces, simplified geometry, prototype skin flaps (wrapped over the body wall with a fat layer by `Tools/blender/skin_layers.py`) and rendered previews |
| BodyParts3D | The Database Center for Life Science; underlying source of Z-Anatomy models | CC BY-SA 2.1 Japan | Attribution retained through Z-Anatomy |
| University Lecture Hall | Heat Of Fusion (융해열흡수); https://sketchfab.com/3d-models/university-lecture-hall-452cc01ebd1a47de9925bd43f44d7da4 | CC BY 4.0 | Simplified to about 246k triangles and 1024 px textures with `Tools/blender/simplify_room.py`; placed around the dissection table in LectureHall_VR |
| SURGICAL INSTRUMENT | ferofluid; https://sketchfab.com/3d-models/surgical-instrument-2971a44d334b45ff926792e92a6bcee7 | CC BY 4.0 | Thumb forceps and scalpel handle extracted with `Tools/blender/extract_instruments.py`; the scalpel blade is project-authored |
| Charité University Hospital - Operating Room | ChrisRE; https://sketchfab.com/3d-models/charite-university-hospital-operating-room-9ec46c4d615a4581a235eebfb162f574 | CC BY-NC 4.0 | Optional backup only; not included; non-commercial use only |
| EzySlice | David Arayan; https://github.com/DavidArayan/ezy-slice | MIT | Comment wording adjusted; runtime code unchanged; license included beside code |
| Dissection table, tray and screen | Project team | Project-authored primitives | Created in Unity using the editor menu |
| Surgical drapes | Project team | Project-authored | Cloth simulation in Blender (`Tools/blender/make_drapes.py`) around the exported anatomy |
| Surgical lamp | Project team | Project-authored | Modelled in Blender (`Tools/blender/make_surgical_lamp.py`) |
| Instrument stand | Project team | Project-authored primitives | Created in Unity by Tools > DIYsect > Arrange Instrument Stand |

Derived anatomy files and anatomy previews are shared under CC BY-SA 4.0. Keep attribution and the same license with redistributed derivatives. These asset terms do not change the licenses of independent code or other assets. This is an educational prototype; the synthetic skin and simplified geometry require visual review.

License texts: https://creativecommons.org/licenses/by-sa/4.0/ ; https://creativecommons.org/licenses/by-sa/2.1/jp/ ; https://creativecommons.org/licenses/by/4.0/ ; https://creativecommons.org/licenses/by-nc/4.0/ .

## Seated audience

The classmates in `Assets/Models/Audience` are rendered images of ten adult avatars from [Microsoft Rocketbox](https://github.com/microsoft/Microsoft-Rocketbox), MIT License, Copyright (c) 2020 Microsoft. The full notice is in `Assets/Models/Audience/LICENSE-Rocketbox.txt`. The avatars were posed seated and rendered once to transparent images. The source models are not in this repository.
