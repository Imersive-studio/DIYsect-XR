// Authority: none. Presentation only.
using DIYsect.Interaction;
using DIYsect.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace DIYsect.Learning
{
    /// <summary>
    /// The inspection title of one organ: a small spaced out "ANATOMICAL STRUCTURE" line, the organ's name in large capitals,
    /// the sentence about what it does and, quieter still, one extra fact, on a dark glass panel so the words stay readable
    /// over anything. It floats above the organ and toward its far side from the viewer (<see cref="TitlePlacement"/>), so
    /// from where the viewer stands it is above the anatomy and never in front of it, and it is at most 22 cm wide.
    /// It is not a child of the organ, so its size is the same whatever the organ or body scale, and a
    /// <see cref="Billboard"/> turns it to the camera.
    /// Showing runs a <see cref="RevealTimeline"/>: the lines reveal softly one after another (a stagger of
    /// <see cref="MotionSpec.TitleStagger"/>); hiding fades everything while it shrinks to <see cref="MotionSpec.TitleOutScale"/>.
    /// Polling exception: the organ moves while held and the viewer's head moves every frame, and neither has a change
    /// event. The per-frame code runs only while the card is visible, because a hidden card is an inactive object.
    /// </summary>
    public class OrganInfoCard : MonoBehaviour
    {
        private const float Width = 0.22f;
        private const float Padding = 0.009f;
        private const float EyebrowCap = 0.0045f;
        private const float TitleCap = 0.011f;
        private const float FunctionCap = 0.0062f;
        private const float FactCap = 0.0054f;
        private const float EyebrowSpacing = 10f;
        private const float TitleSpacing = 3f;
        private const int LineCount = 4;

        private struct Line
        {
            public TextMeshPro Text;
            public Vector3 Home;
            public float Delay;
        }

        private readonly RevealTimeline _timeline = new RevealTimeline(MotionSpec.TitleIn, MotionSpec.TitleOut);
        private readonly Line[] _lines = new Line[LineCount];
        private MaterialPropertyBlock _block;
        // Created on first use: Unity does not allow it in a field initializer.
        private MaterialPropertyBlock Block => _block ??= new MaterialPropertyBlock();
        private MeshRenderer _panel;
        private Transform _organ;
        private CachedBounds _bounds;
        private float _gap;
        private float _applied = -1f;
        private Camera _camera;

        /// <summary>The organ's name in capitals, as shown.</summary>
        public string Title => _lines[1].Text != null ? _lines[1].Text.text : string.Empty;

        /// <summary>The sentence about what the organ does, as shown.</summary>
        public string Function => _lines[2].Text != null ? _lines[2].Text.text : string.Empty;

        /// <summary>The extra fact, as shown.</summary>
        public string Fact => _lines[3].Text != null ? _lines[3].Text.text : string.Empty;

        /// <summary>True from the moment it is asked to show until it is asked to hide, including the reveal.</summary>
        public bool IsShown => _timeline.Phase == RevealPhase.Entering || _timeline.Phase == RevealPhase.Shown;

        /// <summary>True while anything of it can still be seen, including the fade out.</summary>
        public bool IsVisible => _timeline.IsVisible;

        /// <summary>
        /// Builds a hidden card for the organ. <paramref name="gap"/> is the clearance in metres between the top of the organ
        /// and the bottom of the card. <paramref name="panelMaterial"/> is an unlit transparent material for the dark panel
        /// (null means no panel).
        /// </summary>
        public static OrganInfoCard Create(Transform organ, OrganInfoEntry entry, TMP_FontAsset font, Material panelMaterial, float gap)
        {
            var root = new GameObject($"Info Card {organ.name}");
            var card = root.AddComponent<OrganInfoCard>();
            card._organ = organ;
            card._bounds = new CachedBounds(organ);
            card._gap = gap;
            card.BuildLines(root.transform, entry, font);
            card.BuildPanel(root.transform, panelMaterial);
            root.AddComponent<Billboard>();
            card._timeline.PhaseChanged += card.OnPhaseChanged;
            root.SetActive(false);
            return card;
        }

        public void Show()
        {
            _timeline.Show();
            Follow();
            Apply();
        }

        public void Hide()
        {
            _timeline.Hide();
        }

        // Stacks the four lines from the bottom up and remembers where each one sits.
        private void BuildLines(Transform root, OrganInfoEntry entry, TMP_FontAsset font)
        {
            Vector2 topCentre = new Vector2(0.5f, 1f);
            var box = new Vector2(Width, 0.05f);
            _lines[0].Text = LabelText.Create(root, "Eyebrow", font, EyebrowCap, GuidePalette.TextMuted, FontStyles.Normal,
                TextAlignmentOptions.Top, box, topCentre, true, EyebrowSpacing);
            _lines[0].Text.text = OrganInfoCardText.Eyebrow;
            _lines[1].Text = LabelText.Create(root, "Title", font, TitleCap, GuidePalette.TextPrimary, FontStyles.Bold,
                TextAlignmentOptions.Top, box, topCentre, true, TitleSpacing);
            _lines[1].Text.text = OrganInfoCardText.Title(entry);
            _lines[2].Text = LabelText.Create(root, "Function", font, FunctionCap, GuidePalette.TextPrimary, FontStyles.Normal,
                TextAlignmentOptions.Top, box, topCentre, true);
            _lines[2].Text.text = OrganInfoCardText.Function(entry);
            _lines[3].Text = LabelText.Create(root, "Fact", font, FactCap, GuidePalette.TextMuted, FontStyles.Normal,
                TextAlignmentOptions.Top, box, topCentre, true);
            _lines[3].Text.text = OrganInfoCardText.Fact(entry);

            float[] gapAbove = { 0.004f, 0.006f, 0.008f, 0f };
            float cursor = Padding;
            for (int i = LineCount - 1; i >= 0; i--)
            {
                TextMeshPro text = _lines[i].Text;
                float height = text.text.Length == 0 ? 0f : text.GetPreferredValues(text.text, Width, Mathf.Infinity).y;
                text.rectTransform.sizeDelta = new Vector2(Width, Mathf.Max(height, 0.001f));
                cursor += height;
                _lines[i].Home = new Vector3(0f, cursor, 0f);
                text.transform.localPosition = _lines[i].Home;
                _lines[i].Delay = i * MotionSpec.TitleStagger;
                if (height > 0f)
                    cursor += gapAbove[i];
            }
            _blockHeight = cursor + Padding;
        }

        private float _blockHeight;

        private void BuildPanel(Transform root, Material material)
        {
            if (material == null)
            {
                Debug.LogWarning("The info card has no panel material, so its text sits on the scene. Run Tools > DIYsect > Apply Label Design.", this);
                return;
            }
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Panel";
            Destroy(quad.GetComponent<Collider>());
            quad.transform.SetParent(root, false);
            quad.transform.localPosition = new Vector3(0f, _blockHeight * 0.5f, 0.002f);
            quad.transform.localScale = new Vector3(Width + Padding * 2f, _blockHeight, 1f);
            _panel = quad.GetComponent<MeshRenderer>();
            _panel.sharedMaterial = material;
            _panel.shadowCastingMode = ShadowCastingMode.Off;
            _panel.receiveShadows = false;
        }

        private void OnPhaseChanged(RevealPhase phase)
        {
            bool visible = _timeline.IsVisible;
            if (gameObject.activeSelf != visible)
                gameObject.SetActive(visible);
            if (phase == RevealPhase.Exiting)
                _applied = -1f;
        }

        private void LateUpdate()
        {
            if (_organ == null)
            {
                Destroy(gameObject);
                return;
            }
            _timeline.Tick(Time.deltaTime);
            if (!_timeline.IsVisible)
                return;
            Follow();
            if (!Mathf.Approximately(_applied, _timeline.Amount))
                Apply();
        }

        private void Follow()
        {
            if (_camera == null)
                _camera = Camera.main;
            Bounds bounds = _bounds.Current();
            transform.position = _camera != null
                ? TitlePlacement.Position(bounds, _camera.transform.position, _gap)
                : LabelAnchor.Above(bounds, _gap);
        }

        private void Apply()
        {
            _applied = _timeline.Amount;
            bool entering = _timeline.Phase != RevealPhase.Exiting;
            for (int i = 0; i < LineCount; i++)
            {
                float progress = StaggeredReveal.Progress(_timeline.Amount, MotionSpec.TitleIn, _lines[i].Delay, entering);
                float eased = Ease.OutCubic(progress);
                _lines[i].Text.alpha = eased;
                _lines[i].Text.transform.localPosition = _lines[i].Home + Vector3.down * (MotionSpec.LabelRise * (1f - eased));
            }
            if (_panel != null)
            {
                Color ink = GuidePalette.Ink;
                ink.a *= Ease.OutCubic(_timeline.Amount);
                Block.SetColor("_BaseColor", ink);
                _panel.SetPropertyBlock(Block);
            }
            float scale = _timeline.Phase == RevealPhase.Exiting
                ? Mathf.Lerp(MotionSpec.TitleOutScale, 1f, Ease.OutCubic(_timeline.Amount))
                : 1f;
            transform.localScale = Vector3.one * scale;
        }
    }
}
