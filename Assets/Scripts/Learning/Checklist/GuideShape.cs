using UnityEngine;
using UnityEngine.Rendering;

namespace DIYsect.Learning
{
    /// <summary>
    /// One flat, tinted panel shape (rounded rectangle, disc or ring) that builds its own small mesh, so the
    /// guide panel needs no art assets. The colour goes through a property block on the shared guide material.
    /// Replaces <see cref="ChecklistBacking"/> quads.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class GuideShape : MonoBehaviour
    {
        private const int DefaultRoundSegments = 24;
        private const int DefaultCornerSegments = 6;

        [SerializeField] private GuideShapeKind _kind = GuideShapeKind.RoundedRect;
        [SerializeField, Tooltip("Width and height. For a disc or ring, x is the diameter.")]
        private Vector2 _size = new Vector2(0.1f, 0.05f);
        [SerializeField, Min(0f), Tooltip("Corner radius of a rounded rectangle, in metres.")]
        private float _radius = 0.01f;
        [SerializeField, Min(0f), Tooltip("Band width of a ring, in metres.")]
        private float _thickness = 0.004f;
        [SerializeField, Min(0), Tooltip("Segments: per corner for a rounded rectangle, around for a disc or ring. 0 uses a sensible default.")]
        private int _segments = 0;
        [SerializeField] private Color _color = Color.white;
        [SerializeField] private int _sortingOrder;

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private Mesh _mesh;
        private MeshFilter _filter;
        private MeshRenderer _renderer;
        private MaterialPropertyBlock _block;

        public GuideShapeKind Kind => _kind;
        public Vector2 Size => _size;

        /// <summary>The tint. Setting it repaints at once.</summary>
        public Color Color
        {
            get => _color;
            set
            {
                if (_color == value) return;
                _color = value;
                ApplyColor();
            }
        }

        /// <summary>Changes the width and height (the diameter in x for a disc or ring) and rebuilds the mesh.</summary>
        public void SetSize(Vector2 size)
        {
            if (_size == size) return;
            _size = size;
            if (isActiveAndEnabled) Rebuild();
        }

        /// <summary>Switches between rounded rectangle, disc and ring and rebuilds the mesh.</summary>
        public void SetKind(GuideShapeKind kind)
        {
            if (_kind == kind) return;
            _kind = kind;
            if (isActiveAndEnabled) Rebuild();
        }

        private void OnEnable()
        {
            Apply();
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            // Unity forbids touching meshes inside OnValidate, so the rebuild waits one editor tick.
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null && isActiveAndEnabled) Apply();
            };
#endif
        }

        private void OnDisable()
        {
            ReleaseMesh();
        }

        private void OnDestroy()
        {
            ReleaseMesh();
        }

        private void Apply()
        {
            if (_filter == null) _filter = GetComponent<MeshFilter>();
            if (_renderer == null) _renderer = GetComponent<MeshRenderer>();
            if (_filter == null || _renderer == null) return;
            _renderer.shadowCastingMode = ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            _renderer.sortingOrder = _sortingOrder;
            Rebuild();
            ApplyColor();
        }

        private void Rebuild()
        {
            if (_filter == null) return;
            Vector3[] vertices;
            int[] triangles;
            switch (_kind)
            {
                case GuideShapeKind.Disc:
                    GuideShapeGeometry.Disc(_size.x, _segments > 0 ? _segments : DefaultRoundSegments, out vertices, out triangles);
                    break;
                case GuideShapeKind.Ring:
                    GuideShapeGeometry.Ring(_size.x, _thickness, _segments > 0 ? _segments : DefaultRoundSegments, out vertices, out triangles);
                    break;
                default:
                    GuideShapeGeometry.RoundedRect(_size, _radius, _segments > 0 ? _segments : DefaultCornerSegments, out vertices, out triangles);
                    break;
            }
            if (_mesh == null)
            {
                _mesh = new Mesh { name = "GuideShape", hideFlags = HideFlags.DontSave };
            }
            _mesh.Clear();
            _mesh.vertices = vertices;
            var normals = new Vector3[vertices.Length];
            for (int i = 0; i < normals.Length; i++) normals[i] = Vector3.back;
            _mesh.normals = normals;
            _mesh.triangles = triangles;
            _mesh.RecalculateBounds();
            _filter.sharedMesh = _mesh;
        }

        private void ApplyColor()
        {
            if (_renderer == null) return;
            _block ??= new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(_block);
            _block.SetColor(BaseColor, _color);
            _renderer.SetPropertyBlock(_block);
        }

        private void ReleaseMesh()
        {
            if (_mesh == null) return;
            if (_filter != null && _filter.sharedMesh == _mesh) _filter.sharedMesh = null;
            if (Application.isPlaying) Destroy(_mesh);
            else DestroyImmediate(_mesh);
            _mesh = null;
        }
    }
}
