// Authority: none. Presentation only; it reads the pointers the server owns and the dissection stage, and never writes them.
using System.Collections.Generic;
using DIYsect.Dissection;
using DIYsect.Interaction;
using DIYsect.Networking;
using UnityEngine;

namespace DIYsect.Classroom
{
    /// <summary>
    /// Works out what a classmate sees for each active pointer: the organ's name, who points, whether a closed layer still
    /// hides it (from the real dissection stage and <see cref="OrganCover"/>), and where it is on this screen. It also
    /// tints the pointed organ with a gentle pulse in the role's colour. <see cref="PointerHud"/> draws the markers.
    /// The entries are rebuilt only when a pointer or the stage changes; the per-frame step runs only while a pointer is active.
    /// </summary>
    [RequireComponent(typeof(PointerBoard), typeof(SharedBody), typeof(DissectionProgress))]
    public class PointerPresenter : MonoBehaviour
    {
        private static readonly int _baseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int _colorId = Shader.PropertyToID("_Color");

        [SerializeField] private Color _professorColor = new Color(1f, 0.6f, 0.1f);
        [SerializeField] private Color _studentColor = new Color(0.2f, 0.8f, 1f);
        [SerializeField, Range(0f, 1f), Tooltip("How far the organ's colour moves toward the pointer colour at the peak of the pulse.")]
        private float _tintStrength = 0.45f;
        [SerializeField, Min(0.2f), Tooltip("Seconds per pulse.")]
        private float _pulseSeconds = 1.6f;
        [SerializeField, Range(0.01f, 0.2f), Tooltip("Edge arrows sit this far (fraction of the screen) inside the screen edge.")]
        private float _edgeMargin = 0.07f;
        [SerializeField, Range(0.02f, 0.4f), Tooltip("Two edge arrows closer than this (fraction of the screen) are pushed apart.")]
        private float _arrowSeparation = 0.12f;

        private readonly List<PointerEntry> _entries = new List<PointerEntry>();
        private readonly List<Renderer[]> _renderers = new List<Renderer[]>();
        private readonly List<Color[]> _baseColors = new List<Color[]>();
        private PointerBoard _board;
        private SharedBody _body;
        private DissectionProgress _progress;
        private Camera _viewCamera;
        private MaterialPropertyBlock _block;

        /// <summary>The pointers shown right now: the professor's first, then the student's.</summary>
        public IReadOnlyList<PointerEntry> Entries => _entries;

        public float Pulse { get; private set; }

        public float EdgeMargin => _edgeMargin;

        public Color ColorOf(PointerRole role) => role == PointerRole.Professor ? _professorColor : _studentColor;

        /// <summary>Sets the camera the placements are computed for. Without one, the scene's main camera is used.</summary>
        public void SetViewCamera(Camera camera)
        {
            _viewCamera = camera;
        }

        private void Awake()
        {
            _board = GetComponent<PointerBoard>();
            _body = GetComponent<SharedBody>();
            _progress = GetComponent<DissectionProgress>();
            _block = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            _board.PointersChanged += Rebuild;
            _progress.StageChanged += HandleStageChanged;
            Rebuild();
        }

        private void OnDisable()
        {
            _board.PointersChanged -= Rebuild;
            _progress.StageChanged -= HandleStageChanged;
            ClearEntries();
        }

        private void HandleStageChanged(DissectionStage stage, bool initial) => Rebuild();

        private void Rebuild()
        {
            ClearEntries();
            if (!_board.IsSpawned)
                return;
            foreach (PointerRole role in new[] { PointerRole.Professor, PointerRole.Student })
            {
                int index = _board.OrganOf(role);
                if (index < 0 || index >= _body.Organs.Count)
                    continue;
                Transform organ = _body.Organs[index].transform;
                string hidingLayer = OrganCover.Default.HidingLayer(organ.name, _progress.Stage);
                _entries.Add(new PointerEntry(role, index, OrganNames.Display(organ.name), hidingLayer));
                Renderer[] renderers = organ.GetComponentsInChildren<Renderer>();
                var colors = new Color[renderers.Length];
                for (int i = 0; i < renderers.Length; i++)
                    colors[i] = BaseColorOf(renderers[i]);
                _renderers.Add(renderers);
                _baseColors.Add(colors);
            }
        }

        private void ClearEntries()
        {
            foreach (Renderer[] renderers in _renderers)
                foreach (Renderer renderer in renderers)
                    if (renderer != null)
                        renderer.SetPropertyBlock(null);
            _entries.Clear();
            _renderers.Clear();
            _baseColors.Clear();
        }

        private void LateUpdate()
        {
            if (_entries.Count == 0)
                return;
            Pulse = PointerPulse.Value(Time.unscaledTime, _pulseSeconds);
            Tint();
            UpdatePlacements();
        }

        /// <summary>Recomputes where each pointer is on screen. Runs every frame while a pointer is active; public so tests can call it.</summary>
        public void UpdatePlacements()
        {
            Camera view = _viewCamera != null ? _viewCamera : Camera.main;
            if (view == null)
                return;
            for (int i = 0; i < _entries.Count; i++)
            {
                Vector3 viewport = view.WorldToViewportPoint(CentreOf(_renderers[i]));
                _entries[i].Placement = PointerScreenMath.Place(viewport, _edgeMargin);
            }
            if (_entries.Count == 2 && !_entries[0].Placement.OnScreen && !_entries[1].Placement.OnScreen)
            {
                PointerPlacement fixedOne = _entries[0].Placement;
                PointerPlacement moving = _entries[1].Placement;
                Vector2 spread = PointerScreenMath.Separate(fixedOne.Position, moving.Position, _arrowSeparation, _edgeMargin);
                _entries[1].Placement = new PointerPlacement(false, spread, moving.AngleDegrees);
            }
        }

        private static Vector3 CentreOf(Renderer[] renderers)
        {
            Bounds bounds = default;
            bool first = true;
            foreach (Renderer renderer in renderers)
            {
                if (first) { bounds = renderer.bounds; first = false; }
                else bounds.Encapsulate(renderer.bounds);
            }
            return bounds.center;
        }

        private void Tint()
        {
            for (int e = 0; e < _entries.Count; e++)
            {
                Color target = ColorOf(_entries[e].Role);
                for (int i = 0; i < _renderers[e].Length; i++)
                {
                    Renderer renderer = _renderers[e][i];
                    if (renderer == null)
                        continue;
                    Color tinted = Color.Lerp(_baseColors[e][i], target, _tintStrength * Pulse);
                    renderer.GetPropertyBlock(_block);
                    _block.SetColor(_baseColorId, tinted);
                    _block.SetColor(_colorId, tinted);
                    renderer.SetPropertyBlock(_block);
                }
            }
        }

        private static Color BaseColorOf(Renderer renderer)
        {
            Material material = renderer.sharedMaterial;
            if (material == null)
                return Color.white;
            if (material.HasProperty(_baseColorId))
                return material.GetColor(_baseColorId);
            return material.HasProperty(_colorId) ? material.GetColor(_colorId) : Color.white;
        }
    }
}
