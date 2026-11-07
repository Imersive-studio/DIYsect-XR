// Authority: none. Presentation only. It draws the line that the server owns in GuideLine.
using DIYsect.Networking;
using UnityEngine;

namespace DIYsect.Interaction
{
    /// <summary>
    /// Draws the professor's guide line on this peer. The points are body-local, so this sits on a child of the
    /// body root with an identity pose and the LineRenderer works in local space. Event driven: it redraws only
    /// when the shared line changes, including the line a late joiner receives on spawn.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class GuideLinePresenter : MonoBehaviour
    {
        [SerializeField] private GuideLine _line;

        private LineRenderer _renderer;

        private void Awake()
        {
            _renderer = GetComponent<LineRenderer>();
            _renderer.useWorldSpace = false;
            if (_line == null)
            {
                Debug.LogError($"{name}: GuideLinePresenter has no GuideLine. Run Tools > DIYsect > Add Guide Line.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            _line.LineChanged += Redraw;
            Redraw();
        }

        private void OnDisable()
        {
            if (_line != null)
                _line.LineChanged -= Redraw;
        }

        private void Redraw()
        {
            int count = _line.Count;
            _renderer.enabled = count >= 2;
            _renderer.positionCount = count;
            for (int i = 0; i < count; i++)
                _renderer.SetPosition(i, _line[i]);
        }
    }
}
