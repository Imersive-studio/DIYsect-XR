// Authority: none. Local presentation only; it reads head poses every peer already receives and sends nothing.
using DIYsect.Networking;
using UnityEngine;
using UnityEngine.Rendering;
using XRMultiplayer;

namespace DIYsect.Classroom
{
    /// <summary>
    /// A soft dot on the body wherever the professor or the student is looking. Each device works it out itself: it takes the
    /// head pose from the <see cref="HeadPoseBoard"/> (body local space), turns it into world space with the body, and casts
    /// a ray along the head's forward direction. The dot glides to the hit, lies flat on the surface and fades out when the
    /// head looks away for a while or leaves. A device never shows the dot of its own head.
    /// </summary>
    public class GazePresenter : MonoBehaviour
    {
        private static readonly int _baseColorId = Shader.PropertyToID("_BaseColor");
        private const string FallbackShader = "Universal Render Pipeline/Unlit";

        [SerializeField] private HeadPoseBoard _board;
        [SerializeField, Tooltip("One unlit transparent material shared by both dots.")]
        private Material _material;
        [SerializeField] private Color _professorColor = new Color(1f, 0.6f, 0.1f);
        [SerializeField] private Color _studentColor = new Color(0.2f, 0.8f, 1f);
        [SerializeField, Range(0.1f, 1f), Tooltip("Opacity of a fully shown dot.")]
        private float _opacity = 0.6f;
        [SerializeField, Min(0.005f), Tooltip("Dot diameter in metres.")]
        private float _diameter = 0.03f;
        [SerializeField, Min(0.0005f), Tooltip("Metres the dot floats above the surface, so it does not flicker against it.")]
        private float _surfaceOffset = 0.004f;
        [SerializeField, Min(0.5f), Tooltip("Longest gaze ray in metres.")]
        private float _maxDistance = 3f;
        [SerializeField, Min(1f), Tooltip("How fast the dot follows its target. Higher is quicker.")]
        private float _followRate = 14f;

        private sealed class Dot
        {
            public Transform Transform;
            public Renderer Renderer;
            public bool HasPosition;
            public Vector3 Position;
            public Vector3 Normal = Vector3.up;
            public Vector3 TargetPosition;
            public Vector3 TargetNormal = Vector3.up;
            public float LastHitAt = float.NegativeInfinity;
            public float LastCastAt = float.NegativeInfinity;
            public float Alpha;
        }

        private readonly Dot[] _dots = new Dot[2];
        private MaterialPropertyBlock _block;
        private PointerPresenter _colors;
        private Mesh _mesh;
        private int _mask;
        private bool _ownOverridden;
        private PointerRole? _ownOverride;

        public bool IsShown(PointerRole role) => DotOf(role) != null && DotOf(role).Alpha > 0f;
        public float AlphaOf(PointerRole role) => DotOf(role)?.Alpha ?? 0f;
        public Vector3 DotPosition(PointerRole role) => DotOf(role)?.Position ?? Vector3.zero;
        public Vector3 DotNormal(PointerRole role) => DotOf(role)?.Normal ?? Vector3.up;

        /// <summary>Decides which head is mine by hand (null means none, like a phone). For tests; normally it is worked out.</summary>
        public void OverrideOwnSlot(PointerRole? slot)
        {
            _ownOverridden = true;
            _ownOverride = slot;
        }

        private void Awake()
        {
            if (_board == null)
                _board = GetComponentInParent<HeadPoseBoard>() ?? GetComponentInChildren<HeadPoseBoard>(true);
            if (_board == null)
            {
                Debug.LogError($"{name}: GazePresenter has no HeadPoseBoard. Run Tools > DIYsect > Add Shared Gaze.", this);
                enabled = false;
                return;
            }
            _colors = _board.GetComponent<PointerPresenter>();
            if (_material == null)
            {
                Shader shader = Shader.Find(FallbackShader);
                if (shader != null)
                    _material = new Material(shader) { name = "GazeDotFallback" };
            }
            _block = new MaterialPropertyBlock();
            _mask = LayerMask.GetMask("Default", "SeatedOrgan", "LooseOrgan");
            _mesh = BuildDiscMesh();
            _dots[0] = MakeDot("GazeProfessor");
            _dots[1] = MakeDot("GazeStudent");
        }

        private void OnDestroy()
        {
            if (_mesh != null)
                Destroy(_mesh);
        }

        private void Update()
        {
            float now = Time.unscaledTime;
            PointerRole? own = _ownOverridden ? _ownOverride : GazeRule.OwnSlot(ClassroomRole.Current != ClassroomRoleKind.Classmate, _board.IsServer);
            foreach (PointerRole role in new[] { PointerRole.Professor, PointerRole.Student })
                Step(role, DotOf(role), own.HasValue && own.Value == role, now);
        }

        private void Step(PointerRole role, Dot dot, bool isOwn, float now)
        {
            bool present = _board.TryGetLocalPose(role, out Vector3 localPosition, out Quaternion localRotation);
            if (present && !isOwn && GazeRule.ShouldCast(now, dot.LastCastAt))
            {
                dot.LastCastAt = now;
                Transform body = _board.transform;
                Vector3 origin = body.TransformPoint(localPosition);
                Vector3 direction = body.rotation * localRotation * Vector3.forward;
                if (Physics.Raycast(origin, direction, out RaycastHit hit, _maxDistance, _mask, QueryTriggerInteraction.Ignore))
                {
                    dot.TargetPosition = hit.point + hit.normal * _surfaceOffset;
                    dot.TargetNormal = hit.normal;
                    dot.LastHitAt = now;
                }
            }

            float age = dot.LastHitAt == float.NegativeInfinity ? float.NaN : now - dot.LastHitAt;
            dot.Alpha = GazeRule.Alpha(present, isOwn, age);
            if (dot.Alpha <= 0f)
            {
                dot.HasPosition = false; // the next appearance starts at its target instead of gliding from far away
                dot.Renderer.enabled = false;
                return;
            }

            if (!dot.HasPosition)
            {
                dot.Position = dot.TargetPosition;
                dot.Normal = dot.TargetNormal;
                dot.HasPosition = true;
            }
            else
            {
                float t = GazeRule.SmoothFactor(Time.unscaledDeltaTime, _followRate);
                dot.Position = Vector3.Lerp(dot.Position, dot.TargetPosition, t);
                Vector3 normal = Vector3.Lerp(dot.Normal, dot.TargetNormal, t);
                dot.Normal = normal.sqrMagnitude > 1e-6f ? normal.normalized : dot.TargetNormal;
            }
            Show(role, dot);
        }

        private void Show(PointerRole role, Dot dot)
        {
            Transform t = dot.Transform;
            t.SetPositionAndRotation(dot.Position, Quaternion.FromToRotation(Vector3.up, dot.Normal));
            // The parent may be scaled; keep the dot a fixed size in the world.
            float parentScale = Mathf.Max(1e-4f, transform.lossyScale.x);
            t.localScale = Vector3.one * (_diameter / parentScale);
            Color color = ColorOf(role);
            color.a = _opacity * dot.Alpha;
            dot.Renderer.GetPropertyBlock(_block);
            _block.SetColor(_baseColorId, color);
            dot.Renderer.SetPropertyBlock(_block);
            dot.Renderer.enabled = true;
        }

        private Color ColorOf(PointerRole role)
        {
            if (_colors != null)
                return _colors.ColorOf(role);
            return role == PointerRole.Professor ? _professorColor : _studentColor;
        }

        private Dot DotOf(PointerRole role)
        {
            if (_dots[0] == null)
                return null;
            return role == PointerRole.Professor ? _dots[0] : role == PointerRole.Student ? _dots[1] : null;
        }

        private Dot MakeDot(string dotName)
        {
            var go = new GameObject(dotName);
            go.transform.SetParent(transform, false);
            go.layer = 0; // Default: the headset camera always draws it
            go.AddComponent<MeshFilter>().sharedMesh = _mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.enabled = false;
            return new Dot { Transform = go.transform, Renderer = renderer };
        }

        // A flat disc of diameter 1 lying in the XZ plane, facing up (+Y), seen from above.
        private static Mesh BuildDiscMesh()
        {
            const int segments = 32;
            var vertices = new Vector3[segments + 1];
            var triangles = new int[segments * 3];
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                vertices[i + 1] = new Vector3(Mathf.Cos(angle) * 0.5f, 0f, Mathf.Sin(angle) * 0.5f);
                int next = (i + 1) % segments + 1;
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = next;
                triangles[i * 3 + 2] = i + 1;
            }
            var mesh = new Mesh { name = "GazeDisc", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
