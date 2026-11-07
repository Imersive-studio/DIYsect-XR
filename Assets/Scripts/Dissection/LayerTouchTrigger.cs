// Authority: none. A marker that tells a tool which collider asks for which layer step.
using UnityEngine;

namespace DIYsect.Dissection
{
    /// <summary>
    /// A trigger volume over a body-wall layer. A tool whose tip enters it is touching that layer and
    /// asks <see cref="Target"/> to do <see cref="Action"/>. The server still checks the order.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class LayerTouchTrigger : MonoBehaviour
    {
        [SerializeField] private DissectionProgress _target;
        [SerializeField] private DissectionAction _action;

        public IDissectionSteps Target => _target;
        public DissectionAction Action => _action;

        private void Awake()
        {
            Validate();
        }

        // Checked in Awake only: the editor step adds the component before it assigns the references.
        private void Validate()
        {
            if (_target == null)
                Debug.LogError($"{name}: LayerTouchTrigger has no DissectionProgress. Run Tools > DIYsect > Add Body Dissection.", this);
            if (TryGetComponent(out Collider touchCollider) && !touchCollider.isTrigger)
                Debug.LogError($"{name}: the layer collider must be a trigger.", this);
        }
    }
}
