// SINGLE SOURCE OF TRUTH for the stylized frog.
// Every client (headset, phone) and the server import this file, so part ids,
// labels and the "what covers what" rules can never drift apart.
//
// FROG SPACE: origin at the frog's centre, +z toward the head, +y up, +x to the
// frog's right. Units are arbitrary but fixed (the frog is about 3.3 units long).
// All poses sent over the network are expressed in frog space (see docs/PROTOCOL.md).
//
// To swap in a real glTF model later, keep these ids (the glTF mesh names must match)
// and keep `coveredBy` so the rules in protocol.js still work.

export const PARTS = [
  // ---- removable layers, outside to inside --------------------------------
  { id: 'skin',       label: 'Skin',        layer: 0, removable: true,  coveredBy: [],         color: 0x5fae4d, pos: [0, 0, 0],            size: [0.95, 0.55, 1.55] },
  { id: 'muscle',     label: 'Muscle',      layer: 1, removable: true,  coveredBy: ['skin'],   color: 0xb5483f, pos: [0, 0, 0],            size: [0.88, 0.48, 1.46] },

  // ---- organs (only reachable once the muscle layer is removed) -----------
  { id: 'heart',      label: 'Heart',       layer: 2, removable: true,  coveredBy: ['muscle'], color: 0xd62839, pos: [0, 0.05, 0.55],      size: [0.14, 0.14, 0.16] },
  { id: 'lung_l',     label: 'Left lung',   layer: 2, removable: true,  coveredBy: ['muscle'], color: 0xf4a6b8, pos: [-0.28, 0.1, 0.42],   size: [0.16, 0.14, 0.24] },
  { id: 'lung_r',     label: 'Right lung',  layer: 2, removable: true,  coveredBy: ['muscle'], color: 0xf4a6b8, pos: [0.28, 0.1, 0.42],    size: [0.16, 0.14, 0.24] },
  { id: 'liver',      label: 'Liver',       layer: 2, removable: true,  coveredBy: ['muscle'], color: 0x7a3b1d, pos: [0.14, -0.02, 0.05],  size: [0.34, 0.12, 0.22] },
  { id: 'stomach',    label: 'Stomach',     layer: 2, removable: true,  coveredBy: ['muscle'], color: 0xe8a33d, pos: [-0.2, -0.02, 0.0],   size: [0.2, 0.13, 0.22] },
  { id: 'intestines', label: 'Intestines',  layer: 2, removable: true,  coveredBy: ['muscle'], color: 0xe9d58a, pos: [0, -0.08, -0.5],     size: [0.42, 0.1, 0.34] },

  // ---- static body parts (never removed, never covered) -------------------
  { id: 'head',       label: 'Head',        layer: -1, removable: false, coveredBy: [],        color: 0x5fae4d, pos: [0, 0.12, 1.55],      size: [0.5, 0.38, 0.45] },
  { id: 'eye_l',      label: 'Left eye',    layer: -1, removable: false, coveredBy: [],        color: 0xf2e86d, pos: [-0.22, 0.42, 1.66],  size: [0.09, 0.09, 0.09] },
  { id: 'eye_r',      label: 'Right eye',   layer: -1, removable: false, coveredBy: [],        color: 0xf2e86d, pos: [0.22, 0.42, 1.66],   size: [0.09, 0.09, 0.09] },
  { id: 'foreleg_l',  label: 'Left foreleg',  layer: -1, removable: false, coveredBy: [],      color: 0x4e9a3f, pos: [-0.8, -0.32, 0.85],  size: [0.4, 0.1, 0.14] },
  { id: 'foreleg_r',  label: 'Right foreleg', layer: -1, removable: false, coveredBy: [],      color: 0x4e9a3f, pos: [0.8, -0.32, 0.85],   size: [0.4, 0.1, 0.14] },
  { id: 'hindleg_l',  label: 'Left hind leg',  layer: -1, removable: false, coveredBy: [],     color: 0x4e9a3f, pos: [-0.8, -0.28, -0.85], size: [0.5, 0.14, 0.3] },
  { id: 'hindleg_r',  label: 'Right hind leg', layer: -1, removable: false, coveredBy: [],     color: 0x4e9a3f, pos: [0.8, -0.28, -0.85],  size: [0.5, 0.14, 0.3] },
];

export const PART_BY_ID = Object.fromEntries(PARTS.map((p) => [p.id, p]));
