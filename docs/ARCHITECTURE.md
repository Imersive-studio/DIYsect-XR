# Unity architecture and acceptance checklist

## Composition

Start with the working Mixed Reality Multiplayer Tabletop template. Keep one scene composition root and wire serialized dependencies through the Inspector. Each component has one responsibility; interaction logic must not own transport or classroom UI. Use narrow interfaces and ScriptableObjects when implementation needs them, not placeholder abstractions.

| Assembly | Responsibility | Authority / dependency boundary |
|---|---|---|
| Networking | Session, shared organ pose, ownership and layer state | Server validates dissection transitions and ownership requests |
| Interaction | Grab, socket, scalpel and guiding hand | Student steers; professor may own depth only; visible ownership indicator |
| Dissection | Incision progress, flap state, muscle and omentum order | Shared state comes from Networking; events drive presentation |
| Classroom | Organ-local question anchors, clusters and screen | Server accepts pins; clients reconstruct anchor transforms and render their own close-up camera |
| MixedReality | Lines the virtual table and body up with a real table in passthrough | Local to one client, no network state. References XRI and AR Foundation only, never Networking |

Add explicit assembly references when components are implemented. Do not introduce another XR or networking framework. State authority at the top of each network behavior. Validate references in Awake/OnValidate, use events instead of per-frame polling where possible, and keep private serialized fields.

## Anatomy and scene integration

Keep all mesh transforms under one body root. Blender world positions in the manifest are Z-up; GLB coordinates are Y-up. Do not apply either transform twice. Check dimensions with a meter cube and apply a shared uniform scale if necessary. The pipeline preserves the source units rather than asserting anatomical scale.

The prototype skin follows abdominal and pectoral muscles. Check silhouette, holes, normals and left/right midline coverage before the demo. Set flap hinges at the outer body edges in Unity; export origins are not animation pivots. It is not a watertight clinical body model.

Use primitive colliders for interactions where practical. Keep the whole scene under approximately 750k triangles, including avatars, room, props and active tools. Check target-device frame timing. EzySlice is a planar-cut fallback, not an anatomical incision solver.

## Manual acceptance gates

- [ ] Hub template exists in this clone, editor/package versions committed, Unity Console has no compile errors.
- [ ] Android and desktop OpenXR validation passes; simulator starts on Mac.
- [ ] GLBs or equivalent FBXs import with color, scale and organ alignment intact.
- [ ] Prefab menu creates table, tray and screen; running twice preserves edits.
- [ ] Three scenes retain functioning template rigs and correct device input.
- [ ] Two clients agree on organ transforms, ownership and dissection layer state.
- [ ] Student controls scalpel steering while professor controls depth; release restores normal control.
- [ ] Skin, muscle and omentum open in order; organs snap back with feedback.
- [ ] Classmate pins remain attached when organs move; clients see clustered questions.
- [ ] Close-up screen shows the body without rendering itself recursively.
- [ ] Standalone Quest APK passes a headset test; record measured scene triangles and frame timing.
- [ ] Record a 3–5 minute video with code explanation and backup footage before the deadline.

These are outstanding product checks, not claims that scaffold or offline asset tests satisfy them.
