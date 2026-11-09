// Authority: none. Presentation only.
using UnityEngine;

namespace DIYsect.Classroom
{
    /// <summary>
    /// Switches a companion (for example the glass chip behind a message) on and off together with this object. Screen
    /// scripts show and hide a message by switching its text object; this keeps the chip behind it in step, so a hidden
    /// message never leaves an empty chip on the phone. Event driven: it reacts only when this object is switched.
    /// </summary>
    public class ShowWith : MonoBehaviour
    {
        [SerializeField, Tooltip("Switched on when this object is shown and off when it is hidden.")]
        private Behaviour _companion;

        private void OnEnable()
        {
            if (_companion != null)
                _companion.enabled = true;
        }

        private void OnDisable()
        {
            if (_companion != null)
                _companion.enabled = false;
        }
    }
}
