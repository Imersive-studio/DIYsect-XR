// Authority: none. Presentation only.
using DIYsect.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace DIYsect.Interaction
{
    /// <summary>
    /// The scientific callout for the organ in the hand: a small ring and dot on the organ's top, a thin leader line going up
    /// and out at an angle and then a short level run, and at its end the organ's name with its role under it.
    /// On show the line draws from the anchor to the text and then the text reveals; on hide the text dissolves while the line
    /// retracts toward the anchor. Two <see cref="RevealTimeline"/>s drive it, one for the line and one for the text.
    /// It is not a child of the organ. It is placed at the organ's current top every frame while visible and its
    /// <see cref="Billboard"/> keeps its right side toward the viewer's right, so the line always reads the same way.
    /// The per-frame code runs only while visible, because the object is inactive otherwise.
    /// </summary>
    public class OrganCallout : MonoBehaviour
    {
        private const float NameCap = 0.011f;
        private const float RoleCap = 0.0075f;
        private const float LineWidth = 0.0015f;
        private const float RingRadius = 0.0045f;
        private const float DotWidth = 0.003f;
        private const float AnchorLift = 0.003f;
        private const float RoleDelaySeconds = 0.04f;
        private const int RingSegments = 24;
        private static readonly Vector3 Elbow = new Vector3(0.045f, 0.045f, 0f);
        private static readonly Vector3 RunEnd = new Vector3(0.075f, 0.045f, 0f);
        private static readonly Vector3 NameHome = new Vector3(0.083f, 0.0535f, 0f);
        private static readonly Vector3 RoleHome = new Vector3(0.083f, 0.0365f, 0f);
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private readonly RevealTimeline _leader = new RevealTimeline(MotionSpec.LeaderIn, MotionSpec.LeaderOut);
        private readonly RevealTimeline _text = new RevealTimeline(MotionSpec.LabelIn, MotionSpec.LabelOut);
        private readonly Vector3[] _path = new Vector3[3];
        private readonly Vector3[] _trimmed = new Vector3[3];
        private MaterialPropertyBlock _block;
        // Created on first use: Unity does not allow it in a field initializer.
        private MaterialPropertyBlock Block => _block ??= new MaterialPropertyBlock();
        private LineRenderer _line;
        private LineRenderer _ring;
        private LineRenderer _dot;
        private TextMeshPro _name;
        private TextMeshPro _role;
        private Transform _organ;
        private CachedBounds _bounds;
        private Color _lineColor;
        private float _appliedLeader = -1f;
        private float _appliedText = -1f;

        /// <summary>The organ's name, as written at the end of the line.</summary>
        public string Text => _name != null ? _name.text : string.Empty;

        /// <summary>The second line (the organ's role), empty when there is none.</summary>
        public string Role => _role != null ? _role.text : string.Empty;

        /// <summary>True from the moment it is asked to show until it is asked to hide.</summary>
        public bool IsShown => _leader.Phase == RevealPhase.Entering || _leader.Phase == RevealPhase.Shown;

        /// <summary>True while anything of it can still be seen.</summary>
        public bool IsVisible => _leader.IsVisible || _text.IsVisible;

        /// <summary>Builds a hidden callout for the organ. <paramref name="lineMaterial"/> is an unlit transparent material.</summary>
        public static OrganCallout Create(Transform organ, OrganCaption caption, TMP_FontAsset font, Material lineMaterial)
        {
            var root = new GameObject($"Callout {caption.Name}");
            var callout = root.AddComponent<OrganCallout>();
            callout._organ = organ;
            callout._bounds = new CachedBounds(organ);
            callout._lineColor = Color.Lerp(GuidePalette.MintLine, GuidePalette.Mint, 0.6f);

            Vector3 outward = new Vector3(1f, 1f, 0f).normalized;
            callout._path[0] = outward * RingRadius;
            callout._path[1] = Elbow;
            callout._path[2] = RunEnd;

            callout._line = MakeLine(root.transform, "Leader", lineMaterial, LineWidth, 2, false);
            callout._ring = MakeLine(root.transform, "Ring", lineMaterial, LineWidth, RingSegments, true);
            for (int i = 0; i < RingSegments; i++)
            {
                float angle = i / (float)RingSegments * Mathf.PI * 2f;
                callout._ring.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * RingRadius);
            }
            // A line of almost no length with round caps is a small filled dot.
            callout._dot = MakeLine(root.transform, "Dot", lineMaterial, DotWidth, 2, false);
            callout._dot.numCapVertices = 8;
            callout._dot.SetPosition(0, Vector3.zero);
            callout._dot.SetPosition(1, new Vector3(0.0001f, 0f, 0f));

            Vector2 left = new Vector2(0f, 0.5f);
            callout._name = LabelText.Create(root.transform, "Name", font, NameCap, GuidePalette.TextPrimary, FontStyles.Bold,
                TextAlignmentOptions.Left, new Vector2(0.3f, 0.02f), left, false, onTop: true);
            callout._name.text = caption.Name;
            callout._name.transform.localPosition = NameHome;
            callout._role = LabelText.Create(root.transform, "Role", font, RoleCap, GuidePalette.TextMuted, FontStyles.Normal,
                TextAlignmentOptions.Left, new Vector2(0.3f, 0.014f), left, false, onTop: true);
            callout._role.text = caption.Role;
            callout._role.transform.localPosition = RoleHome;
            callout._role.gameObject.SetActive(caption.Role.Length > 0);

            root.AddComponent<Billboard>();
            callout._leader.PhaseChanged += callout.OnLeaderPhaseChanged;
            callout._text.PhaseChanged += callout.OnTextPhaseChanged;
            root.SetActive(false);
            return callout;
        }

        public void Show()
        {
            _leader.Show();
            Place();
            Apply();
        }

        public void Hide()
        {
            _text.Hide();
            _leader.Hide();
        }

        private static LineRenderer MakeLine(Transform parent, string objectName, Material material, float width, int points, bool loop)
        {
            var child = new GameObject(objectName);
            child.transform.SetParent(parent, false);
            var line = child.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = false;
            line.alignment = LineAlignment.View;
            line.loop = loop;
            line.positionCount = points;
            line.widthMultiplier = width;
            line.numCornerVertices = 2;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        // The text starts to reveal only when the line has finished drawing.
        private void OnLeaderPhaseChanged(RevealPhase phase)
        {
            if (phase == RevealPhase.Shown)
                _text.Show();
            RefreshActive();
        }

        private void OnTextPhaseChanged(RevealPhase phase)
        {
            RefreshActive();
        }

        private void RefreshActive()
        {
            bool visible = IsVisible;
            if (gameObject.activeSelf != visible)
                gameObject.SetActive(visible);
        }

        // Polling exception: the organ moves while held and the viewer's head moves every frame; neither has a change event.
        private void LateUpdate()
        {
            if (_organ == null)
            {
                Destroy(gameObject);
                return;
            }
            _leader.Tick(Time.deltaTime);
            _text.Tick(Time.deltaTime);
            if (!IsVisible)
                return;
            Place();
            if (!Mathf.Approximately(_appliedLeader, _leader.Amount) || !Mathf.Approximately(_appliedText, _text.Amount))
                Apply();
        }

        private void Place()
        {
            transform.position = LabelAnchor.Above(_bounds.Current(), AnchorLift);
        }

        private void Apply()
        {
            _appliedLeader = _leader.Amount;
            _appliedText = _text.Amount;

            float drawn = Ease.OutCubic(_leader.Amount);
            int count = LeaderPath.Trim(_path, drawn, _trimmed);
            _line.enabled = count >= 2;
            if (count >= 2)
            {
                _line.positionCount = count;
                for (int i = 0; i < count; i++)
                    _line.SetPosition(i, _trimmed[i]);
            }
            float anchorAlpha = Mathf.Clamp01(drawn * 4f);
            Tint(_line, drawn > 0f ? 1f : 0f);
            Tint(_ring, anchorAlpha);
            Tint(_dot, anchorAlpha);
            _ring.enabled = anchorAlpha > 0f;
            _dot.enabled = anchorAlpha > 0f;

            bool entering = _text.Phase != RevealPhase.Exiting;
            SetLine(_name, NameHome, StaggeredReveal.Progress(_text.Amount, MotionSpec.LabelIn, 0f, entering));
            SetLine(_role, RoleHome, StaggeredReveal.Progress(_text.Amount, MotionSpec.LabelIn, RoleDelaySeconds, entering));
        }

        private void Tint(LineRenderer line, float alpha)
        {
            Color color = _lineColor;
            color.a *= alpha;
            Block.SetColor(BaseColorId, color);
            line.SetPropertyBlock(Block);
        }

        private static void SetLine(TextMeshPro line, Vector3 home, float progress)
        {
            float eased = Ease.OutCubic(progress);
            line.alpha = eased;
            line.transform.localPosition = home + Vector3.down * (MotionSpec.LabelRise * (1f - eased));
        }
    }
}
