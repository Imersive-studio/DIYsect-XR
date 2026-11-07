// Authority: the server. It alone writes the incision progress and the stage. Clients only send contact
// samples and an open request by Rpc; the server validates each one before anything changes.
using System;
using Unity.Netcode;
using UnityEngine;

namespace DIYsect.Dissection
{
    /// <summary>
    /// Server-authoritative state of the midline incision on the shared body.
    /// The midline is a segment from <c>_lineStart</c> (xiphoid) to <c>_lineEnd</c> (pubis) in the body
    /// root's local space. A scalpel sample counts when it is within <c>_toleranceMeters</c> of the segment
    /// and ahead of the current progress by no more than <c>_maxStepFraction</c> of its length.
    /// Reaching the end sets <see cref="DissectionStage.Incised"/>. Opening the skin is a separate request
    /// (<see cref="RequestOpen"/>) that the server accepts only in the Incised stage. Both values are read
    /// by every peer, so late joiners get the current state when the body spawns.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class DissectionProgress : NetworkBehaviour, ICuttable
    {
        [SerializeField] private Vector3 _lineStart;
        [SerializeField] private Vector3 _lineEnd;
        [SerializeField, Min(0.001f)] private float _toleranceMeters = 0.04f;
        [SerializeField, Range(0.01f, 1f)] private float _maxStepFraction = 0.2f;
        [SerializeField, Range(0.5f, 1f)] private float _completeAtFraction = 0.98f;

        private readonly NetworkVariable<float> _progress = new NetworkVariable<float>(0f);
        private readonly NetworkVariable<DissectionStage> _stage = new NetworkVariable<DissectionStage>(DissectionStage.Intact);

        public float IncisionProgress => _progress.Value;
        public DissectionStage Stage => _stage.Value;
        public Vector3 LineStart => _lineStart;
        public Vector3 LineEnd => _lineEnd;

        public event Action<DissectionStage, bool> StageChanged;

        /// <summary>Raised on every peer when the progress changes.</summary>
        public event Action<float> ProgressChanged;

        private void Awake()
        {
            Validate();
        }

        private void OnValidate()
        {
            Validate();
        }

        public override void OnNetworkSpawn()
        {
            _stage.OnValueChanged += HandleStageChanged;
            _progress.OnValueChanged += HandleProgressChanged;
            // A joining peer gets the values before this call without a change event, so report them once.
            StageChanged?.Invoke(_stage.Value, true);
        }

        public override void OnNetworkDespawn()
        {
            _stage.OnValueChanged -= HandleStageChanged;
            _progress.OnValueChanged -= HandleProgressChanged;
        }

        /// <summary>Sends a blade contact (world space) to the server. Safe to call before the body has spawned: it does nothing.</summary>
        public void SubmitContact(Vector3 worldPoint)
        {
            if (!IsSpawned)
                return;
            SubmitContactRpc(transform.InverseTransformPoint(worldPoint));
        }

        /// <summary>Asks the server to open the skin. Rejected unless the incision is complete.</summary>
        public void RequestOpen()
        {
            if (!IsSpawned)
                return;
            RequestOpenRpc();
        }

        /// <summary>
        /// Server only. Judges one sample (body-local point) and applies it. Public so the rules can be tested
        /// without the network; the Rpc is the only way a client reaches it.
        /// </summary>
        public SampleResult EvaluateSample(Vector3 localPoint)
        {
            if (!IsServer)
            {
                Debug.LogError($"{name}: only the server may evaluate incision samples.", this);
                return SampleResult.Invalid;
            }
            if (!IsFinite(localPoint))
                return SampleResult.Invalid;
            if (_stage.Value != DissectionStage.Intact)
                return SampleResult.WrongStage;

            Vector3 line = _lineEnd - _lineStart;
            float along = Mathf.Clamp01(Vector3.Dot(localPoint - _lineStart, line) / line.sqrMagnitude);
            Vector3 closest = _lineStart + line * along;
            if (Vector3.Distance(localPoint, closest) > _toleranceMeters)
                return SampleResult.OffLine;

            float current = _progress.Value;
            if (along <= current)
                return SampleResult.Ignored;
            if (along - current > _maxStepFraction)
                return SampleResult.TooFar;

            bool complete = along >= _completeAtFraction;
            _progress.Value = complete ? 1f : along;
            if (complete)
                _stage.Value = DissectionStage.Incised;
            return SampleResult.Accepted;
        }

        /// <summary>Server only. Opens the skin if the incision is complete.</summary>
        public bool TryOpen()
        {
            if (!IsServer || _stage.Value != DissectionStage.Incised)
                return false;
            _stage.Value = DissectionStage.SkinOpen;
            return true;
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void SubmitContactRpc(Vector3 localPoint)
        {
            EvaluateSample(localPoint);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void RequestOpenRpc()
        {
            TryOpen();
        }

        private void HandleStageChanged(DissectionStage previous, DissectionStage current)
        {
            StageChanged?.Invoke(current, false);
        }

        private void HandleProgressChanged(float previous, float current)
        {
            ProgressChanged?.Invoke(current);
        }

        private void Validate()
        {
            if (_lineStart == _lineEnd)
                Debug.LogError($"{name}: the midline has no length. Run Tools > DIYsect > Add Body Dissection.", this);
        }

        private static bool IsFinite(Vector3 point)
        {
            return float.IsFinite(point.x) && float.IsFinite(point.y) && float.IsFinite(point.z);
        }
    }
}
