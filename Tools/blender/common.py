"""Shared mesh operations. Imported by the command-line processing scripts."""
import argparse
import json
import re
import sys
from pathlib import Path
import bpy

ROOT = Path(__file__).resolve().parents[2]
ORGANS = ['Superior lobe of left lung', 'Inferior lobe of left lung',
 'Superior lobe of right lung', 'Middle lobe of right lung', 'Inferior lobe of right lung',
 'Left atrium', 'Right atrium', 'Left ventricle', 'Right ventricle', 'Liver', 'Gallbladder',
 'Stomach', 'Mucosa of stomach', 'Pancreas', 'Spleen', 'Kidney.l', 'Kidney.r', 'Duodenum',
 'Ascending colon', 'Transverse colon', 'Descending colon', 'Sigmoid colon']
WALL = ['Rectus abdominis muscle.l', 'Rectus abdominis muscle.r',
 'Transversus abdominis muscle.l', 'Transversus abdominis muscle.r',
 'Greater omentum', 'Lesser omentum', 'Diaphragm', 'Body of sternum']

def arguments(description, room=False):
    parser = argparse.ArgumentParser(description=description)
    parser.add_argument('--input', type=Path)
    parser.add_argument('--output', type=Path, default=ROOT / ('Assets/Models/Environment/lecture_hall.glb' if room else 'Assets/Models/Anatomy'))
    parser.add_argument('--budget', type=int, default=300000)
    args = parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else sys.argv[1:])
    if args.budget < 1:
        parser.error('--budget must be positive')
    if not args.input and not room:
        matches = list((ROOT / 'SourceAssets/Z-Anatomy').rglob('Startup.blend'))
        if len(matches) != 1:
            parser.error('Run fetch_zanatomy.sh first, or supply --input Startup.blend')
        args.input = matches[0]
    return args

def select(obj):
    bpy.ops.object.select_all(action='DESELECT')
    obj.hide_set(False)
    obj.hide_viewport = False
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj

def count(obj):
    obj.data.calc_loop_triangles()
    return len(obj.data.loop_triangles)

def simplify(obj, budget):
    select(obj)
    # Bake the world transform into the mesh, including parent transforms.
    world = obj.matrix_world.copy()
    # Preserve surfaces produced by the source's geometry modifiers.
    evaluated = obj.evaluated_get(bpy.context.evaluated_depsgraph_get())
    mesh = bpy.data.meshes.new_from_object(evaluated, preserve_all_data_layers=True, depsgraph=bpy.context.evaluated_depsgraph_get())
    obj.data = mesh
    obj.parent = None
    obj.matrix_world = world
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    for modifier in list(obj.modifiers):
        obj.modifiers.remove(modifier)
    for _ in range(6):
        total = count(obj)
        if total <= budget:
            break
        mod = obj.modifiers.new('Triangle budget', 'DECIMATE')
        mod.ratio = max(0.0001, budget / total * 0.98)
        bpy.ops.object.modifier_apply(modifier=mod.name)
    if count(obj) > budget:
        raise RuntimeError(f'{obj.name}: triangle budget exceeded')
    if count(obj) == 0:
        raise RuntimeError(f'{obj.name}: empty mesh')

def slug(name):
    return re.sub(r'[^a-z0-9]+', '_', name.lower()).strip('_')

def linked_color(socket):
    """Evaluate the simple color nodes (RGB, Gamma) feeding a socket; None if unsupported."""
    if not socket.is_linked:
        return tuple(socket.default_value)
    node = socket.links[0].from_node
    if node.type == 'RGB':
        return tuple(node.outputs[0].default_value)
    if node.type == 'GAMMA':
        color = linked_color(node.inputs['Color'])
        gamma = node.inputs['Gamma'].default_value
        if color is not None:
            return tuple(c ** gamma for c in color[:3]) + (color[3],)
    return None

def source_color(material):
    """Read the source's color controls without exporting unsupported shader groups."""
    def visit(tree, seen):
        if not tree or tree.name in seen:
            return None
        seen.add(tree.name)
        for node in tree.nodes:
            if node.type == 'GROUP' and 'Color2' in node.inputs:
                color = linked_color(node.inputs['Color2'])
                if color is not None:
                    return color
        for node in tree.nodes:
            if node.type == 'BSDF_PRINCIPLED':
                return tuple(node.inputs['Base Color'].default_value)
        for node in tree.nodes:
            if node.type == 'GROUP':
                color = visit(node.node_tree, seen)
                if color is not None:
                    return color
        return None
    return visit(material.node_tree, set()) or tuple(material.diffuse_color)


def export_materials(obj):
    for slot in obj.material_slots:
        source = slot.material
        if source is None:
            continue
        name = source.name + ' Export'
        material = bpy.data.materials.get(name)
        if material is None:
            material = bpy.data.materials.new(name)
            material.use_nodes = True
            color = source_color(source)
            material.diffuse_color = color
            node = material.node_tree.nodes.get('Principled BSDF')
            node.inputs['Base Color'].default_value = color
            node.inputs['Roughness'].default_value = 0.6
        slot.material = material


def export(obj, path, layer, original=None):
    export_materials(obj)
    path.parent.mkdir(parents=True, exist_ok=True)
    select(obj)
    bpy.ops.export_scene.gltf(filepath=str(path), export_format='GLB', use_selection=True,
                              export_yup=True, export_animations=False, export_cameras=False,
                              export_lights=False)
    return {'file':str(path.relative_to(ROOT)) if path.is_relative_to(ROOT) else str(path),
            'original_name':original or obj.name, 'layer':layer,
            'triangles':count(obj), 'world_position':list(obj.matrix_world.translation),
            'coordinate_system':'Blender Z-up; GLB converted to Y-up',
            'license':'CC BY-SA 4.0'}

def save_manifest(output, entries):
    output.mkdir(parents=True, exist_ok=True)
    (output / 'manifest.json').write_text(json.dumps(entries, indent=2)+'\n')

def load_source(path):
    bpy.ops.wm.open_mainfile(filepath=str(path), load_ui=False, use_scripts=False)
    for collection in bpy.data.collections:
        collection.hide_viewport = False
        collection.hide_render = False
    def enable(layer):
        layer.exclude = False
        layer.hide_viewport = False
        for child in layer.children:
            enable(child)
    enable(bpy.context.view_layer.layer_collection)
