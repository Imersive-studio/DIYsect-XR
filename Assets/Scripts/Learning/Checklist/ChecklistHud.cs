// Authority: none. Presentation only; it draws what ChecklistTracker reports.
using UnityEngine;
using UnityEngine.XR;
using DIYsect.Presentation;

namespace DIYsect.Learning
{
    /// <summary>
    /// The checklist for classmates on a laptop or phone: a small rounded ink pill in the top left corner with a mint
    /// "STEP n OF 7" chip, the name of the next step and seven progress dots (mint done, coral now, slate later).
    /// It matches the look of the headset panel and draws nothing inside a headset, where <see cref="ChecklistPanel"/>
    /// is used. Immediate-mode GUI keeps it free of scene setup, like the pointer HUD. The textures are painted once
    /// per screen size and the strings are rebuilt only when the step changes, so a frame allocates nothing.
    /// </summary>
    [RequireComponent(typeof(ChecklistTracker))]
    public class ChecklistHud : MonoBehaviour
    {
        /// <summary>
        /// Set while a full-screen page (the phone's Join page) covers the scene, so nothing is drawn on top of it. Nothing to do
        /// with the checklist's own state.
        /// </summary>
        public static bool Suppressed { get; set; }

        // Sizes below are in pixels at scale 1 (a 540 pixel short side) and are multiplied by the screen scale.
        private const float PillHeight = 40f;
        private const float ChipHeight = 22f;
        private const float EdgeMargin = 12f;
        private const float PillPadding = 14f;
        private const float ChipPadding = 10f;
        private const float Gap = 10f;
        private const float DotSize = 9f;
        private const float DotGap = 5f;
        private const float PulsePeriod = 1.6f;

        private ChecklistTracker _tracker;

        private Texture2D _pillTexture;
        private Texture2D _chipTexture;
        private int _textureHeight;
        private GUIStyle _pillStyle;
        private GUIStyle _chipStyle;
        private GUIStyle _chipTextStyle;
        private GUIStyle _headlineStyle;
        private GUIStyle _countStyle;
        private readonly GUIContent _content = new GUIContent();

        private bool _hasLayout;
        private ChecklistState _layoutState;
        private float _layoutScale;
        private string _chipText;
        private string _headlineText;
        private string _countText;
        private float _chipWidth;
        private float _headlineWidth;
        private float _countWidth;

        private void Awake()
        {
            _tracker = GetComponent<ChecklistTracker>();
        }

        private void OnDestroy()
        {
            DestroyTexture(ref _pillTexture);
            DestroyTexture(ref _chipTexture);
        }

        private void OnGUI()
        {
            if (Event.current.type != EventType.Repaint || XRSettings.isDeviceActive || Suppressed)
                return;

            // Snap the scale to quarter steps so dragging a window edge does not repaint the textures every frame.
            float scale = Mathf.Clamp(Mathf.Min(Screen.width, Screen.height) / 540f, 1f, 3f);
            scale = Mathf.Round(scale * 4f) / 4f;

            ChecklistState state = _tracker.State;
            EnsureStyles(scale);
            if (!_hasLayout || state != _layoutState || !Mathf.Approximately(scale, _layoutScale))
                RefreshLayout(state, scale);

            Draw(state, scale);
        }

        private void EnsureStyles(float scale)
        {
            int pillPixels = Mathf.RoundToInt(PillHeight * scale);
            if (_pillStyle != null && _textureHeight == pillPixels)
                return;

            _textureHeight = pillPixels;
            DestroyTexture(ref _pillTexture);
            DestroyTexture(ref _chipTexture);
            _pillTexture = BuildRoundTexture(pillPixels);
            _chipTexture = BuildRoundTexture(Mathf.RoundToInt(ChipHeight * scale));

            _pillStyle = BuildBackgroundStyle(_pillTexture);
            _chipStyle = BuildBackgroundStyle(_chipTexture);

            _chipTextStyle = BuildTextStyle(FontStyle.Bold, TextAnchor.MiddleCenter);
            _chipTextStyle.normal.textColor = GuidePalette.Ink;
            _headlineStyle = BuildTextStyle(FontStyle.Bold, TextAnchor.MiddleLeft);
            _headlineStyle.normal.textColor = GuidePalette.TextPrimary;
            _countStyle = BuildTextStyle(FontStyle.Bold, TextAnchor.MiddleRight);
            _countStyle.normal.textColor = GuidePalette.TextMuted;

            // New styles have no font size yet, so the next layout pass must run.
            _hasLayout = false;
        }

        private void RefreshLayout(ChecklistState state, float scale)
        {
            _layoutState = state;
            _layoutScale = scale;
            _hasLayout = true;

            _chipTextStyle.fontSize = Mathf.RoundToInt(11f * scale);
            _headlineStyle.fontSize = Mathf.RoundToInt(15f * scale);
            _countStyle.fontSize = Mathf.RoundToInt(12f * scale);

            _chipText = ChecklistLabels.ProgressChip(state);
            _headlineText = ChecklistLabels.Headline(state);
            _countText = ChecklistLabels.Count(state);

            _content.text = _chipText;
            _chipWidth = _chipTextStyle.CalcSize(_content).x + ChipPadding * 2f * scale;
            _content.text = _headlineText;
            _headlineWidth = _headlineStyle.CalcSize(_content).x;
            _content.text = _countText;
            _countWidth = _countStyle.CalcSize(_content).x;
        }

        private void Draw(ChecklistState state, float scale)
        {
            float height = PillHeight * scale;
            float gap = Gap * scale;
            float padding = PillPadding * scale;
            float dotSize = DotSize * scale;
            float dotGap = DotGap * scale;
            float dotsWidth = ChecklistLabels.StepCount * dotSize + (ChecklistLabels.StepCount - 1) * dotGap;

            // Screen.safeArea starts at the bottom left; the GUI starts at the top left.
            Rect safe = Screen.safeArea;
            float left = safe.xMin + EdgeMargin * scale;
            // The connection status line sits in the same top left corner (ClassmateStatusLabel), so the pill goes just below it.
            float top = Screen.height - safe.yMax + 0.075f * Mathf.Min(Screen.width, Screen.height);
            float available = safe.width - EdgeMargin * 2f * scale;

            // On a narrow phone the headline gives way first; it is clipped rather than wrapped.
            float headlineWidth = _headlineWidth;
            float total = padding * 2f + _chipWidth + gap + headlineWidth + gap + dotsWidth + gap + _countWidth;
            if (total > available)
            {
                headlineWidth = Mathf.Max(0f, headlineWidth - (total - available));
                total = available;
            }

            Color previous = GUI.color;

            GUI.color = GuidePalette.Ink;
            _pillStyle.Draw(new Rect(left, top, total, height), false, false, false, false);

            float x = left + padding;
            float chipHeight = ChipHeight * scale;
            var chipRect = new Rect(x, top + (height - chipHeight) * 0.5f, _chipWidth, chipHeight);
            GUI.color = GuidePalette.Mint;
            _chipStyle.Draw(chipRect, false, false, false, false);
            GUI.color = previous;
            GUI.Label(chipRect, _chipText, _chipTextStyle);
            x += _chipWidth + gap;

            GUI.Label(new Rect(x, top, headlineWidth, height), _headlineText, _headlineStyle);
            x += headlineWidth + gap;

            int done = ChecklistLabels.DoneCount(state);
            float dotY = top + (height - dotSize) * 0.5f;
            for (int i = 0; i < ChecklistLabels.StepCount; i++)
            {
                if (i < done)
                    GUI.color = GuidePalette.Mint;
                else if (i == done)
                    GUI.color = GuidePalette.Pulse(GuidePalette.Coral, GuidePalette.CoralSoft, Time.unscaledTime, PulsePeriod);
                else
                    GUI.color = GuidePalette.Slate;
                GUI.DrawTexture(new Rect(x + i * (dotSize + dotGap), dotY, dotSize, dotSize), _pillTexture);
            }
            GUI.color = previous;
            x += dotsWidth + gap;

            GUI.Label(new Rect(x, top, _countWidth, height), _countText, _countStyle);
        }

        // A white texture of size by size pixels with fully round ends, tinted at draw time through GUI.color.
        // Stretched by a nine-slice style it makes a pill; drawn square it makes a dot.
        private static Texture2D BuildRoundTexture(int size)
        {
            size = Mathf.Max(8, size);
            var pixels = new Color32[size * size];
            float radius = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - radius;
                    float dy = y + 0.5f - radius;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(radius - distance + 0.5f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private static GUIStyle BuildBackgroundStyle(Texture2D texture)
        {
            int edge = texture.height / 2;
            var style = new GUIStyle
            {
                border = new RectOffset(edge, edge, edge, edge),
            };
            style.normal.background = texture;
            return style;
        }

        private static GUIStyle BuildTextStyle(FontStyle fontStyle, TextAnchor alignment)
        {
            return new GUIStyle
            {
                fontStyle = fontStyle,
                alignment = alignment,
                wordWrap = false,
                clipping = TextClipping.Clip,
            };
        }

        private static void DestroyTexture(ref Texture2D texture)
        {
            if (texture != null)
                Destroy(texture);
            texture = null;
        }
    }
}
