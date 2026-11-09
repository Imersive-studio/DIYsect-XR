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

        [SerializeField] private ClassmateSketchInput _sketch;
        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private Image _background;
        [SerializeField] private Button _clear;

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

        private void Toggle() => _sketch.SetDrawing(!_sketch.Drawing);

        private void Show(bool drawing)
        {
            _label.text = drawing ? DoneLabel : DrawLabel;
            _background.color = drawing ? GuidePalette.Mint : _offColour;
            _label.color = drawing ? GuidePalette.InkDeep : _offText;
        }
    }
}
