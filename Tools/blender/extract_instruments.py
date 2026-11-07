"""Run: .venv/bin/python -I Tools/blender/extract_instruments.py -- [--input SourceAssets/Instruments/surgical_instrument.glb].

Takes two tools out of the instrument set (A3, "SURGICAL INSTRUMENT" by ferofluid, CC BY 4.0) and writes
Assets/Models/Instruments/forceps.glb and scalpel.glb.

The set is one mesh of 24 loose parts laid side by side. Two parts are used, picked by their centre in the
source file: the thumb (tissue) forceps and the scalpel handle. The source has no scalpel blade, so a
No. 22 style blade (project-authored) is modelled into the handle's blade slot.

Each tool is exported in the convention of the Unity placeholder tools: its long axis along Unity +Z, its
thin side along Unity X, and its origin at the working tip (the forceps jaws, the blade point), so the model
can be placed exactly on the tool's Tip transform. Blender works Z-up and the GLB is Y-up, so Unity +Z is
Blender -Y, Unity +X is Blender -X and Unity +Y is Blender +Z.
"""
import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parent))
from common import *
import bmesh
from mathutils import Matrix, Vector
from validate_assets import glb_triangles

TOOL_BUDGET = 10000
# Part centres in the source (Blender metres, x and z of the flat set); the nearest loose part is taken.
FORCEPS_CENTRE = (0.146, -0.052)
HANDLE_CENTRE = (-0.046, -0.050)
STEEL = (0.78, 0.79, 0.80, 1)
BLADE_LENGTH = 0.040
BLADE_HEIGHT = 0.011
BLADE_THICKNESS = 0.0005
BLADE_SEAT = 0.008          # how far the blade's tang sits inside the handle's slot


def loose_parts(obj):
    """Split one mesh object into one object per connected piece."""
    select(obj)
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.separate(type='LOOSE')
    bpy.ops.object.mode_set(mode='OBJECT')
    return [o for o in bpy.context.scene.objects if o.type == 'MESH']


def centre(obj):
    points = [obj.matrix_world @ v.co for v in obj.data.vertices]
    return sum(points, Vector()) / len(points)


def nearest(parts, xz):
    return min(parts, key=lambda o: (centre(o).x - xz[0]) ** 2 + (centre(o).z - xz[1]) ** 2)


def bake(obj):
    """Apply the object's full world transform (including its parents) to its mesh."""
    select(obj)
    world = obj.matrix_world.copy()
    obj.parent = None
    obj.matrix_world = world
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)


def end_widths(obj, fraction=0.15):
    """Width across the flat set (Blender X) near each end of the long axis (Blender Z): (low end, high end)."""
    zs = [v.co.z for v in obj.data.vertices]
    low, high = min(zs), max(zs)
    span = (high - low) * fraction

    def width(test):
        xs = [v.co.x for v in obj.data.vertices if test(v.co.z)]
        return max(xs) - min(xs)
    return width(lambda z: z < low + span), width(lambda z: z > high - span), low, high


def to_tool_frame(obj, tip_at_high_z, tip_point, width_up):
    """Move the tip to the origin and lay the tool along Blender -Y (Unity +Z).

    In the source the long axis is Blender Z, the width across the set is X and the thin side is Y.
    Afterwards the long axis points along -Y toward the tip. With width_up the width stands vertical
    (Blender Z, Unity up) and the thin side is X, like the scalpel's blade plane; otherwise the width lies
    along X, like the placeholder forceps whose arms sit side by side, and the thin side is up.
    """
    toward_tip = 1 if tip_at_high_z else -1
    # Rows give the new X, Y, Z from the source X (width), Y (thin) and Z (long).
    if width_up:
        turn = Matrix(((0, 1, 0), (0, 0, -toward_tip), (1, 0, 0))).to_4x4()
    else:
        turn = Matrix(((1, 0, 0), (0, 0, -toward_tip), (0, 1, 0))).to_4x4()
    obj.data.transform(Matrix.Translation(-tip_point))
    obj.data.transform(turn)
    obj.data.update()


def blade():
    """A No. 22 style blade in the tool frame (point at the origin, heel toward the handle along +Y).

    The outline runs from the point back along the spine, which rises to the full height at the heel, then
    returns along the curved cutting edge (the belly) to the point. It is centred on the handle's centre line
    and extruded to the blade thickness across X.
    """
    height, length, steps = BLADE_HEIGHT, BLADE_LENGTH, 10
    spine = [(t * length, 0.3 * height + 0.7 * height * (1 - (1 - t) ** 3)) for t in (i / steps for i in range(steps + 1))]
    belly = [(t * length, 0.3 * height * (1 - t) ** 3) for t in (1 - i / steps for i in range(1, steps))]
    outline = [(along, up - height / 2) for along, up in spine + belly]
    mesh = bpy.data.meshes.new('blade')
    bm = bmesh.new()
    face = bm.faces.new([bm.verts.new((-BLADE_THICKNESS / 2, along, up)) for along, up in outline])
    extruded = bmesh.ops.extrude_face_region(bm, geom=[face])
    moved = [g for g in extruded['geom'] if isinstance(g, bmesh.types.BMVert)]
    bmesh.ops.translate(bm, verts=moved, vec=(BLADE_THICKNESS, 0, 0))
    bmesh.ops.triangulate(bm, faces=list(bm.faces))
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new('blade', mesh)
    bpy.context.scene.collection.objects.link(obj)
    return obj


def finish(obj, name, path):
    material = bpy.data.materials.get('Instrument steel') or bpy.data.materials.new('Instrument steel')
    material.use_nodes = True
    bsdf = material.node_tree.nodes['Principled BSDF']
    bsdf.inputs['Base Color'].default_value = STEEL
    bsdf.inputs['Metallic'].default_value = 1.0
    bsdf.inputs['Roughness'].default_value = 0.2
    material.diffuse_color = STEEL
    obj.data.materials.clear()
    obj.data.materials.append(material)
    for poly in obj.data.polygons:
        poly.use_smooth = True
    obj.name = name
    triangles = count(obj)
    if triangles > TOOL_BUDGET:
        raise RuntimeError(f'{name}: {triangles} triangles, budget {TOOL_BUDGET}')
    export(obj, path, 'instrument', name)
    return glb_triangles(path)


def main():
    argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    source = Path(argv[argv.index('--input') + 1]) if '--input' in argv else ROOT / 'SourceAssets/Instruments/surgical_instrument.glb'
    output = ROOT / 'Assets/Models/Instruments'
    if not source.exists():
        raise FileNotFoundError(f'{source} is missing. Download A3 into SourceAssets/Instruments/.')
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(source))
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    if len(meshes) != 1:
        raise RuntimeError(f'Expected the instrument set as one mesh, found {len(meshes)}')
    bake(meshes[0])
    parts = loose_parts(meshes[0])
    forceps = nearest(parts, FORCEPS_CENTRE)
    handle = nearest(parts, HANDLE_CENTRE)
    for obj in parts:
        if obj not in (forceps, handle):
            bpy.data.objects.remove(obj, do_unlink=True)

    # Forceps: the jaws are the end where the two arms stand apart, the wider end.
    low_w, high_w, low, high = end_widths(forceps)
    tip_high = high_w > low_w
    tip_z = high if tip_high else low
    jaws = [v.co for v in forceps.data.vertices if abs(v.co.z - tip_z) < 0.004]
    to_tool_frame(forceps, tip_high, width_up=False, tip_point=Vector((sum(p.x for p in jaws) / len(jaws), sum(p.y for p in jaws) / len(jaws), tip_z)))
    print(f'forceps: {finish(forceps, "forceps", output / "forceps.glb")} triangles, jaws at the {"high" if tip_high else "low"} end')

    # Scalpel: the blade slot is at the narrower end of the handle. The blade's point sits BLADE_LENGTH ahead of it.
    low_w, high_w, low, high = end_widths(handle)
    slot_high = high_w < low_w
    slot_z = high if slot_high else low
    slot = [v.co for v in handle.data.vertices if abs(v.co.z - slot_z) < 0.004]
    slot_point = Vector((sum(p.x for p in slot) / len(slot), sum(p.y for p in slot) / len(slot), slot_z))
    direction = 1 if slot_high else -1
    point = slot_point + Vector((0, 0, direction * (BLADE_LENGTH - BLADE_SEAT)))
    to_tool_frame(handle, slot_high, point, width_up=True)
    steel = blade()
    bpy.ops.object.select_all(action='DESELECT')
    handle.select_set(True)
    steel.select_set(True)
    bpy.context.view_layer.objects.active = handle
    bpy.ops.object.join()
    print(f'scalpel: {finish(handle, "scalpel", output / "scalpel.glb")} triangles, slot at the {"high" if slot_high else "low"} end')


if __name__ == '__main__':
    main()
