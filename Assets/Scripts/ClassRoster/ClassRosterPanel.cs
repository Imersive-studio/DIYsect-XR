// Authority: none. Local presentation for the professor. It reads the connected players and the server-owned pins.
using System.Collections.Generic;
using DIYsect.Classroom;
using UnityEngine;
using XRMultiplayer;

namespace DIYsect.ClassRoster
{
    /// <summary>
    /// A floating panel in the professor's headset that lists every student in the room: their name, whether they are on
    /// a headset or a phone or laptop, and how many questions they have pinned. It refreshes when a pin changes and about
    /// once a second otherwise, because a student's name arrives a moment after they join.
    /// </summary>
    public class ClassRosterPanel : MonoBehaviour
    {
        // XRPlatformType: 0 Meta, 1 Android XR, 2 other OpenXR runtime, 3 not on OpenXR (a phone or laptop).
        private const int LastHeadsetPlatform = 2;

        [SerializeField] private QuestionBoard _board;
        [SerializeField] private TextMesh _text;
        [SerializeField, Min(0.25f)] private float _refreshSeconds = 1f;

        private float _nextRefresh;
        private string _shown;

        private void Awake()
        {
            if (_text == null)
            {
                Debug.LogError($"{name}: ClassRosterPanel has no TextMesh. Run Tools > DIYsect > Add Class Roster.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (_board != null)
                _board.PinsChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (_board != null)
                _board.PinsChanged -= Refresh;
        }

        private void Update()
        {
            if (Time.unscaledTime >= _nextRefresh)
                Refresh();
        }

        /// <summary>Rebuilds the text from the players in the room right now.</summary>
        public void Refresh()
        {
            _nextRefresh = Time.unscaledTime + _refreshSeconds;
            var entries = new List<RosterEntry>();
            foreach (XRINetworkPlayer player in FindObjectsByType<XRINetworkPlayer>(FindObjectsSortMode.None))
            {
                if (player == XRINetworkPlayer.LocalPlayer || !player.IsSpawned)
                    continue;
                RosterKind kind = player.platformType.Value <= LastHeadsetPlatform ? RosterKind.Headset : RosterKind.Screen;
                entries.Add(new RosterEntry(player.OwnerClientId, player.playerName, kind, CountQuestions(player.OwnerClientId)));
            }

            string text = ClassRosterText.Format(entries);
            if (text == _shown)
                return;
            _shown = text;
            _text.text = text;
        }

        private int CountQuestions(ulong clientId)
        {
            if (_board == null || !_board.IsSpawned)
                return 0;
            int count = 0;
            for (int i = 0; i < _board.Count; i++)
                if (_board[i].AuthorClientId == clientId)
                    count++;
            return count;
        }
    }
}
