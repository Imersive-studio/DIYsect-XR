"""Run: .venv/bin/python -I Tools/blender/simplify_room.py -- --input SourceAssets/LectureHall/room.glb."""
import sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from common import *

def main():
    args=arguments(__doc__,room=True)
    if not args.input or args.input.suffix.lower() not in ['.glb','.gltf']:
        raise RuntimeError('Supply --input pointing to the downloaded GLB or glTF')
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(args.input))
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    total=sum(count(o) for o in meshes)
    if not total:
        raise RuntimeError('Room contains no mesh triangles')
    for obj in meshes:
        simplify(obj,max(1,int(args.budget*count(obj)/total)))
    bpy.ops.object.select_all(action='DESELECT')
    for obj in meshes:
        obj.select_set(True)
    actual=sum(count(o) for o in meshes)
    if actual>args.budget:
        raise RuntimeError('Room exceeds total budget')
    output=args.output
    output.parent.mkdir(parents=True,exist_ok=True)
    bpy.ops.export_scene.gltf(filepath=str(output),export_format='GLB',use_selection=True,export_animations=False)
    print(f'{output}: {actual} triangles')

if __name__=='__main__':
    main()
