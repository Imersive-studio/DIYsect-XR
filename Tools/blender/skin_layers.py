"""Run: blender -b -P Tools/blender/skin_layers.py -- [--input Assets/Models/Anatomy/Skin].

Finishes the skin flaps so they read as real skin over the body wall:
  1. keep only the outer skin surface,
  2. smooth it a little and wrap it just outside the muscles and costal cartilage it covers,
  3. give it thickness inward, with the inside and the cut edge as subcutaneous fat.

make_skin.py calls finish_flap() before export. This command applies the same steps to flaps that
were already exported and updates their triangle counts in the manifest.
"""
import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parent))
from common import *
import bmesh
from mathutils import Vector
from validate_assets import glb_triangles

SKIN_COLOR = (0.65, 0.38, 0.24, 1)
FAT_COLOR = (0.93, 0.78, 0.42, 1)
THICKNESS = 0.006      # skin plus a thin layer of fat, in metres
CLEARANCE = 0.002      # gap kept between the skin and the wall beneath it
# A face counts as outer skin when its normal points this much away from the body's long axis.
OUTER_THRESHOLD = 0.3
FLAP_BUDGET = 15000
OUTER_BUDGET = 7000
MIDLINE_SNAP = 0.015   # open edge vertices this close to the midline are moved onto it
HOLE_SIDES = 24       # holes with up to this many edges are filled


def _material(name, color):
    material = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    material.diffuse_color = color
    material.use_nodes = True
    material.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = color
    return material


def _radial(point, axis_xy):
    return Vector((point.x - axis_xy[0], point.y - axis_xy[1], 0.0))


def _keep_outer_surface(obj, axis_xy):
    """Delete every face that does not face away from the body axis; orient the rest outward."""
    mesh = bmesh.new()
    mesh.from_mesh(obj.data)
    world = obj.matrix_world
    rotation = world.to_3x3()

    def outer(face):
        radial = _radial(world @ face.calc_center_median(), axis_xy)
        return radial.length > 1e-6 and (rotation @ face.normal).normalized().dot(radial.normalized()) > OUTER_THRESHOLD

    bmesh.ops.delete(mesh, geom=[f for f in mesh.faces if not outer(f)], context='FACES')
    if not mesh.faces:
        raise RuntimeError(f'{obj.name}: no outer skin faces found')
    # Close the small holes left where a few faces turned sideways; only the flap outline stays open.
    bmesh.ops.holes_fill(mesh, edges=mesh.edges, sides=HOLE_SIDES)
    bmesh.ops.triangulate(mesh, faces=mesh.faces)
    bmesh.ops.recalc_face_normals(mesh, faces=mesh.faces)
    facing = sum((rotation @ f.normal).dot(_radial(world @ f.calc_center_median(), axis_xy)) for f in mesh.faces)
    if facing < 0:
        bmesh.ops.reverse_faces(mesh, faces=mesh.faces)
    boundary = sum(1 for e in mesh.edges if e.is_boundary)
    mesh.to_mesh(obj.data)
    mesh.free()
    return boundary


def _open_edge_vertices(mesh):
    """Indices of vertices on open edges (edges used by only one face), such as the flap outline."""
    users = {}
    for poly in mesh.polygons:
        for key in poly.edge_keys:
            users[key] = users.get(key, 0) + 1
    return {i for key, n in users.items() if n == 1 for i in key}


def _close_midline(obj, midline_x):
    """Move the flap's open edge vertices near the midline onto it, so the two flaps meet."""
    world = obj.matrix_world
    to_local = world.inverted()
    for i in _open_edge_vertices(obj.data):
        v = obj.data.vertices[i]
        point = world @ v.co
        if abs(point.x - midline_x) < MIDLINE_SNAP:
            point.x = midline_x
            v.co = to_local @ point
    obj.data.update()


def _apply(obj, kind, **settings):
    select(obj)
    mod = obj.modifiers.new(kind.title(), kind)
    for key, value in settings.items():
        setattr(mod, key, value)
    bpy.ops.object.modifier_apply(modifier=mod.name)


def _joined_copy(objects):
    """One world-space mesh made from copies of the given objects, used as a shrinkwrap target."""
    copies = []
    for obj in objects:
        copy = obj.copy()
        copy.data = obj.data.copy()
        bpy.context.scene.collection.objects.link(copy)
        copy.parent = None
        copy.matrix_world = obj.matrix_world
        copies.append(copy)
    bpy.ops.object.select_all(action='DESELECT')
    for copy in copies:
        copy.select_set(True)
    bpy.context.view_layer.objects.active = copies[0]
    bpy.ops.object.join()
    target = bpy.context.object
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    return target


def finish_flap(obj, wall, axis_xy):
    """Outer skin wrapped over `wall` (muscles, cartilage), thickened inward, with a fat layer.

    Material 0 is skin; material 1 is subcutaneous fat on the inner surface and the cut edge.
    Returns the triangle count.
    """
    boundary = _keep_outer_surface(obj, axis_xy)
    # Solidify doubles the surface and adds two rim triangles per open edge.
    simplify(obj, min(OUTER_BUDGET, (FLAP_BUDGET - 2 * boundary) // 2 - 100))
    target = _joined_copy(wall)
    # Smooth only the interior so the midline edges of the two flaps stay closed.
    edge_vertices = _open_edge_vertices(obj.data)
    interior = obj.vertex_groups.new(name='Interior')
    interior.add([v.index for v in obj.data.vertices if v.index not in edge_vertices], 1.0, 'REPLACE')
    _apply(obj, 'SMOOTH', factor=0.5, iterations=2, vertex_group='Interior')
    obj.vertex_groups.remove(obj.vertex_groups['Interior'])
    # Only vertices inside the wall move, to just outside it.
    _apply(obj, 'SHRINKWRAP', target=target, wrap_method='NEAREST_SURFACEPOINT', wrap_mode='OUTSIDE', offset=CLEARANCE)
    bpy.data.objects.remove(target, do_unlink=True)
    _close_midline(obj, axis_xy[0])
    obj.data.materials.clear()
    obj.data.materials.append(_material('Skin', SKIN_COLOR))
    obj.data.materials.append(_material('Subcutaneous fat', FAT_COLOR))
    for poly in obj.data.polygons:
        poly.material_index = 0
    _apply(obj, 'SOLIDIFY', thickness=THICKNESS, offset=-1, use_rim=True, use_quality_normals=True,
           material_offset=1, material_offset_rim=1)
    for poly in obj.data.polygons:
        poly.use_smooth = True
    triangles = count(obj)
    if triangles > FLAP_BUDGET:
        raise RuntimeError(f'{obj.name}: {triangles} triangles, budget {FLAP_BUDGET}')
    return triangles


def flap_axis(objects):
    """Centre of the flaps' combined footprint in X/Y, on the long axis of the body."""
    points = [o.matrix_world @ v.co for o in objects for v in o.data.vertices]
    xs = [p.x for p in points]
    ys = [p.y for p in points]
    return ((min(xs) + max(xs)) / 2, (min(ys) + max(ys)) / 2)


def _import(path):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=str(path))
    return [o for o in set(bpy.data.objects) - before if o.type == 'MESH']


def main():
    argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    folder = Path(argv[argv.index('--input') + 1]) if '--input' in argv else ROOT / 'Assets/Models/Anatomy/Skin'
    files = sorted(folder.glob('skin_flap_*.glb'))
    if len(files) != 2:
        raise RuntimeError(f'Expected two skin flaps in {folder}, found {len(files)}')
    bpy.ops.wm.read_factory_settings(use_empty=True)
    # The skin covers the muscles and the costal cartilage; omentum and diaphragm lie deeper.
    wall = []
    for path in sorted((folder.parent / 'BodyWall').glob('*.glb')):
        if 'muscle' in path.name or 'costal_cartilage' in path.name:
            wall += _import(path)
    flaps = {}
    for path in files:
        meshes = _import(path)
        if len(meshes) != 1:
            raise RuntimeError(f'{path} should hold one mesh, found {len(meshes)}')
        flaps[path] = meshes[0]
    axis = flap_axis(flaps.values())
    manifest_path = folder.parent / 'manifest.json'
    manifest = json.loads(manifest_path.read_text())
    for path, obj in flaps.items():
        finish_flap(obj, wall, axis)
        obj.name = path.stem
        export(obj, path, 'skin')
        # The exporter drops degenerate triangles, so record what the file actually holds.
        triangles = glb_triangles(path)
        for entry in manifest:
            if Path(entry['file']).name == path.name:
                entry['triangles'] = triangles
        print(f'{path.name}: {triangles} triangles')
    save_manifest(manifest_path.parent, manifest)


if __name__ == '__main__':
    main()
