// Authority: none. Local client behavior; it only forwards the classmate's join request to ClassmateAutoJoin.
using UnityEngine;
using XRMultiplayer;

namespace DIYsect.ClassmateConnection
{
    /// <summary>
    /// Lets the start screen drive <see cref="ClassmateAutoJoin"/> through <see cref="IClassmateJoiner"/>. ClassmateAutoJoin
    /// lives in the template's assembly, which cannot see this one, so the interface is implemented here.
    /// </summary>
    public class ClassmateJoinAdapter : MonoBehaviour, IClassmateJoiner
    {
        [SerializeField] private ClassmateAutoJoin _join;

        public bool WaitingForPress => _join != null && _join.isActiveAndEnabled && _join.WaitForJoinPress && !_join.JoinPressed;

        public bool Connected => XRINetworkGameManager.Connected.Value;

        public bool CodeRejected => _join != null && _join.CodeRejected;

        private void Awake()
        {
            if (_join == null)
            {
                Debug.LogError($"{name}: ClassmateJoinAdapter needs the scene's ClassmateAutoJoin. Run Tools > DIYsect > Build Shared Scene.", this);
                enabled = false;
            }
        }

        public void PressJoin() => _join.PressJoin();

        public bool JoinWithCode(string typedCode) => _join.JoinWithCode(typedCode);
    }
}
