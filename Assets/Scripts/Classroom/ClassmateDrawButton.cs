// Authority: none. Local UI only; the sketch itself goes through the server-held line.
using DIYsect.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DIYsect.Classroom
{
    /// <summary>
    /// The phone's Draw button in the dock. A tap turns drawing on (the button turns mint and says "Done"), so a finger drawn
    /// across the body sketches on it for everyone; another tap turns it off and the finger orbits the camera again. The Clear
    /// button next to it wipes the sketch. Event driven: it changes only when drawing is switched.
    /// </summary>
    public class ClassmateDrawButton : MonoBehaviour
    {
        public const string DrawLabel = "Draw";
        public const string DoneLabel = "Done";
        public const string HintText = "Draw on the body. The whole class sees it.";
        private const float HintSeconds = 3.5f;

        [SerializeField] private ClassmateSketchInput _sketch;
        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private Image _background;
        [SerializeField] private Button _clear;
        [SerializeField, Tooltip("Optional: a short note shown for a few seconds when drawing turns on.")]
        private TMP_Text _hint;

        private Vector2 _hintRest;
        private Color _offColour;
        private Color _offText;
        private bool _valid;

        private void Awake()
        {
            _valid = _sketch != null && _button != null && _label != null && _background != null && _clear != null;
            if (!_valid)
            {
                Debug.LogError($"{name}: ClassmateDrawButton is missing a reference. Run Tools > DIYsect > Rebuild Phone UI.", this);
                enabled = false;
                return;
            }
            _offColour = _background.color;
            _offText = _label.color;
            if (_hint != null && _hint.transform.parent is RectTransform chip)
                _hintRest = chip.anchoredPosition;
        }

        private void OnEnable()
        {
            if (!_valid)
                return;
            _button.onClick.AddListener(Toggle);
            _clear.onClick.AddListener(_sketch.Clear);
            _sketch.DrawingChanged += Show;
            Show(_sketch.Drawing);
        }

        private void OnDisable()
        {
            if (!_valid)
                return;
            _button.onClick.RemoveListener(Toggle);
            _clear.onClick.RemoveListener(_sketch.Clear);
            _sketch.DrawingChanged -= Show;
            // Leaving the room or the screen never leaves the finger stuck in drawing.
            if (_sketch.Drawing)
                _sketch.SetDrawing(false);
        }

        // The hint and its chip fade and rise in, rest, then fade out; the chip's CanvasGroup carries the alpha for both.
        private System.Collections.IEnumerator ShowHint()
        {
            _hint.text = HintText;
            _hint.gameObject.SetActive(true);
            var group = _hint.transform.parent != null ? _hint.transform.parent.GetComponent<CanvasGroup>() : null;
            var chip = _hint.transform.parent as RectTransform;
            Vector2 rest = _hintRest;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / MotionSpec.InstructionIn)
            {
                Fade(group, chip, rest, Ease.OutCubic(t));
                yield return null;
            }
            Fade(group, chip, rest, 1f);
            yield return new WaitForSecondsRealtime(HintSeconds);
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / MotionSpec.InstructionOut)
            {
                Fade(group, chip, rest, 1f - Ease.InCubic(t));
                yield return null;
            }
            Fade(group, chip, rest, 1f);
            _hint.gameObject.SetActive(false);
        }

        private const float HintRise = 10f;

        private static void Fade(CanvasGroup group, RectTransform chip, Vector2 rest, float amount)
        {
            if (group != null)
                group.alpha = amount;
            if (chip != null)
                chip.anchoredPosition = rest + Vector2.down * (HintRise * (1f - amount));
        }

        private void Toggle() => _sketch.SetDrawing(!_sketch.Drawing);

        private void Show(bool drawing)
        {
            StopAllCoroutines();
            if (_hint != null)
            {
                if (drawing)
                    StartCoroutine(ShowHint());
                else
                {
                    Fade(_hint.transform.parent != null ? _hint.transform.parent.GetComponent<CanvasGroup>() : null, _hint.transform.parent as RectTransform, _hintRest, 1f);
                    _hint.gameObject.SetActive(false);
                }
            }
            _label.text = drawing ? DoneLabel : DrawLabel;
            // Ease into mint and back instead of jumping, so the change of mode reads as one calm motion.
            if (isActiveAndEnabled)
                StartCoroutine(Tint(drawing ? GuidePalette.Mint : _offColour, drawing ? GuidePalette.InkDeep : _offText));
        }

        private System.Collections.IEnumerator Tint(Color fill, Color text)
        {
            Color fromFill = _background.color;
            Color fromText = _label.color;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / MotionSpec.LabelIn)
            {
                float k = Ease.OutCubic(t);
                _background.color = Color.Lerp(fromFill, fill, k);
                _label.color = Color.Lerp(fromText, text, k);
                yield return null;
            }
            _background.color = fill;
            _label.color = text;
        }
    }
}
