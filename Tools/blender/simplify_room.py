"""Run: .venv/bin/python -I Tools/blender/simplify_room.py -- --input SourceAssets/LectureHall/lecture_hall.glb [--budget 300000] [--max-texture 1024].

Simplifies a downloaded room to a triangle budget for Quest 3 and writes
Assets/Models/Environment/lecture_hall.glb.

Triangles are shared with one per-object cap: objects smaller than the cap (walls, floor, screen, desks)
keep every triangle, and only objects above it (the many chair rows) are decimated down to it. The cap is
the largest value that keeps the total within the budget. Textures larger than --max-texture pixels on a
side are scaled down, keeping their aspect ratio.
"""
import sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from common import *

def per_object_cap(counts,budget):
    """Largest cap c with sum(min(n, c)) <= budget. Objects at or under c are left untouched."""
    if sum(counts)<=budget:
        return max(counts)
    low,high=0,max(counts)
    while low<high:
        middle=(low+high+1)//2
        if sum(min(n,middle) for n in counts)<=budget:
            low=middle
        else:
            high=middle-1
    return low

def shrink_textures(max_size):
    """Scale every image larger than max_size on a side down to fit, keeping its aspect ratio."""
    shrunk=0
    for image in bpy.data.images:
        width,height=image.size
        if max(width,height)<=max_size or width==0 or height==0:
            continue
        scale=max_size/max(width,height)
        image.scale(max(1,int(width*scale)),max(1,int(height*scale)))
        image.pack()
        shrunk+=1
    return shrunk

def main():
    argv=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else sys.argv[1:]
    max_texture=int(argv.pop(argv.index('--max-texture')+1)) if '--max-texture' in argv else 1024
    if '--max-texture' in argv:
        argv.remove('--max-texture')
    sys.argv=['simplify_room.py','--',*argv]
    args=arguments(__doc__,room=True)
    if not args.input or args.input.suffix.lower() not in ['.glb','.gltf']:
        raise RuntimeError('Supply --input pointing to the downloaded GLB or glTF')
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(args.input))
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    counts=[count(o) for o in meshes]
    if not sum(counts):
        raise RuntimeError('Room contains no mesh triangles')
    cap=per_object_cap(counts,args.budget)
    if cap<1:
        raise RuntimeError(f'A budget of {args.budget} cannot hold {len(meshes)} objects')
    decimated=0
    for obj,triangles in zip(meshes,counts):
        if triangles>cap:
            simplify(obj,cap)
            decimated+=1
    actual=sum(count(o) for o in meshes)
    if actual>args.budget:
        raise RuntimeError('Room exceeds total budget')
    shrunk=shrink_textures(max_texture)
    bpy.ops.object.select_all(action='DESELECT')
    for obj in meshes:
        obj.select_set(True)
    output=args.output
    output.parent.mkdir(parents=True,exist_ok=True)
    bpy.ops.export_scene.gltf(filepath=str(output),export_format='GLB',use_selection=True,export_animations=False)
    print(f'{output}: {actual} triangles ({sum(counts)} before); {decimated} of {len(meshes)} objects decimated '
          f'to at most {cap}; {shrunk} textures scaled to {max_texture} px or less')

if __name__=='__main__':
    main()
