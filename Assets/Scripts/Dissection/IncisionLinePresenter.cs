// Authority: none. Presentation only. It draws the incision progress that the server owns in DissectionProgress.
using UnityEngine;

namespace DIYsect.Dissection
{
    /// <summary>
    /// Draws the cut along the midline as the incision advances: a thin dark-red line that follows the skin
    /// surface from the xiphoid (path start) toward the pubis, as far as the synced progress has reached.
    /// The path is measured on the skin by Tools > DIYsect > Add Body Dissection and stored in this object's local
    /// space; the editor step puts it on a child of the body root with an identity pose. Once the skin opens the edges
    /// of the flaps show the cut, so the line hides. Event driven: it redraws only when progress or stage changes.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class IncisionLinePresenter : MonoBehaviour
    {
        [SerializeField] private DissectionProgress _progress;
        [Tooltip("Points on the skin along the midline, xiphoid first, in this object's local space.")]
        [SerializeField] private Vector3[] _path = new Vector3[0];

        private LineRenderer _line;
        private float[] _fractions;   // each path point's progress fraction, as the server measures progress

        private void Awake()
        {
            _line = GetComponent<LineRenderer>();
            _line.useWorldSpace = false;
            if (!Validate())
            {
                enabled = false;
                return;
            }
            _fractions = ProgressFractions(_path, _progress.LineStart, _progress.LineEnd);
        }

        private void OnEnable()
        {
            _progress.ProgressChanged += HandleProgressChanged;
            _progress.StageChanged += HandleStageChanged;
            Redraw();
        }

        private void OnDisable()
        {
            if (_progress == null)
                return;
            _progress.ProgressChanged -= HandleProgressChanged;
            _progress.StageChanged -= HandleStageChanged;
        }

        private void HandleProgressChanged(float progress) => Redraw();

        private void HandleStageChanged(DissectionStage stage, bool initialState) => Redraw();

        private void Redraw()
        {
            float progress = _progress.IsSpawned ? _progress.IncisionProgress : 0f;
            bool visible = progress > 0f && _progress.Stage < DissectionStage.SkinOpen;
            _line.enabled = visible;
            if (!visible)
                return;

            // Every path point already passed, then one point interpolated where the blade is now.
            int passed = 1;
            while (passed < _path.Length && _fractions[passed] <= progress)
                passed++;
            _line.positionCount = passed + (passed < _path.Length ? 1 : 0);
            for (int i = 0; i < passed; i++)
                _line.SetPosition(i, _path[i]);
            if (passed < _path.Length)
            {
                float span = _fractions[passed] - _fractions[passed - 1];
                float t = span > 0f ? (progress - _fractions[passed - 1]) / span : 1f;
                _line.SetPosition(passed, Vector3.Lerp(_path[passed - 1], _path[passed], t));
            }
        }

        // The server measures progress as the projection onto the straight midline segment, so each skin point
        // is projected the same way. The path's local space equals the body root's (identity child).
        private static float[] ProgressFractions(Vector3[] path, Vector3 start, Vector3 end)
        {
            Vector3 line = end - start;
            var fractions = new float[path.Length];
            for (int i = 0; i < path.Length; i++)
                fractions[i] = Mathf.Clamp01(Vector3.Dot(path[i] - start, line) / line.sqrMagnitude);
            return fractions;
        }

        // Checked in Awake only: the editor step adds the component before it assigns the references.
        private bool Validate()
        {
            bool valid = true;
            if (_progress == null)
            {
                Debug.LogError($"{name}: IncisionLinePresenter has no DissectionProgress. Run Tools > DIYsect > Add Body Dissection.", this);
                valid = false;
            }
            if (_path == null || _path.Length < 2)
            {
                Debug.LogError($"{name}: the incision path needs at least two points. Run Tools > DIYsect > Add Body Dissection.", this);
                valid = false;
            }
            return valid;
        }
    }
}
