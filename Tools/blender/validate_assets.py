"""Run: python3 Tools/blender/validate_assets.py. Check exported GLB geometry and budgets."""
import json
import math
import struct
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]

def glb_triangles(path):
    data=path.read_bytes()
    magic,version,length=struct.unpack_from('<4sII',data)
    if magic!=b'glTF' or version!=2 or length!=len(data):
        raise ValueError(f'Invalid GLB header: {path}')
    size,kind=struct.unpack_from('<II',data,12)
    if kind!=0x4E4F534A:
        raise ValueError(f'Missing GLB JSON: {path}')
    document=json.loads(data[20:20+size])
    total=0
    for mesh in document['meshes']:
        for primitive in mesh['primitives']:
            if primitive.get('mode',4)!=4:
                raise ValueError(f'Non-triangle primitive: {path}')
            accessor=primitive.get('indices',primitive['attributes']['POSITION'])
            count=document['accessors'][accessor]['count']
            if count%3:
                raise ValueError(f'Incomplete triangles: {path}')
            total+=count//3
    return total

def main():
    entries=json.loads((ROOT/'Assets/Models/Anatomy/manifest.json').read_text())
    assert len([e for e in entries if e['layer']=='organ'])==22, 'Expected 22 organs'
    assert len([e for e in entries if e['layer']=='skin'])==2, 'Expected two skin flaps'
    assert len({e['file'] for e in entries})==len(entries), 'Duplicate output path'
    for entry in entries:
        path=ROOT/entry['file']
        actual=glb_triangles(path)
        assert actual==entry['triangles'], f'Manifest mismatch: {path}'
        name=entry['original_name']
        budget=15000 if entry['layer']=='skin' else (30000 if 'lung' in name or any(s in name for s in ['atrium','ventricle']) else 20000)
        assert 0<actual<=budget, f'Triangle budget: {path}'
        assert all(math.isfinite(v) for v in entry['world_position']), f'Invalid transform: {path}'
        assert path.stat().st_size<100_000_000, f'Oversize asset: {path}'
    total=sum(e['triangles'] for e in entries)
    assert total<=360000, 'Combined anatomy budget exceeded'
    print(f'Validated {len(entries)} GLBs, including 22 organs and two skin flaps: {total:,} triangles')

if __name__=='__main__':
    main()
