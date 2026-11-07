"""Run: .venv/bin/python -I Tools/blender/render_previews.py -- (after extraction)."""
import sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from common import *
from mathutils import Vector

def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    files=sorted((ROOT/'Assets/Models/Anatomy/Organs').glob('*.glb'))
    if len(files)!=22:
        raise RuntimeError(f'Expected 22 organs, found {len(files)}')
    for path in files:
        bpy.ops.import_scene.gltf(filepath=str(path))
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    points=[o.matrix_world@Vector(v) for o in meshes for v in o.bound_box]
    low=Vector(tuple(min(p[i] for p in points) for i in range(3)))
    high=Vector(tuple(max(p[i] for p in points) for i in range(3)))
    center=(low+high)/2
    size=max(high-low)
    scene=bpy.context.scene
    scene.render.engine='CYCLES'
    scene.cycles.device='CPU'
    scene.cycles.samples=48
    scene.render.use_freestyle=False
    scene.render.resolution_x=900
    scene.render.resolution_y=1100
    scene.render.resolution_percentage=100
    scene.world=bpy.data.worlds.new('Preview world')
    scene.world.use_nodes=True
    scene.world.node_tree.nodes['Background'].inputs[0].default_value=(0.22,0.22,0.22,1)
    for offset,energy in [((1,-2,2),150),((-2,-1,1),100)]:
        bpy.ops.object.light_add(type='AREA',location=center+Vector(offset)*size)
        light=bpy.context.object
        light.data.energy=energy*size*size
        light.data.shape='DISK'
        light.data.size=size*2
        light.rotation_euler=(center-light.location).to_track_quat('-Z','Y').to_euler()
    bpy.ops.object.camera_add()
    camera=bpy.context.object
    scene.camera=camera
    camera.data.type='ORTHO'
    camera.data.ortho_scale=size*1.3
    output=ROOT/'docs/previews'
    output.mkdir(parents=True,exist_ok=True)
    for name,offset in [('front',(0,-3,0)),('three_quarter',(1.8,-3,0.3))]:
        camera.location=center+Vector(offset)*size
        camera.rotation_euler=(center-camera.location).to_track_quat('-Z','Y').to_euler()
        scene.render.filepath=str(output/f'{name}.png')
        bpy.ops.render.render(write_still=True)

if __name__=='__main__':
    main()
