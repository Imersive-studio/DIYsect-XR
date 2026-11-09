// Authority: none. Local scenery; nothing here is sent over the network.
using UnityEngine;

namespace DIYsect.Audience
{
    /// <summary>
    /// The seated classmates in the lecture hall. Each one is a flat photo style cutout, so it keeps turning to face the
    /// student and never shows its edge. A slow breath and an occasional small shift keep the room from looking frozen.
    /// </summary>
    public sealed class SeatedAudience : MonoBehaviour
    {
        [SerializeField] private Transform _student;
        [SerializeField] private Transform[] _figures;
        [SerializeField] private string _layoutKey;

        private Transform[] _cutouts;
        private Vector3[] _restPositions;

        public bool Matches(string layoutKey, Transform student, int figureCount) =>
            _layoutKey == layoutKey && _student == student &&
            _figures != null && _figures.Length == figureCount && transform.childCount == figureCount;

        public void Configure(Transform student, Transform[] figures, string layoutKey)
        {
            _student = student;
            _figures = figures;
            _layoutKey = layoutKey;
        }

        private void Awake()
        {
            if (_student == null || _figures == null || _figures.Length == 0)
            {
                Debug.LogError("Seated audience is missing its scene references. Run Tools > DIYsect > Add Seated Audience.", this);
                enabled = false;
                return;
            }

            _cutouts = new Transform[_figures.Length];
            _restPositions = new Vector3[_figures.Length];
            for (int i = 0; i < _figures.Length; i++)
            {
                _cutouts[i] = _figures[i].GetChild(0);
                _restPositions[i] = _cutouts[i].localPosition;
            }
        }

        private void LateUpdate()
        {
            Vector3 student = _student.position;
            float time = Time.time;
            for (int i = 0; i < _figures.Length; i++)
            {
                Vector3 away = _figures[i].position - student;
                away.y = 0f;
                if (away.sqrMagnitude > 0.01f)
                    _figures[i].rotation = Quaternion.LookRotation(away, Vector3.up);

                float shiftTime = Mathf.Repeat(time + i * 3.71f, AudienceRules.ShiftInterval(i));
                float shift = shiftTime < 1.6f ? Mathf.Sin(shiftTime / 1.6f * Mathf.PI) : 0f;
                _cutouts[i].localPosition = _restPositions[i] +
                    new Vector3(0.02f * shift, 0.006f * Mathf.Sin(time * 1.5f + i * 2.39f), 0f);
            }
        }
    }
}
