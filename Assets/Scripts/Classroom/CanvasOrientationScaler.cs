// Authority: none. Local screen layout only.
using UnityEngine;
using UnityEngine.UI;

namespace DIYsect.Classroom
{
    /// <summary>
    /// On a classmate canvas: sets the CanvasScaler match by orientation (landscape follows the height, portrait the width),
    /// so a 240 unit button stays thumb-sized and on screen on a phone held either way. Re-checks only when the resolution changes.
    /// </summary>
    [RequireComponent(typeof(CanvasScaler))]
    public class CanvasOrientationScaler : MonoBehaviour
    {
        private CanvasScaler _scaler;
        private IScreenMetrics _metrics;
        private Vector2Int _lastResolution;
        private bool _applied;

        private void Awake()
        {
            _scaler = GetComponent<CanvasScaler>();
            _metrics = new UnityScreenMetrics();
        }

        private void OnEnable() => Apply();

        private void Update()
        {
            if (_applied && _lastResolution == _metrics.Resolution)
                return;
            Apply();
        }

        /// <summary>Replaces where the numbers come from and applies at once. Used by tests.</summary>
        public void SetMetrics(IScreenMetrics metrics)
        {
            _metrics = metrics ?? throw new System.ArgumentNullException(nameof(metrics));
            if (_scaler == null)
                _scaler = GetComponent<CanvasScaler>();
            Apply();
        }

        private void Apply()
        {
            _lastResolution = _metrics.Resolution;
            _applied = true;
            _scaler.matchWidthOrHeight = ScreenLayoutMath.MatchFor(new Vector2(_lastResolution.x, _lastResolution.y));
        }
    }
}
