// Authority: none. Presentation only. It follows the stage that the server owns in DissectionProgress.
using System.Collections;
using UnityEngine;

namespace DIYsect.Dissection
{
    /// <summary>
    /// Sits on a pivot placed at the outer edge of one skin flap (the flap is its child). When the stage
    /// reaches SkinOpen the pivot swings about the hinge axis to the open angle; below that it stays closed.
    /// A peer that joins after the skin was opened gets the open pose at once. Nothing here is networked:
    /// every peer derives the same pose from the same synced stage.
    /// </summary>
    public class SkinFlapHinge : MonoBehaviour
    {
        [SerializeField] private DissectionProgress _progress;
        [Tooltip("Hinge axis in this pivot's local space (along the body's long axis).")]
        [SerializeField] private Vector3 _hingeAxis = Vector3.up;
        [Tooltip("Signed swing in degrees. The sign picks the direction so the flap opens outward.")]
        [SerializeField, Range(-180f, 180f)] private float _openAngleDegrees = 150f;
        [SerializeField, Min(0.05f)] private float _openSeconds = 1.5f;

        private Quaternion _closedRotation;
        private Coroutine _swing;

        public bool IsOpen => _progress != null && _progress.Stage >= DissectionStage.SkinOpen;
        public Quaternion OpenRotation => _closedRotation * Quaternion.AngleAxis(_openAngleDegrees, _hingeAxis.normalized);

        private void Awake()
        {
            _closedRotation = transform.localRotation;
            Validate();
        }

        private void OnValidate()
        {
            Validate();
        }

        private void OnEnable()
        {
            _progress.StageChanged += HandleStageChanged;
            if (_progress.IsSpawned)
                SetPose(IsOpen, animate: false);
        }

        private void OnDisable()
        {
            _progress.StageChanged -= HandleStageChanged;
            StopSwing();
        }

        private void HandleStageChanged(DissectionStage stage, bool initialState)
        {
            SetPose(stage >= DissectionStage.SkinOpen, animate: !initialState);
        }

        private void SetPose(bool open, bool animate)
        {
            StopSwing();
            Quaternion target = open ? OpenRotation : _closedRotation;
            if (animate && open)
                _swing = StartCoroutine(Swing(target));
            else
                transform.localRotation = target;
        }

        private IEnumerator Swing(Quaternion target)
        {
            Quaternion start = transform.localRotation;
            for (float elapsed = 0f; elapsed < _openSeconds; elapsed += Time.deltaTime)
            {
                transform.localRotation = Quaternion.Slerp(start, target, Mathf.SmoothStep(0f, 1f, elapsed / _openSeconds));
                yield return null;
            }
            transform.localRotation = target;
            _swing = null;
        }

        private void StopSwing()
        {
            if (_swing == null)
                return;
            StopCoroutine(_swing);
            _swing = null;
        }

        private void Validate()
        {
            if (_progress == null)
                Debug.LogError($"{name}: SkinFlapHinge has no DissectionProgress. Run Tools > DIYsect > Add Body Dissection.", this);
            if (_hingeAxis == Vector3.zero)
                Debug.LogError($"{name}: the hinge axis must not be zero.", this);
        }
    }
}
