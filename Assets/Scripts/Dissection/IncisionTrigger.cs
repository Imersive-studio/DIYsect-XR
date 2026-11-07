// Authority: none. A marker that tells a tool which collider is the incision zone.
using UnityEngine;

namespace DIYsect.Dissection
{
    /// <summary>
    /// The trigger volume along the midline. A tool that enters it is touching the skin where the
    /// incision goes, and reports to <see cref="Target"/>. Other body colliders (the organs) carry no
    /// IncisionTrigger, so tools ignore them.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class IncisionTrigger : MonoBehaviour
    {
        [SerializeField] private DissectionProgress _target;

        public ICuttable Target => _target;

        private void Awake()
        {
            Validate();
        }

        private void OnValidate()
        {
            Validate();
        }

        private void Validate()
        {
            if (_target == null)
                Debug.LogError($"{name}: IncisionTrigger has no DissectionProgress. Run Tools > DIYsect > Add Body Dissection.", this);
            if (TryGetComponent(out Collider incisionCollider) && !incisionCollider.isTrigger)
                Debug.LogError($"{name}: the incision collider must be a trigger.", this);
        }
    }
}
