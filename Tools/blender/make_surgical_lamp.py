"""Run: blender -b -P Tools/blender/make_surgical_lamp.py -- [--output Assets/Models/Environment/Lamp/surgical_lamp.glb].

Builds a project-authored ceiling-mounted surgical lamp: a domed head with a ring of LED lenses
and a central sterile handle, hung from a jointed arm. The origin is the centre of the lens face,
so placing the model puts the light exactly where the Unity spot light sits. Blender Z-up;
the GLB is Y-up like the other models.
"""
import math
import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parent))
from common import *
import bmesh
from mathutils import Vector

TRIANGLE_BUDGET = 6000
HEAD_RADIUS = 0.3
ARM_REACH = 0.6        # horizontal offset from the ceiling post to the head
CEILING_HEIGHT = 1.3   # from the lens face up to the ceiling mount

COLORS = {
    'Lamp housing': (0.92, 0.93, 0.94, 1),
    'Lamp lens': (1.0, 0.98, 0.92, 1),
    'Lamp handle': (0.45, 0.62, 0.72, 1),
    'Lamp arm': (0.62, 0.64, 0.66, 1),
}


def material(name):
    mat = bpy.data.materials.get(name)
    if mat is None:
        mat = bpy.data.materials.new(name)
        mat.use_nodes = True
        bsdf = mat.node_tree.nodes['Principled BSDF']
        bsdf.inputs['Base Color'].default_value = COLORS[name]
        mat.diffuse_color = COLORS[name]
        if name == 'Lamp lens':
            bsdf.inputs['Emission Color'].default_value = COLORS[name]
            bsdf.inputs['Emission Strength'].default_value = 4.0
    return mat


def finish(obj, material_name, smooth=True):
    obj.data.materials.clear()
    obj.data.materials.append(material(material_name))
    for poly in obj.data.polygons:
        poly.use_smooth = smooth
    return obj


def cylinder(a, b, radius, material_name, vertices=20):
    a, b = Vector(a), Vector(b)
    direction = b - a
    bpy.ops.mesh.primitive_cylinder_add(radius=radius, depth=direction.length, location=(a + b) / 2, vertices=vertices)
    obj = bpy.context.object
    obj.rotation_euler = direction.to_track_quat('Z', 'Y').to_euler()
    return finish(obj, material_name, smooth=False)


def sphere(centre, radius, material_name):
    bpy.ops.mesh.primitive_uv_sphere_add(radius=radius, location=centre, segments=16, ring_count=8)
    return finish(bpy.context.object, material_name)


def head():
    parts = []
    # Dome: the upper half of a flattened sphere, open side down.
    bpy.ops.mesh.primitive_uv_sphere_add(radius=1, segments=48, ring_count=16, location=(0, 0, 0))
    dome = bpy.context.object
    mesh = dome.data
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if v.co.z < -1e-4], context='VERTS')
    bm.to_mesh(mesh)
    bm.free()
    dome.scale = (HEAD_RADIUS, HEAD_RADIUS, 0.11)
    bpy.ops.object.transform_apply(scale=True)
    solid = dome.modifiers.new('Shell', 'SOLIDIFY')
    solid.thickness = 0.012
    bpy.ops.object.modifier_apply(modifier=solid.name)
    parts.append(finish(dome, 'Lamp housing'))
    # Lens face: a disc just inside the rim with seven LED lenses.
    bpy.ops.mesh.primitive_cylinder_add(radius=HEAD_RADIUS - 0.01, depth=0.01, location=(0, 0, 0.005), vertices=48)
    parts.append(finish(bpy.context.object, 'Lamp housing', smooth=False))
    lenses = [(0, 0)] + [(0.17 * math.cos(a), 0.17 * math.sin(a)) for a in [i * math.tau / 6 for i in range(6)]]
    for x, y in lenses:
        bpy.ops.mesh.primitive_cylinder_add(radius=0.055, depth=0.008, location=(x, y, -0.002), vertices=24)
        parts.append(finish(bpy.context.object, 'Lamp lens', smooth=False))
    # Sterile handle in the middle of the lens face.
    parts.append(cylinder((0, 0, -0.006), (0, 0, -0.13), 0.022, 'Lamp handle'))
    parts.append(sphere((0, 0, -0.13), 0.026, 'Lamp handle'))
    return parts


def arm():
    top = CEILING_HEIGHT
    parts = [
        cylinder((0, 0, 0.11), (0, 0, 0.3), 0.025, 'Lamp arm'),           # neck above the dome
        sphere((0, 0, 0.3), 0.04, 'Lamp arm'),                             # tilt joint
        cylinder((0, 0, 0.3), (ARM_REACH, 0, 0.45), 0.03, 'Lamp arm'),     # spring arm
        sphere((ARM_REACH, 0, 0.45), 0.045, 'Lamp arm'),                   # elbow
        cylinder((ARM_REACH, 0, 0.45), (ARM_REACH, 0, top), 0.035, 'Lamp arm'),  # ceiling post
    ]
    bpy.ops.mesh.primitive_cylinder_add(radius=0.14, depth=0.04, location=(ARM_REACH, 0, top), vertices=32)
    parts.append(finish(bpy.context.object, 'Lamp housing', smooth=False))   # ceiling canopy
    return parts


def main():
    argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    output = Path(argv[argv.index('--output') + 1]) if '--output' in argv else \
        ROOT / 'Assets/Models/Environment/Lamp/surgical_lamp.glb'
    bpy.ops.wm.read_factory_settings(use_empty=True)
    parts = head() + arm()
    bpy.ops.object.select_all(action='DESELECT')
    for part in parts:
        part.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    lamp = bpy.context.object
    lamp.name = 'surgical_lamp'
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    triangles = count(lamp)
    if triangles > TRIANGLE_BUDGET:
        raise RuntimeError(f'Lamp has {triangles} triangles, budget {TRIANGLE_BUDGET}')
    export(lamp, output, 'environment', 'Surgical lamp')
    print(f'Exported {output} with {triangles} triangles (project-authored).')


if __name__ == '__main__':
    main()
