"""Run: .venv/bin/python -I Tools/blender/test_pipeline.py. Uses temporary synthetic meshes."""
import sys
import tempfile
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from common import *
from validate_assets import glb_triangles
import simplify_room

bpy.ops.wm.read_factory_settings(use_empty=True)
with tempfile.TemporaryDirectory(prefix='diysect-mesh-test-') as folder:
    folder=Path(folder)
    bpy.ops.mesh.primitive_uv_sphere_add(segments=64,ring_count=32,location=(2,3,4))
    obj=bpy.context.object
    obj.scale=(2,1,0.5)
    material=bpy.data.materials.new('Fixture red')
    material.diffuse_color=(0.7,0.1,0.2,1)
    obj.data.materials.append(material)
    simplify(obj,500)
    assert tuple(obj.matrix_world.translation)==(2,3,4)
    entry=export(obj,folder/'fixture.glb','organ')
    assert glb_triangles(folder/'fixture.glb')==entry['triangles']<=500
    sys.argv=['simplify_room.py','--','--input',str(folder/'fixture.glb'),'--output',str(folder/'room.glb'),'--budget','100']
    simplify_room.main()
    assert 0<glb_triangles(folder/'room.glb')<=100
print('PASS: transform retention, source color export, GLB counts and room budget')
