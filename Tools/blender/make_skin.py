"""Run: .venv/bin/python -I Tools/blender/make_skin.py -- [--input Startup.blend]."""
import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parent))
from common import *
import bmesh
from mathutils import Matrix

def main():
    args = arguments(__doc__)
    load_source(args.input)
    sources = [bpy.data.objects[n] for n in WALL[:4]]
    sources += [o for o in bpy.data.objects if o.type == 'MESH' and 'pectoralis major' in o.name.lower()
                and not re.search(r'\.(?:j|i|o.*|e.*)$', o.name)]
    copies = []
    for obj in sources:
        copy = obj.copy()
        copy.data = obj.data.copy()
        bpy.context.scene.collection.objects.link(copy)
        world = obj.matrix_world.copy()
        copy.parent = None
        copy.matrix_world = world
        copies.append(copy)
    bpy.ops.object.select_all(action='DESELECT')
    for obj in copies:
        obj.hide_set(False)
        obj.hide_viewport = False
        obj.select_set(True)
    bpy.context.view_layer.objects.active = copies[0]
    bpy.ops.object.join()
    shell = bpy.context.object
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    for mod in list(shell.modifiers):
        shell.modifiers.remove(mod)
    extent = max(shell.dimensions)
    mod = shell.modifiers.new('Skin offset', 'SOLIDIFY')
    mod.thickness = extent * 0.008
    mod.offset = 1
    bpy.ops.object.modifier_apply(modifier=mod.name)
    mod = shell.modifiers.new('Smooth shell', 'SMOOTH')
    mod.factor = 0.8
    mod.iterations = 5
    bpy.ops.object.modifier_apply(modifier=mod.name)
    material = bpy.data.materials.new('Skin')
    material.diffuse_color = (0.65, 0.38, 0.24, 1)
    material.use_nodes = True
    material.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = material.diffuse_color
    entries = []
    for side in ['l', 'r']:
        obj = shell.copy()
        obj.data = shell.data.copy()
        bpy.context.scene.collection.objects.link(obj)
        mesh = bmesh.new()
        mesh.from_mesh(obj.data)
        bmesh.ops.bisect_plane(mesh, geom=list(mesh.verts)+list(mesh.edges)+list(mesh.faces),
            dist=0.000001, plane_co=(0,0,0), plane_no=(1,0,0),
            clear_inner=side=='l', clear_outer=side=='r')
        mesh.to_mesh(obj.data)
        mesh.free()
        obj.name = f'skin_flap_{side}'
        obj.data.materials.clear()
        obj.data.materials.append(material)
        simplify(obj,15000)
        for poly in obj.data.polygons:
            poly.use_smooth=True
        entries.append(export(obj,args.output/'Skin'/f'{obj.name}.glb','skin'))
    path=args.output/'manifest.json'
    existing=json.loads(path.read_text()) if path.exists() else []
    save_manifest(args.output,[e for e in existing if e['layer']!='skin']+entries)
    print('Two prototype skin flaps exported; inspect coverage and hinges in Unity.')

if __name__=='__main__':
    main()
