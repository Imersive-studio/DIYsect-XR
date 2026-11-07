// Authority: none. Local presentation only; it never touches network state.
using UnityEngine;

namespace DIYsect.Classroom
{
    /// <summary>
    /// Turns a pin label toward the viewer's camera so the number stays readable.
    /// Polling exception: the camera can move every frame (head, orbit) and has no change event.
    /// It only runs while the label's renderer is visible.
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    public class PinLabelBillboard : MonoBehaviour
    {
        private MeshRenderer _renderer;

        private void Awake()
        {
            _renderer = GetComponent<MeshRenderer>();
        }

        private void LateUpdate()
        {
            Camera view = Camera.main;
            if (view == null || !_renderer.isVisible)
                return;
            transform.rotation = Quaternion.LookRotation(transform.position - view.transform.position, view.transform.up);
        }
    }
}
