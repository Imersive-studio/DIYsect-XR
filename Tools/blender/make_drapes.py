"""Run: blender -b -P Tools/blender/make_drapes.py -- [--output Assets/Models/Environment/Drapes/surgical_drapes.glb].

Builds project-authored surgical drapes around the exported anatomy with a cloth simulation:
a leg drape from the hips to the foot of the table, a head drape over a screen bar at the neck,
and two side drapes. The torso stays uncovered for the dissection.

The anatomy GLBs are upright (Blender Z-up, head +Z, front -Y). The simulation runs with the body
lying face-up (head +Y) so gravity is -Z, then the result is turned back upright before export.
That way the drapes go through the same import path and Supine pivot in Unity as the anatomy and
line up with it without extra transforms.

The table matches Assets/Models/Environment/DissectionTable.prefab: 0.8 m wide and 2.0 m long,
centred under the body, its top at the lowest point of the anatomy.
"""
import glob
import math
import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parent))
from common import *
from mathutils import Matrix, Vector

TABLE_HALF_WIDTH = 0.4
TABLE_HALF_LENGTH = 1.0
HANG = 0.32            # how far the drapes hang over the table edges
TRIANGLE_BUDGET = 40000
DRAPE_COLOR = (0.16, 0.42, 0.52, 1)   # surgical blue-green
FRAMES = 70

# Upright -> lying face-up (head toward +Y). The inverse turns the drapes back upright.
LIE_DOWN = Matrix.Rotation(math.radians(-90), 4, 'X')


def anatomy_bounds(band=0.05):
    """Bounds of the lying anatomy, plus the highest point within `band` of the hip end."""
    files = sorted(glob.glob(str(ROOT / 'Assets/Models/Anatomy/**/*.glb'), recursive=True))
    if not files:
        raise RuntimeError('No anatomy GLBs found. Run the anatomy pipeline first.')
    points = []
    for path in files:
        before = set(bpy.data.objects)
        bpy.ops.import_scene.gltf(filepath=path)
        for obj in set(bpy.data.objects) - before:
            if obj.type == 'MESH':
                matrix = LIE_DOWN @ obj.matrix_world
                points += [matrix @ v.co for v in obj.data.vertices]
            bpy.data.objects.remove(obj, do_unlink=True)
    lo = Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points)))
    hi = Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points)))
    hip_top = max(p.z for p in points if p.y < lo.y + band)
    return lo, hi, hip_top


def collider(obj, thickness=0.004):
    mod = obj.modifiers.new('Collision', 'COLLISION')
    obj.collision.thickness_outer = thickness
    obj.collision.cloth_friction = 20
    obj.hide_render = True
    return obj


def box(name, centre, size):
    bpy.ops.mesh.primitive_cube_add(location=centre)
    obj = bpy.context.object
    obj.name = name
    obj.scale = (size[0] / 2, size[1] / 2, size[2] / 2)
    bpy.ops.object.transform_apply(scale=True)
    return collider(obj)


def capsule(name, a, b, radius):
    """A cylinder with spheres at both ends, from point a to point b."""
    a, b = Vector(a), Vector(b)
    direction = b - a
    bpy.ops.mesh.primitive_cylinder_add(radius=radius, depth=direction.length, location=(a + b) / 2, vertices=24)
    cyl = bpy.context.object
    cyl.rotation_euler = direction.to_track_quat('Z', 'Y').to_euler()
    parts = [cyl]
    for end in (a, b):
        bpy.ops.mesh.primitive_uv_sphere_add(radius=radius, location=end, segments=24, ring_count=12)
        parts.append(bpy.context.object)
    return join(name, parts)


def ellipsoid(name, centre, radii):
    bpy.ops.mesh.primitive_uv_sphere_add(radius=1, location=centre, segments=32, ring_count=16)
    obj = bpy.context.object
    obj.name = name
    obj.scale = radii
    bpy.ops.object.transform_apply(scale=True)
    return collider(obj)


def join(name, parts):
    bpy.ops.object.select_all(action='DESELECT')
    for part in parts:
        part.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    obj = bpy.context.object
    obj.name = name
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    return collider(obj)


def _grid(name, size, location, cuts):
    """A flat grid in the XY plane; UVs are in metres so the weave tiles evenly."""
    bpy.ops.mesh.primitive_grid_add(x_subdivisions=cuts[0], y_subdivisions=cuts[1], size=1, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = (size[0], size[1], 1)
    bpy.ops.object.transform_apply(scale=True)
    for loop in obj.data.uv_layers.active.data:
        loop.uv = (loop.uv[0] * size[0], loop.uv[1] * size[1])
    return obj


def sheet(name, x_range, y_range, z, cuts, pin=None):
    """A horizontal cloth sheet that falls onto the colliders; cuts = (along x, along y)."""
    size = (x_range[1] - x_range[0], y_range[1] - y_range[0])
    obj = _grid(name, size, ((x_range[0] + x_range[1]) / 2, (y_range[0] + y_range[1]) / 2, z), cuts)
    return _add_cloth(obj, pin)


def curtain(name, x_range, z_range, y, cuts, pin=None):
    """A vertical cloth sheet in the XZ plane at y; cuts = (along x, along z)."""
    size = (x_range[1] - x_range[0], z_range[1] - z_range[0])
    obj = _grid(name, size, ((x_range[0] + x_range[1]) / 2, y, (z_range[0] + z_range[1]) / 2), cuts)
    obj.rotation_euler = (math.radians(90), 0, 0)
    bpy.ops.object.transform_apply(rotation=True)
    return _add_cloth(obj, pin)


def _add_cloth(obj, pin):
    cloth = obj.modifiers.new('Cloth', 'CLOTH')
    s = cloth.settings
    s.quality = 8
    s.mass = 0.25
    s.tension_stiffness = 12
    s.compression_stiffness = 12
    s.shear_stiffness = 5
    s.bending_stiffness = 0.4
    s.air_damping = 1.5
    if pin:
        # Pinned vertices stay where they start, like a drape clipped to the body.
        group = obj.vertex_groups.new(name='Pin')
        group.add([v.index for v in obj.data.vertices if pin(obj.matrix_world @ v.co)], 1.0, 'REPLACE')
        s.vertex_group_mass = 'Pin'
    cloth.collision_settings.collision_quality = 5
    cloth.collision_settings.distance_min = 0.004
    cloth.collision_settings.use_self_collision = False
    cloth.point_cache.frame_start = 1
    cloth.point_cache.frame_end = FRAMES
    return obj


def main():
    argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    output = Path(argv[argv.index('--output') + 1]) if '--output' in argv else \
        ROOT / 'Assets/Models/Environment/Drapes/surgical_drapes.glb'
    bpy.ops.wm.read_factory_settings(use_empty=True)
    lo, hi, hip_top = anatomy_bounds()
    cx, cy, top = (lo.x + hi.x) / 2, (lo.y + hi.y) / 2, lo.z
    hips, neck = lo.y, hi.y
    print(f'Anatomy lying down: x {lo.x:.3f}..{hi.x:.3f}, head-foot {hips:.3f}..{neck:.3f}, '
          f'height {top:.3f}..{hi.z:.3f}, hip end top {hip_top:.3f}')

    foot_end, head_end = cy - TABLE_HALF_LENGTH, cy + TABLE_HALF_LENGTH
    box('Table', (cx, cy, top - 0.04), (TABLE_HALF_WIDTH * 2, TABLE_HALF_LENGTH * 2, 0.08))
    # Lower body under the leg drape: pelvis, thighs and shins, toes up at the foot end.
    # The pelvis closes the cut hip end of the torso from the outside, up to the skin height there.
    hip_height = hip_top - top
    ellipsoid('Pelvis', (cx, hips - 0.05, top + hip_height / 2), (0.19, 0.09, hip_height / 2 + 0.005))
    for side in (-1, 1):
        capsule(f'Leg{side}', (cx + side * 0.09, hips - 0.08, top + 0.085), (cx + side * 0.1, foot_end + 0.12, top + 0.06), 0.08)
        ellipsoid(f'Foot{side}', (cx + side * 0.1, foot_end + 0.1, top + 0.11), (0.045, 0.05, 0.09))
    # Head and the screen bar that keeps the head drape off the face.
    ellipsoid('Head', (cx, neck + 0.13, top + 0.1), (0.08, 0.11, 0.1))
    # The screen stands right at the shoulders so no cut edge of the torso shows past it.
    bar_y, bar_z = neck - 0.01, top + 0.42
    capsule('ScreenBar', (cx - TABLE_HALF_WIDTH - 0.1, bar_y, bar_z), (cx + TABLE_HALF_WIDTH + 0.1, bar_y, bar_z), 0.012)
    for side in (-1, 1):
        capsule(f'ScreenPost{side}', (cx + side * (TABLE_HALF_WIDTH + 0.1), bar_y, top - 0.1),
                (cx + side * (TABLE_HALF_WIDTH + 0.1), bar_y, bar_z), 0.012)

    width = (cx - TABLE_HALF_WIDTH - HANG, cx + TABLE_HALF_WIDTH + HANG)
    side_x = hi.x + 0.03   # side drapes start just outside the torso
    sheets = [
        # Its hip edge is pinned over the lower abdomen, just above the skin.
        sheet('LegDrape', width, (foot_end - HANG, hips + 0.03), hip_top + 0.01, (64, 72),
              pin=lambda p: p.y > hips + 0.02 and abs(p.x - cx) < 0.17),
        # The head drape is clipped to the screen bar and falls over the head and the table end.
        sheet('HeadDrape', width, (bar_y, head_end + HANG), bar_z + 0.015, (64, 40),
              pin=lambda p: p.y < bar_y + 0.02),
        # A curtain hangs from the bar to the table and hides the cut neck end of the torso.
        curtain('ScreenCurtain', (cx - TABLE_HALF_WIDTH - 0.05, cx + TABLE_HALF_WIDTH + 0.05), (top + 0.01, bar_z),
                bar_y - 0.015, (48, 24), pin=lambda p: p.z > bar_z - 0.02),
        # Side drapes are clipped along the torso, so their inner edge is pinned and the rest hangs.
        sheet('SideDrapeR', (side_x, width[1]), (hips + 0.03, bar_y - 0.02), top + 0.01, (28, 40),
              pin=lambda p: abs(p.x - cx) < side_x - cx + 0.01),
        sheet('SideDrapeL', (width[0], cx - (side_x - cx)), (hips + 0.03, bar_y - 0.02), top + 0.01, (28, 40),
              pin=lambda p: abs(p.x - cx) < side_x - cx + 0.01),
    ]
    scene = bpy.context.scene
    scene.frame_start, scene.frame_end = 1, FRAMES
    for frame in range(1, FRAMES + 1):
        scene.frame_set(frame)

    for obj in sheets:
        select(obj)
        bpy.ops.object.modifier_apply(modifier='Cloth')
        smooth = obj.modifiers.new('Soften', 'CORRECTIVE_SMOOTH')
        smooth.iterations = 3
        bpy.ops.object.modifier_apply(modifier=smooth.name)
        for poly in obj.data.polygons:
            poly.use_smooth = True
    for obj in list(bpy.data.objects):
        if obj not in sheets:
            bpy.data.objects.remove(obj, do_unlink=True)

    bpy.ops.object.select_all(action='DESELECT')
    for obj in sheets:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = sheets[0]
    bpy.ops.object.join()
    drapes = bpy.context.object
    drapes.name = 'surgical_drapes'
    drapes.data.transform(LIE_DOWN.inverted())
    material = bpy.data.materials.new('Surgical drape')
    material.diffuse_color = DRAPE_COLOR
    material.use_nodes = True
    material.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = DRAPE_COLOR
    material.use_backface_culling = False
    drapes.data.materials.clear()
    drapes.data.materials.append(material)

    triangles = count(drapes)
    if triangles > TRIANGLE_BUDGET:
        raise RuntimeError(f'Drapes have {triangles} triangles, budget {TRIANGLE_BUDGET}')
    entry = export(drapes, output, 'drape', 'Surgical drapes')
    print(f'Exported {output} with {triangles} triangles (project-authored).')
    return entry


if __name__ == '__main__':
    main()
