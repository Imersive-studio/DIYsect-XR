// Authority: none. Presentation only; it reads the pins the server owns and never writes them.
using System.Collections.Generic;
using UnityEngine;

namespace DIYsect.Classroom
{
    /// <summary>
    /// Shows the pins on this client. Whenever the pin list changes it rebuilds the cluster markers.
    /// Each marker is a child of its organ transform at the cluster's organ-local centre, so it moves
    /// with the organ on every client without any per-frame code. A cluster glows (colour and size) with its open count; an answered pin is grey and
    /// marked in its label, and a cluster whose pins are all answered does not glow.
    /// </summary>
    [RequireComponent(typeof(QuestionBoard))]
    public class QuestionPinPresenter : MonoBehaviour
    {
        [SerializeField, Min(0.001f), Tooltip("Pins on the same organ closer than this, in metres, form one cluster.")]
        private float _clusterRadius = 0.03f;
        [SerializeField, Min(0.001f), Tooltip("Marker diameter in metres for a single question.")]
        private float _baseSize = 0.012f;
        [SerializeField, Min(0f), Tooltip("Extra diameter in metres added at full glow.")]
        private float _glowSize = 0.02f;
        [SerializeField] private Color _calmColor = new Color(0.2f, 0.6f, 1f);
        [SerializeField] private Color _glowColor = new Color(1f, 0.85f, 0.1f);
        [SerializeField] private Color _answeredColor = new Color(0.45f, 0.45f, 0.45f);
        [SerializeField, Tooltip("Unlit material used for the markers. Its colour is set per marker.")]
        private Material _markerMaterial;

        private static readonly int _baseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int _colorId = Shader.PropertyToID("_Color");

        private QuestionBoard _board;
        private PinClusterer _clusterer;
        private readonly List<PinMarker> _markers = new List<PinMarker>();
        private MaterialPropertyBlock _block;

        /// <summary>The markers shown right now, one per cluster.</summary>
        public IReadOnlyList<PinMarker> Markers => _markers;

        private void Awake()
        {
            _board = GetComponent<QuestionBoard>();
            if (_markerMaterial == null)
            {
                Debug.LogError($"{name}: QuestionPinPresenter has no marker material. Run Tools > DIYsect > Add Question Pins.", this);
                enabled = false;
                return;
            }
            _clusterer = new PinClusterer(_clusterRadius);
            _block = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            _board.PinsChanged += Rebuild;
            Rebuild();
        }

        private void OnDisable()
        {
            _board.PinsChanged -= Rebuild;
            ClearMarkers();
        }

        private void Rebuild()
        {
            ClearMarkers();
            if (!_board.IsSpawned)
                return;

            var pins = new List<QuestionPin>(_board.Count);
            for (int i = 0; i < _board.Count; i++)
                pins.Add(_board[i]);

            foreach (PinCluster cluster in _clusterer.Build(pins))
                _markers.Add(CreateMarker(cluster, pins));
        }

        private PinMarker CreateMarker(PinCluster cluster, List<QuestionPin> pins)
        {
            Transform organ = _board.Body.Organs[cluster.OrganIndex].transform;
            float glow = cluster.AllAnswered ? 0f : PinClusterer.Glow(cluster.OpenCount);
            float diameter = _baseSize + _glowSize * glow;

            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = $"Pin x{cluster.Count}";
            Destroy(sphere.GetComponent<Collider>());
            Transform marker = sphere.transform;
            marker.SetParent(organ, false);
            marker.localPosition = cluster.Centre;
            // Cancel the organ's scale so the size is in metres.
            Vector3 scale = organ.lossyScale;
            marker.localScale = new Vector3(diameter / scale.x, diameter / scale.y, diameter / scale.z);

            var renderer = sphere.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = _markerMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            Color color = cluster.AllAnswered ? _answeredColor : Color.Lerp(_calmColor, _glowColor, glow);
            renderer.GetPropertyBlock(_block);
            _block.SetColor(_baseColorId, color);
            _block.SetColor(_colorId, color);
            renderer.SetPropertyBlock(_block);

            AddLabel(marker, cluster, pins, diameter);

            var pinMarker = sphere.AddComponent<PinMarker>();
            pinMarker.Show(cluster, glow);
            return pinMarker;
        }

        // See PinLabels for the wording.
        private void AddLabel(Transform marker, PinCluster cluster, List<QuestionPin> pins, float diameter)
        {
            string label = PinLabels.ForCluster(cluster.Count, cluster.OpenCount, cluster.Count == 1 ? TextOf(pins, cluster.PinIds[0]) : null);

            var labelObject = new GameObject("Label");
            labelObject.transform.SetParent(marker, false);
            labelObject.transform.position = marker.position + Vector3.up * diameter;
            var text = labelObject.AddComponent<TextMesh>();
            text.text = label;
            text.anchor = TextAnchor.LowerCenter;
            text.alignment = TextAlignment.Center;
            text.characterSize = 0.01f;
            text.fontSize = 48;
            text.color = cluster.AllAnswered ? Color.Lerp(_answeredColor, Color.white, 0.4f) : Color.white;
            // Keep the label the same world size whatever the organ scale.
            Vector3 scale = marker.lossyScale;
            labelObject.transform.localScale = new Vector3(1f / scale.x, 1f / scale.y, 1f / scale.z);
            labelObject.AddComponent<PinLabelBillboard>();
        }

        private static string TextOf(List<QuestionPin> pins, int id)
        {
            foreach (QuestionPin pin in pins)
                if (pin.Id == id)
                    return pin.Text.ToString();
            return "?";
        }

        private void ClearMarkers()
        {
            foreach (PinMarker marker in _markers)
                if (marker != null)
                    Destroy(marker.gameObject);
            _markers.Clear();
        }
    }
}
