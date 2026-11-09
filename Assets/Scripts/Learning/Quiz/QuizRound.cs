// Authority: the server. It alone writes the phase, the target organ, the round number and the counts, after
// checking every request against QuizRule. Only the host (the professor) may start, end or clear a round; only
// other clients (classmates) may answer. Every peer and late joiner reads the same values from the network state.
using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using DIYsect.Networking;

namespace DIYsect.Learning
{
    /// <summary>
    /// The shared "Tap the ..." quiz on the anatomy body. The professor picks a target organ and starts a round;
    /// each classmate taps one organ as the answer; the server counts right and wrong answers and how many taps each
    /// organ got, and tells the asker at once whether they were right. The start request also carries the question text
    /// (for example "Tap the organ that stores bile"), which the server publishes with the round. Ending the round freezes the counts on every
    /// screen until the next round or Close. Answers carry the round number, so a late answer to an earlier round
    /// cannot count for the current one.
    /// </summary>
    [RequireComponent(typeof(SharedBody))]
    public class QuizRound : NetworkBehaviour
    {
        private readonly QuizRule _rule = QuizRule.Default;
        // Listeners re-read every value on each StateChanged, so the order these values arrive in on a peer does not matter.
        private readonly NetworkVariable<int> _target = new NetworkVariable<int>(-1);
        private readonly NetworkVariable<int> _round = new NetworkVariable<int>(0);
        private readonly NetworkVariable<int> _right = new NetworkVariable<int>(0);
        private readonly NetworkVariable<int> _wrong = new NetworkVariable<int>(0);
        // The question text the professor chose. It travels with the round so a classmate can read it without
        // owning the question list. Empty means "just say tap the <organ>".
        private readonly NetworkVariable<FixedString128Bytes> _prompt = new NetworkVariable<FixedString128Bytes>();
        private readonly NetworkVariable<QuizPhase> _phase = new NetworkVariable<QuizPhase>(QuizPhase.Idle);

        private SharedBody _body;
        private NetworkList<int> _tapsPerOrgan;
        private QuizTally _tally;

        /// <summary>Raised on every peer when the phase, target, round or any count changes, and once on spawn.</summary>
        public event Action StateChanged;

        /// <summary>Raised on the client that answered when the server judged its answer (round, result, correct).</summary>
        public event Action<int, QuizResult, bool> LocalAnswerResolved;

        /// <summary>Raised on the client that asked to start, end or clear a round when the server refused it.</summary>
        public event Action<QuizResult> LocalRequestRejected;

        // Safe before Awake: a panel in the same scene may be enabled before this component.
        public SharedBody Body => _body != null ? _body : (_body = GetComponent<SharedBody>());
        public QuizPhase Phase => _phase.Value;
        /// <summary>The organ index the current or last round asks for, or -1 before the first round.</summary>
        public int TargetOrgan => _target.Value;
        /// <summary>Counts up by one for every started round. 0 means no round has started yet.</summary>
        public int Round => _round.Value;
        public int RightCount => _right.Value;
        public int WrongCount => _wrong.Value;
        /// <summary>The question text of the current or last round, or empty when the professor sent none.</summary>
        public string Prompt => _prompt.Value.ToString();
        /// <summary>Number of organs with a published count. Equals the body's organ count once spawned.</summary>
        public int OrganCount => _tapsPerOrgan != null ? _tapsPerOrgan.Count : 0;

        /// <summary>Taps the organ got in the current or last round.</summary>
        public int TapsFor(int organIndex) =>
            organIndex >= 0 && organIndex < OrganCount ? _tapsPerOrgan[organIndex] : 0;

        private void Awake()
        {
            _body = GetComponent<SharedBody>();
            _tapsPerOrgan = new NetworkList<int>();
        }

        public override void OnNetworkSpawn()
        {
            _phase.OnValueChanged += HandlePhaseChanged;
            _target.OnValueChanged += HandleIntChanged;
            _round.OnValueChanged += HandleIntChanged;
            _right.OnValueChanged += HandleIntChanged;
            _wrong.OnValueChanged += HandleIntChanged;
            _prompt.OnValueChanged += HandlePromptChanged;
            _tapsPerOrgan.OnListChanged += HandleListChanged;

            if (IsServer)
            {
                int organs = Body.Organs.Count;
                _tally = new QuizTally(organs);
                _tapsPerOrgan.Clear();
                for (int i = 0; i < organs; i++)
                    _tapsPerOrgan.Add(0);
            }
            // A joining peer gets the values before this call without a change event, so report them once.
            StateChanged?.Invoke();
        }

        public override void OnNetworkDespawn()
        {
            _phase.OnValueChanged -= HandlePhaseChanged;
            _target.OnValueChanged -= HandleIntChanged;
            _round.OnValueChanged -= HandleIntChanged;
            _right.OnValueChanged -= HandleIntChanged;
            _wrong.OnValueChanged -= HandleIntChanged;
            _prompt.OnValueChanged -= HandlePromptChanged;
            _tapsPerOrgan.OnListChanged -= HandleListChanged;
        }

        /// <summary>Asks the server to start a round for the organ, with no question text. Only the host is obeyed.</summary>
        public void RequestStart(int organIndex)
        {
            RequestStart(organIndex, string.Empty);
        }

        /// <summary>
        /// Asks the server to start a round for the organ and show the question text to classmates
        /// (cut to <see cref="QuizPromptText.MaxBytes"/> bytes). Only the host is obeyed.
        /// </summary>
        public void RequestStart(int organIndex, string prompt)
        {
            if (IsSpawned)
                StartRpc(organIndex, new FixedString128Bytes(QuizPromptText.Clamp(prompt)));
        }

        /// <summary>Asks the server to end the running round. Only the host is obeyed.</summary>
        public void RequestEnd()
        {
            if (IsSpawned)
                ControlRpc(QuizEvent.End);
        }

        /// <summary>Asks the server to clear the finished round from every screen. Only the host is obeyed.</summary>
        public void RequestClose()
        {
            if (IsSpawned)
                ControlRpc(QuizEvent.Close);
        }

        /// <summary>Sends this client's answer for the current round. Safe to call before spawn: it does nothing.</summary>
        public void SubmitAnswer(int organIndex)
        {
            if (IsSpawned)
                AnswerRpc(_round.Value, organIndex);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void StartRpc(int organIndex, FixedString128Bytes prompt, RpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            QuizResult result = TryStart(sender, organIndex, prompt);
            if (result != QuizResult.Accepted)
                ReportRejection(sender, result);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void ControlRpc(QuizEvent quizEvent, RpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            QuizResult result = TryControl(sender, quizEvent);
            if (result != QuizResult.Accepted)
                ReportRejection(sender, result);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void AnswerRpc(int round, int organIndex, RpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            QuizResult result = TryAnswer(sender, round, organIndex, out bool correct);
            // The answer goes back to the asker only. The host asking itself needs no message: it is the server.
            if (sender == NetworkManager.LocalClientId)
                LocalAnswerResolved?.Invoke(round, result, correct);
            else
                AnswerResolvedRpc(round, result, correct, RpcTarget.Single(sender, RpcTargetUse.Temp));
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void AnswerResolvedRpc(int round, QuizResult result, bool correct, RpcParams rpcParams)
        {
            LocalAnswerResolved?.Invoke(round, result, correct);
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void RequestRejectedRpc(QuizResult result, RpcParams rpcParams)
        {
            LocalRequestRejected?.Invoke(result);
        }

        // Server only. The target and counts are written before the phase, so a peer that reacts to the phase sees the new round.
        private QuizResult TryStart(ulong sender, int organIndex, FixedString128Bytes prompt)
        {
            if (sender != NetworkManager.ServerClientId)
                return QuizResult.NotAllowed;
            QuizResult result = _rule.Evaluate(_phase.Value, QuizEvent.Start, out QuizPhase next);
            if (result != QuizResult.Accepted)
                return result;
            result = _tally.Begin(organIndex);
            if (result != QuizResult.Accepted)
                return result;

            for (int i = 0; i < _tapsPerOrgan.Count; i++)
                if (_tapsPerOrgan[i] != 0)
                    _tapsPerOrgan[i] = 0;
            _right.Value = 0;
            _wrong.Value = 0;
            _target.Value = organIndex;
            _prompt.Value = prompt;
            _round.Value = _round.Value + 1;
            _phase.Value = next;
            return QuizResult.Accepted;
        }

        // Server only. End and Close change the phase and nothing else.
        private QuizResult TryControl(ulong sender, QuizEvent quizEvent)
        {
            if (quizEvent != QuizEvent.End && quizEvent != QuizEvent.Close)
                return QuizResult.Unknown;
            if (sender != NetworkManager.ServerClientId)
                return QuizResult.NotAllowed;
            QuizResult result = _rule.Evaluate(_phase.Value, quizEvent, out QuizPhase next);
            if (result == QuizResult.Accepted)
                _phase.Value = next;
            return result;
        }

        // Server only. Checked in this order so a malformed answer never uses up the sender's turn.
        private QuizResult TryAnswer(ulong sender, int round, int organIndex, out bool correct)
        {
            correct = false;
            if (sender == NetworkManager.ServerClientId)
                return QuizResult.NotAllowed;
            QuizResult result = _rule.Evaluate(_phase.Value, QuizEvent.Answer, out _);
            if (result != QuizResult.Accepted)
                return result;
            if (round != _round.Value)
                return QuizResult.StaleRound;
            result = _tally.Record(sender, organIndex, out correct);
            if (result != QuizResult.Accepted)
                return result;

            _tapsPerOrgan[organIndex] = _tally.CountFor(organIndex);
            _right.Value = _tally.Right;
            _wrong.Value = _tally.Wrong;
            return QuizResult.Accepted;
        }

        private void ReportRejection(ulong sender, QuizResult result)
        {
            if (sender == NetworkManager.LocalClientId)
                LocalRequestRejected?.Invoke(result);
            else
                RequestRejectedRpc(result, RpcTarget.Single(sender, RpcTargetUse.Temp));
        }

        private void HandlePhaseChanged(QuizPhase previous, QuizPhase current)
        {
            StateChanged?.Invoke();
        }

        private void HandleIntChanged(int previous, int current)
        {
            StateChanged?.Invoke();
        }

        private void HandlePromptChanged(FixedString128Bytes previous, FixedString128Bytes current)
        {
            StateChanged?.Invoke();
        }

        private void HandleListChanged(NetworkListEvent<int> change)
        {
            StateChanged?.Invoke();
        }
    }
}
