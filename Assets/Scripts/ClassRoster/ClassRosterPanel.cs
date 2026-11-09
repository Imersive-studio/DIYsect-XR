// Authority: none. Local presentation for the professor. It reads the connected players and the server-owned pins.
using System.Collections.Generic;
using System;
using DIYsect.Classroom;
using TMPro;
using UnityEngine;
using XRMultiplayer;

namespace DIYsect.ClassRoster
{
    /// <summary>
    /// A floating panel in the professor's headset that lists every student in the room: their name, whether they are on
    /// a headset or a phone or laptop, whether their hand is up and how many questions they have pinned. It refreshes when a pin changes and about
    /// once a second otherwise, because a student's name arrives a moment after they join.
    /// </summary>
    public class ClassRosterPanel : MonoBehaviour
    {
        /// <summary>One line of the list. The mint dot is a child of Root, so it shows and hides with the line.</summary>
        [Serializable]
        public class Row
        {
            public GameObject Root;
            public TMP_Text Name;
            public TMP_Text Detail;
            public TMP_Text Hand;
        }

        // XRPlatformType: 0 Meta, 1 Android XR, 2 other OpenXR runtime, 3 not on OpenXR (a phone or laptop).
        private const int LastHeadsetPlatform = 2;

        [SerializeField] private QuestionBoard _board;
        [SerializeField, Tooltip("The raised hands. The roster shows a hand next to each name, first to raise first.")]
        private HandBoard _hands;
        [SerializeField, Tooltip("The student's help request. While it is open the roster says so and what to do.")]
        private AssistBoard _assist;
        [SerializeField, Tooltip("The lines above the list: the help request, then the join code and link.")]
        private TMP_Text _text;
        [SerializeField, Tooltip("The count under the list, such as 3 students, 1 hand up.")]
        private TMP_Text _summary;
        [SerializeField, Tooltip("One line per student, up to ClassRosterText.MaxLines.")]
        private Row[] _rows;
        [SerializeField, Min(0.25f)] private float _refreshSeconds = 1f;

        private float _nextRefresh;
        private string _shown;

        private void Awake()
        {
            bool valid = _text != null && _summary != null && _rows != null && _rows.Length > 0;
            if (valid)
                foreach (Row row in _rows)
                    if (row == null || row.Root == null || row.Name == null || row.Detail == null || row.Hand == null)
                        valid = false;
            if (!valid)
            {
                Debug.LogError($"{name}: ClassRosterPanel is missing its text, summary or rows. Run Tools > DIYsect > Add Class Roster.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (_board != null)
                _board.PinsChanged += Refresh;
            if (_hands != null)
                _hands.Changed += Refresh;
            if (_assist != null)
                _assist.PhaseChanged += OnAssistChanged;
            Refresh();
        }

        private void OnDisable()
        {
            if (_board != null)
                _board.PinsChanged -= Refresh;
            if (_hands != null)
                _hands.Changed -= Refresh;
            if (_assist != null)
                _assist.PhaseChanged -= OnAssistChanged;
        }

        private void OnAssistChanged(AssistPhase phase)
        {
            Refresh();
        }

        /// <summary>Takes down the hand that went up first (the next student to answer). Does nothing when no hand is up.</summary>
        public void LowerNext()
        {
            if (_hands != null && _hands.Count > 0)
                _hands.RequestLower(_hands[0]);
        }

        /// <summary>Takes down every hand.</summary>
        public void LowerAll()
        {
            if (_hands != null)
                _hands.RequestLowerAll();
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
                entries.Add(new RosterEntry(player.OwnerClientId, player.playerName, kind, CountQuestions(player.OwnerClientId), _hands != null ? _hands.OrderOf(player.OwnerClientId) : 0));
            }

            // The invite is shown once a room exists, so the professor always has the code to read out.
            string code = XRINetworkGameManager.Connected.Value ? XRINetworkGameManager.ConnectedRoomCode : null;
            string top = AssistLine() + ClassRosterText.InviteLines(code, RoomInvite.BuildLink(code));
            List<RosterEntry> sorted = ClassRosterText.Sorted(entries);
            string summary = ClassRosterText.Summary(sorted);
            if (sorted.Count > _rows.Length)
                summary += $", +{sorted.Count - _rows.Length} more";

            // The whole picture as one string, so nothing is rewritten while it has not changed.
            string key = top + "|" + summary + "|" + string.Join("|", sorted.ConvertAll(e => ClassRosterText.Label(e, sorted) + e.HandOrder + ClassRosterText.DetailText(e)));
            if (key == _shown)
                return;
            _shown = key;

            _text.text = top;
            _summary.text = summary;
            for (int i = 0; i < _rows.Length; i++)
            {
                Row view = _rows[i];
                bool used = i < sorted.Count;
                if (view.Root.activeSelf != used)
                    view.Root.SetActive(used);
                if (!used)
                    continue;
                RosterEntry entry = sorted[i];
                view.Name.text = ClassRosterText.Label(entry, sorted);
                view.Detail.text = ClassRosterText.DetailText(entry);
                view.Hand.text = entry.HandUp ? ClassRosterText.HandLabel(entry.HandOrder) : string.Empty;
            }
        }

        // The first line of the panel while a student has asked for help, in the beacon's colours.
        private string AssistLine()
        {
            switch (_assist != null ? _assist.Phase : AssistPhase.None)
            {
                case AssistPhase.Requested:
                    return "<color=#FF7A59>Needs help: grab the depth handle to guide</color>\n";
                case AssistPhase.Helping:
                    return "<color=#5EF2C2>You are guiding the depth</color>\n";
                default:
                    return string.Empty;
            }
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
