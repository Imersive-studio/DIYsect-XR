// Authority: none. Local display of the room this device is in; nothing is sent over the network.
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace XRMultiplayer
{
    /// <summary>The words on the room code board. Pure, so it is tested without a scene or a network.</summary>
    public static class RoomCodeBoardText
    {
        public const string Title = "JOIN THIS CLASS";
        public const string NoCode = "------";
        public const string PhoneHint = "On a phone: open DIYsect Classmate, tap \"Have a room code instead?\", type the code and tap Join. Headsets join on their own.";
        public const string WaitingHint = "Waiting for the room to open...";
        public const string PracticeHint = "Practice session. No one else can join.";

        /// <summary>The code line and the hint line for what this device knows about its room.</summary>
        public static (string code, string hint) For(string roomCode, bool networkRunning)
        {
            if (RoomInvite.TryNormalizeCode(roomCode, out string code))
                return (code, PhoneHint);
            return (NoCode, networkRunning ? PracticeHint : WaitingHint);
        }
    }

    /// <summary>
    /// A board in the classroom that shows the room code, so the professor can read it out and everyone in the room can
    /// see how a phone joins. It is separate from the organ screen and from the checklist panels. It checks the room twice
    /// a second; the code only changes when this device joins or leaves a room.
    /// </summary>
    public class RoomCodeBoard : MonoBehaviour
    {
        [SerializeField] private TMP_Text _code;
        [SerializeField] private TMP_Text _hint;

        private string _shownCode;
        private string _shownHint;

        private void Awake()
        {
            if (_code == null || _hint == null)
            {
                Debug.LogError($"{name}: RoomCodeBoard needs its code and hint text. Run Tools > DIYsect > Add Room Code Board.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            InvokeRepeating(nameof(Refresh), 0f, 0.5f);
        }

        private void OnDisable()
        {
            CancelInvoke(nameof(Refresh));
        }

        private void Refresh()
        {
            NetworkManager network = NetworkManager.Singleton;
            bool running = network != null && network.IsListening;
            var (code, hint) = RoomCodeBoardText.For(running ? XRINetworkGameManager.ConnectedRoomCode : null, running);
            if (code == _shownCode && hint == _shownHint)
                return;
            _shownCode = code;
            _shownHint = hint;
            _code.text = code;
            _hint.text = hint;
        }
    }
}
