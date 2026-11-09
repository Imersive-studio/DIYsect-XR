// Authority: none. Local presentation and buttons for the professor. Every press is a request the server checks:
// only the host is obeyed when it asks to let a student speak or to take that away.
using System;
using System.Collections.Generic;
using DIYsect.Classroom;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using XRMultiplayer;

namespace DIYsect.ClassRoster
{
    /// <summary>
    /// The professor's participant list, like a video call: one row per student with their name, a raised-hand marker, a
    /// microphone marker (green when they are speaking, grey and slashed when they are muted) and an Allow / Mute button. A student
    /// cannot unmute until the professor presses Allow, which also takes their hand down; Mute takes the permission away again.
    /// Raised hands are listed first, in the order they went up. It refreshes when a hand or a permission changes, and a few times a
    /// second otherwise, because a student's name and microphone state arrive over the network on their own schedule.
    /// </summary>
    public class SpeakPanel : MonoBehaviour
    {
        /// <summary>One row of the list.</summary>
        [Serializable]
        public class Row
        {
            public GameObject Root;
            public TMP_Text Name;
            public TMP_Text Status;
            public TMP_Text Hand;
            public Renderer HandIcon;
            public Renderer MicIcon;
            public GameObject MicSlash;
            public TMP_Text ButtonLabel;
            public XRSimpleInteractable Button;
        }

        // XRPlatformType: 0 Meta, 1 Android XR, 2 other OpenXR runtime, 3 not on OpenXR (a phone or laptop).
        private const int LastHeadsetPlatform = 2;

        [SerializeField] private SpeakBoard _speak;
        [SerializeField] private HandBoard _hands;
        [SerializeField] private Row[] _rows;
        [SerializeField, Tooltip("Shown when no student is in the room, or how many more there are than rows.")]
        private TMP_Text _summary;
        [SerializeField, Min(0.1f)] private float _refreshSeconds = 0.25f;
        [SerializeField] private Color _liveColor = new Color(0.369f, 0.949f, 0.761f);
        [SerializeField] private Color _canSpeakColor = new Color(0.45f, 0.7f, 1f);
        [SerializeField] private Color _mutedColor = new Color(0.42f, 0.52f, 0.58f);
        [SerializeField] private Color _handColor = new Color(1f, 0.478f, 0.349f);

        private ulong[] _rowClient;
        private UnityEngine.Events.UnityAction<SelectEnterEventArgs>[] _handlers;
        private MaterialPropertyBlock _block;
        private float _nextRefresh;

        /// <summary>The rows, for tests.</summary>
        public int RowCount => _rows != null ? _rows.Length : 0;

        private void Awake()
        {
            bool valid = _speak != null && _hands != null && _rows != null && _rows.Length > 0 && _summary != null;
            if (valid)
                foreach (Row row in _rows)
                    if (row == null || row.Root == null || row.Name == null || row.Status == null || row.Hand == null || row.HandIcon == null
                        || row.MicIcon == null || row.MicSlash == null || row.ButtonLabel == null || row.Button == null)
                        valid = false;
            if (!valid)
            {
                Debug.LogError($"{name}: SpeakPanel is missing a board, a row part or the summary. Run Tools > DIYsect > Add Speak Panel.", this);
                enabled = false;
                return;
            }
            _rowClient = new ulong[_rows.Length];
            _block = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            _speak.Changed += Refresh;
            _hands.Changed += Refresh;
            _handlers = new UnityEngine.Events.UnityAction<SelectEnterEventArgs>[_rows.Length];
            for (int i = 0; i < _rows.Length; i++)
            {
                int index = i;
                _handlers[i] = _ => Press(index);
                _rows[i].Button.selectEntered.AddListener(_handlers[i]);
            }
            Refresh();
        }

        private void OnDisable()
        {
            if (_speak != null)
                _speak.Changed -= Refresh;
            if (_hands != null)
                _hands.Changed -= Refresh;
            if (_handlers == null)
                return;
            for (int i = 0; i < _rows.Length; i++)
                if (_rows[i].Button != null && _handlers[i] != null)
                    _rows[i].Button.selectEntered.RemoveListener(_handlers[i]);
        }

        private void Update()
        {
            if (Time.unscaledTime >= _nextRefresh)
                Refresh();
        }

        /// <summary>Presses the button on a row: Allow when the student may not speak yet, Mute when they may.</summary>
        public void Press(int index)
        {
            if (index < 0 || index >= _rowClient.Length || !_rows[index].Root.activeSelf)
                return;
            ulong client = _rowClient[index];
            if (_speak.IsAllowed(client))
            {
                _speak.RequestRevoke(client);
            }
            else
            {
                _speak.RequestAllow(client);
                // Allowing answers the question, so the hand comes down.
                _hands.RequestLower(client);
            }
        }

        /// <summary>Rebuilds the rows from the students in the room right now.</summary>
        public void Refresh()
        {
            _nextRefresh = Time.unscaledTime + _refreshSeconds;
            var entries = new List<RosterEntry>();
            var live = new Dictionary<ulong, bool>();
            foreach (XRINetworkPlayer player in FindObjectsByType<XRINetworkPlayer>(FindObjectsSortMode.None))
            {
                if (player == XRINetworkPlayer.LocalPlayer || !player.IsSpawned)
                    continue;
                RosterKind kind = player.platformType.Value <= LastHeadsetPlatform ? RosterKind.Headset : RosterKind.Screen;
                entries.Add(new RosterEntry(player.OwnerClientId, player.playerName, kind, 0));
                live[player.OwnerClientId] = !player.selfMuted.Value;
            }

            var rows = new List<SpeakRow>(entries.Count);
            foreach (RosterEntry entry in entries)
            {
                bool allowed = _speak.IsAllowed(entry.ClientId);
                rows.Add(new SpeakRow(entry.ClientId, ClassRosterText.Label(entry, entries), _hands.OrderOf(entry.ClientId), allowed, allowed && live[entry.ClientId]));
            }
            List<SpeakRow> sorted = SpeakRowText.Sorted(rows);

            for (int i = 0; i < _rows.Length; i++)
            {
                Row view = _rows[i];
                bool used = i < sorted.Count;
                if (view.Root.activeSelf != used)
                    view.Root.SetActive(used);
                if (!used)
                    continue;
                SpeakRow row = sorted[i];
                _rowClient[i] = row.ClientId;
                MicState state = SpeakRowText.StateOf(row.Allowed, row.Live);
                SetText(view.Name, row.Name);
                SetText(view.Status, SpeakRowText.StatusText(state, row.HandOrder));
                SetText(view.Hand, SpeakRowText.HandText(row.HandOrder));
                view.HandIcon.gameObject.SetActive(row.HandOrder > 0);
                Tint(view.HandIcon, _handColor);
                Tint(view.MicIcon, state == MicState.Live ? _liveColor : state == MicState.CanSpeak ? _canSpeakColor : _mutedColor);
                view.MicSlash.SetActive(state != MicState.Live);
                SetText(view.ButtonLabel, SpeakRowText.ButtonLabel(row.Allowed));
            }

            string summary = sorted.Count == 0 ? SpeakRowText.EmptyText : sorted.Count > _rows.Length ? $"+{sorted.Count - _rows.Length} more" : string.Empty;
            SetText(_summary, summary);
        }

        private void Tint(Renderer renderer, Color color)
        {
            renderer.GetPropertyBlock(_block);
            _block.SetColor("_BaseColor", color);
            _block.SetColor("_Color", color);
            renderer.SetPropertyBlock(_block);
        }

        private static void SetText(TMP_Text mesh, string text)
        {
            if (mesh.text != text)
                mesh.text = text;
        }
    }
}
