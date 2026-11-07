"""Run: .venv/bin/python -I Tools/blender/extract_organs.py -- [--input Startup.blend]."""
import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parent))
from common import *

def main():
    args = arguments(__doc__)
    load_source(args.input)
    missing = [name for name in ORGANS + WALL if name not in bpy.data.objects or bpy.data.objects[name].type != 'MESH']
    if missing:
        raise RuntimeError(f'Missing required surfaces: {missing}')
    extras = [o.name for o in bpy.data.objects if o.type == 'MESH'
              and any(s in o.name.lower() for s in ['rib', 'costal cartilage', 'pectoralis major'])
              and not re.search(r'\.(?:j|i|o.*|e.*)$', o.name)]
    names = list(dict.fromkeys(ORGANS + WALL + sorted(extras)))
    entries = []
    # Reserve most of the scene for the room, skin, tools and avatars.
    # Per-piece limits alone could exceed the whole-scene budget.
    weights = {n: (30000 if 'lung' in n or n in ORGANS[5:9] else 20000) for n in names}
    allocation = 330000 / sum(weights.values())
    for name in names:
        obj = bpy.data.objects[name]
        layer = 'organ' if name in ORGANS else ('omentum' if 'omentum' in name.lower() else ('muscle' if name in WALL[:4] else 'chest'))
        folder = 'Organs' if name in ORGANS else 'BodyWall'
        simplify(obj, min(weights[name], int(weights[name] * allocation)))
        entries.append(export(obj, args.output / folder / f'{slug(name)}.glb', layer, name))
    save_manifest(args.output, entries)
    print(f'Exported {len(entries)} surfaces; {sum(e["triangles"] for e in entries)} triangles')

if __name__ == '__main__':
    main()
