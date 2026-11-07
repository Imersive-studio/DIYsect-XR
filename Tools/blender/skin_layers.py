"""Run: blender -b -P Tools/blender/skin_layers.py -- [--input Assets/Models/Anatomy/Skin].

Gives each skin flap two materials: the outer skin and the subcutaneous fat on the inside and
the cut edge. make_skin.py calls split_skin_layers() before export; this command applies the same
split to flaps that were already exported, without changing their geometry or triangle count.
"""
import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parent))
from common import *
from mathutils import Vector

SKIN_COLOR = (0.65, 0.38, 0.24, 1)
FAT_COLOR = (0.93, 0.78, 0.42, 1)
# A face counts as outer skin when its normal points this much away from the body's long axis.
OUTER_THRESHOLD = 0.3


def _material(name, color):
    material = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    material.diffuse_color = color
    material.use_nodes = True
    material.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = color
    return material


def split_skin_layers(obj, axis_xy):
    """Material 0 = skin (faces pointing away from the long axis), 1 = fat (inside and cut edge).

    axis_xy is a point on the body's long (Z) axis in the upright Blender frame.
    """
    mesh = obj.data
    mesh.materials.clear()
    mesh.materials.append(_material('Skin', SKIN_COLOR))
    mesh.materials.append(_material('Subcutaneous fat', FAT_COLOR))
    world = obj.matrix_world
    normal_matrix = world.to_3x3().inverted().transposed()
    fat = 0
    for poly in mesh.polygons:
        centre = world @ poly.center
        radial = Vector((centre.x - axis_xy[0], centre.y - axis_xy[1], 0.0))
        normal = (normal_matrix @ poly.normal).normalized()
        outer = radial.length > 1e-6 and normal.dot(radial.normalized()) > OUTER_THRESHOLD
        poly.material_index = 0 if outer else 1
        fat += 0 if outer else 1
    if fat == 0 or fat == len(mesh.polygons):
        raise RuntimeError(f'{obj.name}: skin/fat split failed ({fat} of {len(mesh.polygons)} faces are fat)')
    return fat


def flap_axis(objects):
    """Centre of the flaps' combined footprint in X/Y, on the long axis of the body."""
    points = [o.matrix_world @ v.co for o in objects for v in o.data.vertices]
    xs = [p.x for p in points]
    ys = [p.y for p in points]
    return ((min(xs) + max(xs)) / 2, (min(ys) + max(ys)) / 2)


def main():
    argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    folder = Path(argv[argv.index('--input') + 1]) if '--input' in argv else ROOT / 'Assets/Models/Anatomy/Skin'
    files = sorted(folder.glob('skin_flap_*.glb'))
    if len(files) != 2:
        raise RuntimeError(f'Expected two skin flaps in {folder}, found {len(files)}')
    bpy.ops.wm.read_factory_settings(use_empty=True)
    flaps = {}
    for path in files:
        before = set(bpy.data.objects)
        bpy.ops.import_scene.gltf(filepath=str(path))
        meshes = [o for o in set(bpy.data.objects) - before if o.type == 'MESH']
        if len(meshes) != 1:
            raise RuntimeError(f'{path} should hold one mesh, found {len(meshes)}')
        flaps[path] = meshes[0]
    axis = flap_axis(flaps.values())
    for path, obj in flaps.items():
        triangles = count(obj)
        fat = split_skin_layers(obj, axis)
        obj.name = path.stem
        export(obj, path, 'skin')
        if count(obj) != triangles:
            raise RuntimeError(f'{path}: triangle count changed')
        print(f'{path.name}: {fat} of {len(obj.data.polygons)} faces are subcutaneous fat')


if __name__ == '__main__':
    main()
