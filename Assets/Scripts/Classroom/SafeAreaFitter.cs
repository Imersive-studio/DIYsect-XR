// Authority: none. Local screen layout only.
using UnityEngine;

namespace DIYsect.Classroom
{
    /// <summary>
    /// On a full-screen panel inside a classmate canvas: keeps the panel inside Screen.safeArea, so a notch, rounded corner
    /// or gesture bar never covers a button. It looks at the safe area, resolution and orientation each frame, which is three
    /// comparisons, and only touches the layout when one of them changed (a rotation, a window resize, a cutout change).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform _rect;
        private IScreenMetrics _metrics;
        private Rect _lastSafeArea;
        private Vector2Int _lastResolution;
        private ScreenOrientation _lastOrientation;
        private bool _fitted;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            _metrics = new UnityScreenMetrics();
        }

        private void OnEnable() => Fit();

        private void Update()
        {
            if (_fitted && _lastSafeArea == _metrics.SafeArea && _lastResolution == _metrics.Resolution && _lastOrientation == _metrics.Orientation)
                return;
            Fit();
        }

        /// <summary>Replaces where the numbers come from and fits at once. Used by tests.</summary>
        public void SetMetrics(IScreenMetrics metrics)
        {
            _metrics = metrics ?? throw new System.ArgumentNullException(nameof(metrics));
            if (_rect == null)
                _rect = GetComponent<RectTransform>();
            Fit();
        }

        private void Fit()
        {
            _lastSafeArea = _metrics.SafeArea;
            _lastResolution = _metrics.Resolution;
            _lastOrientation = _metrics.Orientation;
            _fitted = true;
            ScreenLayoutMath.AnchorsFor(_lastSafeArea, new Vector2(_lastResolution.x, _lastResolution.y), out Vector2 min, out Vector2 max);
            _rect.anchorMin = min;
            _rect.anchorMax = max;
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
        }
    }
}
