// Authority: none. Presentation only; it reads who holds each organ and where the pointers are, and never writes either.
using System.Collections.Generic;
using DIYsect.Classroom;
using DIYsect.Networking;
using Unity.Netcode;
using UnityEngine;

namespace DIYsect.Learning
{
    /// <summary>
    /// Shows a small info card (name, function, one fact) next to an organ on every device while the organ is held or a
    /// pointer rests on it, and hides it when the organ is let go and no pointer is on it.
    /// It keeps no network state of its own. It listens to events that every peer already gets:
    /// <see cref="OrganGrabAuthority.HolderChanged"/> (picked up or let go, by anyone) and
    /// <see cref="PointerBoard.PointersChanged"/> (the professor's or the student's pointer). The yes or no of showing a
    /// card is decided by <see cref="OrganInfoCardRules"/> through <see cref="OrganInfoCardTracker"/>.
    /// A device that joins late reads the holders and pointers once when the body spawns.
    ///
    /// How it fits with the other labels: OrganGrabPresenter shows the organ's name just above it while it is held, and
    /// OrganHoverLabel shows the name while this device's controller points at it. Those stay as they are. The card is the
    /// longer text and sits higher, starting <c>_cardHeight</c> metres above the organ's middle, which is above the name
    /// tag (the tags sit about 0.15 to 0.22 metres up), so the two never cover each other. The card never replaces a
    /// name tag and the tags do not carry the function or the fact.
    /// A pointer is the professor's or the student's pointer from the PointerBoard, so a card for a pointed organ shows on
    /// every device a short moment after the pointer settles. Cards are built the first time they are needed and then reused.
    /// </summary>
    [RequireComponent(typeof(SharedBody), typeof(OrganGrabAuthority), typeof(PointerBoard))]
    public class OrganInfoCardPresenter : NetworkBehaviour
    {
        [SerializeField, Tooltip("The text for every organ. Run Tools > DIYsect > Add Organ Info Cards to fill this in.")]
        private OrganInfoLibrary _library;
        [SerializeField, Tooltip("Font for the card text. Run Tools > DIYsect > Add Organ Info Cards to fill this in.")]
        private Font _font;
        [SerializeField, Min(0.1f), Tooltip("Metres between the middle of the organ and the bottom of the card. Keep it above the name tag height (0.15 to 0.22).")]
        private float _cardHeight = 0.25f;

        private SharedBody _body;
        private OrganGrabAuthority _authority;
        private PointerBoard _board;
        private OrganInfoCardTracker _tracker;
        private readonly Dictionary<int, OrganInfoCard> _cards = new Dictionary<int, OrganInfoCard>();
        private readonly HashSet<int> _warned = new HashSet<int>();
        private readonly List<int> _pointed = new List<int>(2);

        private void Awake()
        {
            _body = GetComponent<SharedBody>();
            _authority = GetComponent<OrganGrabAuthority>();
            _board = GetComponent<PointerBoard>();
            if (_library == null)
            {
                Debug.LogError($"{name}: OrganInfoCardPresenter has no organ info library. Run Tools > DIYsect > Add Organ Info Cards.", this);
                enabled = false;
            }
        }

        public override void OnNetworkSpawn()
        {
            if (!enabled)
                return;
            _tracker = new OrganInfoCardTracker(_body.Organs.Count);
            _tracker.Changed += OnCardStateChanged;
            _authority.HolderChanged += OnHolderChanged;
            _board.PointersChanged += OnPointersChanged;

            // A device that joins late gets the holders and pointers with the spawn and no change events.
            for (int i = 0; i < _body.Organs.Count; i++)
                if (_authority.GetHolder(i) != OrganGrabAuthority.NoHolder)
                    _tracker.Handle(i, OrganInfoCardEvent.Grabbed);
            OnPointersChanged();
        }

        public override void OnNetworkDespawn()
        {
            if (_authority != null)
                _authority.HolderChanged -= OnHolderChanged;
            if (_board != null)
                _board.PointersChanged -= OnPointersChanged;
            if (_tracker != null)
                _tracker.Changed -= OnCardStateChanged;
            _tracker = null;
            Clear();
        }

        /// <summary>The card of the organ, or null if it has never been shown on this device.</summary>
        public OrganInfoCard GetCard(int organIndex)
        {
            return _cards.TryGetValue(organIndex, out OrganInfoCard card) ? card : null;
        }

        /// <summary>Why the organ's card is shown now (or Hidden).</summary>
        public OrganInfoCardState StateOf(int organIndex)
        {
            return _tracker != null ? _tracker.StateOf(organIndex) : OrganInfoCardState.Hidden;
        }

        private void OnHolderChanged(int organIndex, ulong holder)
        {
            if (_tracker == null)
                return;
            _tracker.Handle(organIndex, holder != OrganGrabAuthority.NoHolder ? OrganInfoCardEvent.Grabbed : OrganInfoCardEvent.Released);
        }

        private void OnPointersChanged()
        {
            if (_tracker == null)
                return;
            _pointed.Clear();
            AddPointed(PointerRole.Professor);
            AddPointed(PointerRole.Student);
            _tracker.SyncPointers(_pointed);
        }

        private void AddPointed(PointerRole role)
        {
            int organIndex = _board.OrganOf(role);
            if (organIndex != PointerBoard.None)
                _pointed.Add(organIndex);
        }

        private void OnCardStateChanged(int organIndex, OrganInfoCardState previous, OrganInfoCardState current)
        {
            bool wasShown = OrganInfoCardRules.IsShown(previous);
            bool isShown = OrganInfoCardRules.IsShown(current);
            if (isShown && !wasShown)
                GetOrCreateCard(organIndex)?.Show();
            else if (!isShown && wasShown)
                GetCard(organIndex)?.Hide();
        }

        private OrganInfoCard GetOrCreateCard(int organIndex)
        {
            if (_cards.TryGetValue(organIndex, out OrganInfoCard existing))
                return existing;
            if (organIndex < 0 || organIndex >= _body.Organs.Count)
                return null;

            Transform organ = _body.Organs[organIndex].transform;
            if (!_library.TryGet(organ.name, out OrganInfoEntry entry))
            {
                if (_warned.Add(organIndex))
                    Debug.LogWarning($"{name}: the organ info library has no entry for '{organ.name}', so it gets no card.", this);
                return null;
            }
            if (!organ.TryGetComponent(out ISnappable snappable))
            {
                if (_warned.Add(organIndex))
                    Debug.LogError($"{organ.name}: no SnappableOrgan, so its info card cannot find the middle of the organ. Run Tools > DIYsect > Add Organ Sockets.", organ);
                return null;
            }

            Font font = _font != null ? _font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            OrganInfoCard card = OrganInfoCard.Create(organ, snappable, OrganInfoCardText.Build(entry), _cardHeight, font);
            _cards[organIndex] = card;
            return card;
        }

        private void Clear()
        {
            foreach (OrganInfoCard card in _cards.Values)
                if (card != null)
                    Destroy(card.gameObject);
            _cards.Clear();
            _warned.Clear();
        }
    }
}
