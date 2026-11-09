// Authority: none. Presentation only. It draws the classmate sketch the server holds in a GuideLine, plus this phone's own ink.
using System.Collections.Generic;
using DIYsect.Networking;
using DIYsect.Presentation;
using TMPro;
using UnityEngine;

namespace DIYsect.Interaction
{
    /// <summary>
    /// Draws a classmate's sketch on the body on every device: a smooth violet line (<see cref="SketchLook.Smooth"/>) with a soft
    /// glow around it and a small "CLASSMATE" tag at its end, so the professor can see who drew it. A sketch that is cleared or
    /// replaced fades out instead of vanishing. On the drawing phone, <see cref="ShowPreview"/> shows the ink under the finger at
    /// once, before the room has answered; the shared line takes over when it catches up.
    /// The points are body-local; this sits on a child of the body root with an identity pose.
    /// Event driven, except while a fade or a preview hand-over is running.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class SketchPresenter : MonoBehaviour
    {
        [SerializeField] private GuideLine _line;
        [SerializeField, Tooltip("A second, wider line drawn behind the core as a soft glow.")]
        private LineRenderer _glow;
        [SerializeField, Tooltip("Font for the chip at the end of the sketch (Inter).")]
        private TMP_FontAsset _font;
        [SerializeField] private string _chipText = "CLASSMATE";
        [SerializeField, Min(0f), Tooltip("Seconds this phone's ink waits for the shared line before it gives up.")]
        private float _previewTimeout = 1.5f;

        private const float ChipCapHeight = 0.009f;
        private const float ChipLift = 0.02f;

        private readonly List<Vector3> _shared = new List<Vector3>();
        private readonly List<Vector3> _preview = new List<Vector3>();
        private readonly List<Vector3> _drawn = new List<Vector3>();
        private readonly List<Vector3> _smooth = new List<Vector3>();
        private LineRenderer _core;
        private TextMeshPro _chip;
        private Color _coreColour;
        private Color _glowColour;
        private bool _previewing;
        private float _previewEndsAt = -1f;
        private float _fadeStartedAt = -1f;
        private bool _showing;
        private bool _valid;

        /// <summary>
        /// True on the drawing phone: the person who drew the sketch does not need their own "Classmate" tag on it, so one
        /// stroke never carries two texts there. Everyone else still sees the tag once, at the end of the line.
        /// </summary>
        public static bool ChipHiddenOnThisDevice { get; set; }

        /// <summary>True while a line (shared or this phone's own) is on screen or fading.</summary>
        public bool IsShowing => _showing || _fadeStartedAt >= 0f;

        /// <summary>How many curve points are drawn now. Used by tests.</summary>
        public int DrawnPoints => _core != null && _core.enabled ? _core.positionCount : 0;

        private void Awake()
        {
            _core = GetComponent<LineRenderer>();
            _core.useWorldSpace = false;
            _valid = _line != null && _glow != null;
            if (!_valid)
            {
                Debug.LogError($"{name}: SketchPresenter has no line or glow. Run Tools > DIYsect > Add Classmate Sketch.", this);
                enabled = false;
                return;
            }
            _glow.useWorldSpace = false;
            _coreColour = _core.startColor;
            _glowColour = _glow.startColor;
            TMP_FontAsset font = LabelText.Resolve(_font, this);
            if (font != null)
            {
                // A quiet eyebrow in the label style: small white spaced capitals. The ink carries the colour, not the text.
                _chip = LabelText.Create(transform, "SketchChip", font, ChipCapHeight, GuidePalette.TextPrimary, FontStyles.Normal,
                    TextAlignmentOptions.Bottom, new Vector2(0.3f, 0.05f), new Vector2(0.5f, 0f), false, 14f, onTop: true);
                _chip.text = _chipText;
                _chip.gameObject.AddComponent<Billboard>();
                _chip.gameObject.SetActive(false);
            }
            Hide();
        }

        private void OnEnable()
        {
            if (!_valid)
                return;
            _line.LineChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (_line != null)
                _line.LineChanged -= Refresh;
        }

        /// <summary>This phone's ink so far (body-local points), shown at once while the room has not echoed it yet.</summary>
        public void ShowPreview(IReadOnlyList<Vector3> localPoints)
        {
            if (!_valid)
                return;
            _preview.Clear();
            for (int i = 0; i < localPoints.Count; i++)
                _preview.Add(localPoints[i]);
            _previewing = true;
            _previewEndsAt = -1f;
            Refresh();
        }

        /// <summary>The finger lifted: keep the ink until the shared line has caught up, or the room has not answered in time.</summary>
        public void EndPreview()
        {
            if (!_previewing)
                return;
            _previewEndsAt = Time.unscaledTime + _previewTimeout;
            Refresh();
        }

        private void Refresh()
        {
            _shared.Clear();
            for (int i = 0; i < _line.Count; i++)
                _shared.Add(_line[i]);
            if (_previewing && _previewEndsAt >= 0f && SketchLook.CaughtUp(_shared.Count, _preview.Count))
                _previewing = false;
            // While this phone draws, its own ink wins: the room clears the old sketch a moment later.
            List<Vector3> source = _previewing && _preview.Count >= 2 ? _preview : _shared;
            if (source.Count >= 2)
            {
                _drawn.Clear();
                _drawn.AddRange(source);
                Draw(1f);
                _fadeStartedAt = -1f;
                _showing = true;
            }
            else if (_showing)
            {
                // Cleared or replaced: fade what was drawn instead of making it vanish.
                _showing = false;
                _fadeStartedAt = Time.unscaledTime;
            }
        }

        // Polling exception: a fade and the preview hand-over need a clock. Both stop on their own.
        private void LateUpdate()
        {
            if (_previewing && _previewEndsAt >= 0f && Time.unscaledTime >= _previewEndsAt)
            {
                _previewing = false;
                Refresh();
            }
            if (_fadeStartedAt < 0f)
                return;
            float alpha = SketchLook.FadeAlpha(Time.unscaledTime - _fadeStartedAt);
            if (alpha <= 0f)
            {
                _fadeStartedAt = -1f;
                Hide();
                return;
            }
            Draw(alpha);
        }

        private void Draw(float alpha)
        {
            SketchLook.Smooth(_drawn, SketchLook.PointsPerSegment, _smooth);
            Apply(_core, _coreColour, alpha);
            Apply(_glow, _glowColour, alpha);
            if (_chip != null && ChipHiddenOnThisDevice)
                _chip.gameObject.SetActive(false);
            else if (_chip != null)
            {
                _chip.gameObject.SetActive(true);
                _chip.transform.localPosition = _drawn[_drawn.Count - 1] + transform.InverseTransformDirection(Vector3.up) * ChipLift;
                Color chipColour = _chip.color;
                chipColour.a = alpha;
                _chip.color = chipColour;
            }
        }

        private void Apply(LineRenderer renderer, Color colour, float alpha)
        {
            renderer.enabled = true;
            renderer.positionCount = _smooth.Count;
            for (int i = 0; i < _smooth.Count; i++)
                renderer.SetPosition(i, _smooth[i]);
            colour.a *= alpha;
            renderer.startColor = renderer.endColor = colour;
        }

        private void Hide()
        {
            _core.enabled = false;
            _core.positionCount = 0;
            if (_glow != null)
            {
                _glow.enabled = false;
                _glow.positionCount = 0;
            }
            if (_chip != null)
                _chip.gameObject.SetActive(false);
        }
    }
}
