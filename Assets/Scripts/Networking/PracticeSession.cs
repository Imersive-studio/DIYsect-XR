// Authority: this device. It starts a session that only this headset hosts, so the cut and organ rules (which the server decides) run with nobody else in the room.
using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace DIYsect.Networking
{
    /// <summary>
    /// Lets the student practise alone. The cuts and organ rules are decided by the host, so with no room to join the
    /// student would have no server and nothing could be dissected. If no room has been joined a little while after the
    /// scene starts, this starts a local host and switches off the scripts that would keep looking for a room (so they
    /// cannot start a second connection). A room that is joined in time wins and this does nothing.
    /// </summary>
    public class PracticeSession : MonoBehaviour
    {
        [SerializeField, Min(0f), Tooltip("Seconds to wait for a room before starting a practice session.")]
        private float _waitSeconds = 14f;
        [SerializeField, Tooltip("Scripts that look for or join a room. They are switched off when practice starts.")]
        private Behaviour[] _disableOnStart;

        /// <summary>True once this device is hosting its own practice session.</summary>
        public bool InPractice { get; private set; }

        private IEnumerator Start()
        {
            float started = Time.unscaledTime;
            while (true)
            {
                yield return new WaitForSeconds(1f);
                NetworkManager manager = NetworkManager.Singleton;
                bool running = manager != null && (manager.IsListening || manager.IsConnectedClient);
                if (manager == null)
                    continue;
                if (PracticeRules.ShouldStart(Time.unscaledTime - started, _waitSeconds, running))
                {
                    StartPractice(manager);
                    yield break;
                }
                if (running)
                    yield break;
            }
        }

        private void StartPractice(NetworkManager manager)
        {
            if (_disableOnStart != null)
                foreach (Behaviour behaviour in _disableOnStart)
                    if (behaviour != null)
                        behaviour.enabled = false;

            // The template's transport may be set up for the cloud relay, which a local host has no data for.
            if (manager.NetworkConfig.NetworkTransport is UnityTransport transport)
                transport.SetConnectionData("127.0.0.1", 7777);

            InPractice = manager.StartHost();
            if (InPractice)
                Debug.Log("[PracticeSession] No room found. Started a practice session on this headset.");
            else
                Debug.LogError("[PracticeSession] No room was found and the practice session could not start.", this);
        }
    }
}
