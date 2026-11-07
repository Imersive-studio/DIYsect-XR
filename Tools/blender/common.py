"""Shared mesh operations. Imported by the command-line processing scripts."""
import argparse
import colorsys
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

def _rgba(value):
    if hasattr(value, '__len__'):
        return tuple(value) + (1.0,) * (4 - len(value))
    return (value, value, value, 1.0)

def _blend(kind, factor, a, b):
    if kind == 'MIX':
        return [x + (y - x) * factor for x, y in zip(a, b)]
    if kind == 'MULTIPLY':
        return [x * (1 - factor) + x * y * factor for x, y in zip(a, b)]
    if kind == 'SCREEN':
        return [1 - ((1 - factor) + factor * (1 - y)) * (1 - x) for x, y in zip(a, b)]
    if kind == 'ADD':
        return [x + factor * y for x, y in zip(a, b)]
    raise ValueError(f'unsupported mix blend {kind}')

def evaluate(socket, group_inputs=None):
    """Evaluate the simple color nodes feeding a socket to one RGBA constant.

    Handles RGB, Gamma, Mix, Hue/Saturation/Value, Bright/Contrast, linear ColorRamp,
    reroutes and group inputs. Attribute nodes read 0, because the source drives them with
    per-object custom properties (key_color, comic_shader) that are 0 on every object.
    Ambient Occlusion reads as unoccluded (1). Anything else raises ValueError.
    """
    if not socket.is_linked:
        return _rgba(socket.default_value)
    link = socket.links[0]
    node = link.from_node
    def value(name):
        return evaluate(node.inputs[name], group_inputs)
    if node.type == 'REROUTE':
        return evaluate(node.inputs[0], group_inputs)
    if node.type == 'RGB':
        return _rgba(node.outputs[0].default_value)
    if node.type == 'GROUP_INPUT':
        if group_inputs is None or link.from_socket.name not in group_inputs:
            raise ValueError('unbound group input')
        return group_inputs[link.from_socket.name]
    if node.type == 'ATTRIBUTE':
        return _rgba(0.0)
    if node.type == 'AMBIENT_OCCLUSION':
        return _rgba(1.0)
    if node.type == 'GAMMA':
        color, gamma = value('Color'), value('Gamma')[0]
        return tuple(c ** gamma for c in color[:3]) + (color[3],)
    if node.type == 'VALTORGB':
        return tuple(node.color_ramp.evaluate(value('Fac')[0]))
    if node.type == 'MIX' and node.data_type == 'RGBA':
        pick = lambda name: evaluate(next(i for i in node.inputs if i.name == name and i.type == 'RGBA'), group_inputs)
        factor = value('Factor')[0]
        if node.clamp_factor:
            factor = min(max(factor, 0.0), 1.0)
        return tuple(_blend(node.blend_type, factor, pick('A'), pick('B')))
    if node.type == 'BRIGHTCONTRAST':
        color, bright, contrast = value('Color'), value('Bright')[0], value('Contrast')[0]
        gain, offset = 1 + contrast, bright - contrast * 0.5
        return tuple(max(c * gain + offset, 0.0) for c in color[:3]) + (color[3],)
    if node.type == 'HUE_SAT':
        color, factor = value('Color'), value('Fac')[0]
        hue, sat, val = colorsys.rgb_to_hsv(*color[:3])
        shifted = colorsys.hsv_to_rgb((hue + value('Hue')[0] - 0.5) % 1.0, min(sat * value('Saturation')[0], 1.0),
                                      val * value('Value')[0])
        return tuple(c + (n - c) * factor for c, n in zip(color[:3], shifted)) + (color[3],)
    raise ValueError(f'unsupported node {node.type}')

def group_color(node):
    """Visible base color of a shader group: its Principled Base Color with Color2 bound."""
    try:
        inputs = {socket.name: evaluate(socket) for socket in node.inputs}
        principled = next(n for n in node.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
        return evaluate(principled.inputs['Base Color'], inputs)
    except (ValueError, StopIteration, KeyError):
        pass
    try:
        return evaluate(node.inputs['Color2'])
    except ValueError:
        return None

def source_color(material):
    """Read the source's color controls without exporting unsupported shader groups."""
    def visit(tree, seen):
        if not tree or tree.name in seen:
            return None
        seen.add(tree.name)
        for node in tree.nodes:
            if node.type == 'GROUP' and 'Color2' in node.inputs:
                color = group_color(node)
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
