// Authority: none. Presentation only.
using UnityEngine;

namespace DIYsect.Interaction
{
    /// <summary>Turns its object to face the main camera so a label stays readable. Enable it only while the label is shown.</summary>
    public class Billboard : MonoBehaviour
    {
        private Camera _camera;

        // Needs a per-frame update: the camera (the viewer's head) moves every frame and the label must keep facing it.
        // Cost is limited because GuidingHandIndicator enables this component only while the label is visible.
        private void LateUpdate()
        {
            if (_camera == null)
                _camera = Camera.main;
            if (_camera == null)
                return;
            Vector3 away = transform.position - _camera.transform.position;
            if (away.sqrMagnitude > 1e-6f)
                transform.rotation = Quaternion.LookRotation(away, Vector3.up);
        }
    }
}
