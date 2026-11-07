// Builds the stylized frog from shared/parts.js. Used by BOTH the headset client and
// the audience client so they always show the same thing. Pass in the THREE module.
//
// Returns { group, meshes, applyRemoved, setGlow, clearGlow }.
// Every mesh has userData.partId, which is what raycasts read.

import { PARTS } from './parts.js';

export function buildFrog(THREE) {
  const group = new THREE.Group();
  const meshes = new Map();
  const geometry = new THREE.SphereGeometry(1, 32, 20); // shared, scaled per part

  for (const part of PARTS) {
    const material = new THREE.MeshStandardMaterial({
      color: part.color,
      roughness: 0.55,
      metalness: 0,
      emissive: 0x000000,
    });
    const mesh = new THREE.Mesh(geometry, material);
    mesh.position.set(...part.pos);
    mesh.scale.set(...part.size);
    mesh.userData.partId = part.id;
    group.add(mesh);
    meshes.set(part.id, mesh);
  }

  return {
    group,
    meshes,

    /** Show/hide parts so that exactly the ids in `removedIds` are hidden. */
    applyRemoved(removedIds) {
      const removed = new Set(removedIds);
      for (const [id, mesh] of meshes) mesh.visible = !removed.has(id);
    },

    /** Light a part up (hover, pointer, selection). color is a hex number. */
    setGlow(id, color = 0xffffff, intensity = 0.5) {
      const mesh = meshes.get(id);
      if (!mesh) return;
      mesh.material.emissive.setHex(color);
      mesh.material.emissiveIntensity = intensity;
    },

    clearGlow() {
      for (const mesh of meshes.values()) mesh.material.emissive.setHex(0x000000);
    },
  };
}
