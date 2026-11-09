// Authority: none. Presentation only; it reads the classmate pointer the server owns and the organ holders, and never writes either.
using System.Collections.Generic;
using DIYsect.Classroom;
using DIYsect.Interaction;
using DIYsect.Networking;
using DIYsect.Presentation;
using TMPro;
using UnityEngine;

namespace DIYsect.Learning
{
    /// <summary>
    /// "Look here" from a classmate. When a classmate taps an organ on a phone, the organ rises a few centimetres out of the
    /// body with a soft spring and a name chip ("Classmate: Liver") floats above it, on every device. The violet glow comes from
    /// <see cref="PointerPresenter"/>, which already tints the pointed organ in the classmate's colour.
    ///
    /// It keeps no state of its own about who points: every change is read again from <see cref="PointerBoard.OrganOf"/>, so a
    /// device that joins late shows the right organ at once, and the organ goes back down when the pointer is cleared (a second tap,
    /// a tap on empty space, a disconnect), when somebody grabs the organ, and when this component is switched off or destroyed.
    ///
    /// What moves: only the organ's mesh children, never the organ object itself. The organ object carries the networked pose,
    /// the grab and the socket snapping, so those never see a thing. The lift is a world-up offset on each mesh child, worked out from a base
    /// position remembered when the lift starts and restored exactly when it ends. A mesh that sits directly on the organ object
    /// cannot be lifted this way and keeps the glow and the chip only.
    /// One <see cref="RevealTimeline"/> per organ runs the animation, so a fast double tap turns it round smoothly instead of jumping.
    /// The per-frame step runs only while an organ is up or still settling.
    /// </summary>
    [RequireComponent(typeof(SharedBody), typeof(OrganGrabAuthority))]
    [RequireComponent(typeof(PointerBoard), typeof(PointerPresenter))]
    public class ClassmatePopOutPresenter : MonoBehaviour
    {
        [SerializeField, Tooltip("Font for the name chip (Inter). Run Tools > DIYsect > Add Organ Pointer to fill this in.")]
        private TMP_FontAsset _fontAsset;
        [SerializeField, Min(0f), Tooltip("Metres the organ rises.")]
        private float _liftMetres = MotionSpec.PopOutLift;
        [SerializeField, Min(0f), Tooltip("Metres between the top of the lifted organ and the name chip.")]
        private float _chipGap = LabelAnchor.Gap;

        private readonly Dictionary<int, PoppedOrgan> _organs = new Dictionary<int, PoppedOrgan>();
        private readonly List<PoppedOrgan> _running = new List<PoppedOrgan>(2);
        private SharedBody _body;
        private OrganGrabAuthority _authority;
        private PointerBoard _board;
        private PointerPresenter _presenter;
        private bool _valid;
        private int _quizOrgan = PointerBoard.None;

        /// <summary>The organ that is risen or rising now, or -1.</summary>
        public int PoppedIndex { get; private set; } = PointerBoard.None;

        private void Awake()
        {
            _body = GetComponent<SharedBody>();
            _authority = GetComponent<OrganGrabAuthority>();
            _board = GetComponent<PointerBoard>();
            _presenter = GetComponent<PointerPresenter>();
            if (_fontAsset == null)
            {
                Debug.LogError($"{name}: ClassmatePopOutPresenter has no font. Run Tools > DIYsect > Add Organ Pointer.", this);
                enabled = false;
                return;
            }
            _valid = true;
        }

        private void OnEnable()
        {
            if (!_valid)
                return;
            _board.PointersChanged += Refresh;
            _authority.HolderChanged += OnHolderChanged;
            Refresh();
        }

        private void OnDisable()
        {
            if (_board != null)
                _board.PointersChanged -= Refresh;
            if (_authority != null)
                _authority.HolderChanged -= OnHolderChanged;
            // Never leave an organ up: put every one back exactly where it was.
            foreach (PoppedOrgan organ in _organs.Values)
                organ.Reset();
            _running.Clear();
            PoppedIndex = PointerBoard.None;
        }

        private void OnDestroy()
        {
            foreach (PoppedOrgan organ in _organs.Values)
                organ.Destroy();
            _organs.Clear();
        }

        private void OnHolderChanged(int organIndex, ulong holder) => Refresh();

        /// <summary>
        /// Lifts one more organ for the quiz reveal (the correct answer), without the classmate's name chip. -1 lowers it.
        /// It rises beside any organ a classmate is pointing at.
        /// </summary>
        public void SetQuizLift(int organIndex)
        {
            _quizOrgan = organIndex;
            if (_valid && isActiveAndEnabled)
                Refresh();
        }

        // Reads the classmate slot and the holders again and turns every organ the right way.
        private void Refresh()
        {
            int wanted = _board.OrganOf(PointerRole.Classmate);
            if (wanted < 0 || wanted >= _body.Organs.Count || _authority.GetHolder(wanted) != OrganGrabAuthority.NoHolder)
                wanted = PointerBoard.None;
            PoppedIndex = wanted;
            int quiz = _quizOrgan;
            if (quiz < 0 || quiz >= _body.Organs.Count || _authority.GetHolder(quiz) != OrganGrabAuthority.NoHolder)
                quiz = PointerBoard.None;
            foreach (PoppedOrgan organ in _organs.Values)
                if (organ.Index != wanted && organ.Index != quiz)
                    organ.Lower();
            // No extra chip: the info card names the organ, and a second label over it only crowds the view.
            Raise(wanted, false);
            if (quiz != wanted)
                Raise(quiz, false);
        }

        private void Raise(int index, bool withChip)
        {
            if (index == PointerBoard.None)
                return;
            PoppedOrgan organ = GetOrCreate(index);
            organ.Raise(withChip);
            if (!_running.Contains(organ))
                _running.Add(organ);
        }

        // Polling exception: the animation needs a clock, and a lifted organ follows its parent while it is up. Runs only while one is up or settling.
        private void LateUpdate()
        {
            for (int i = _running.Count - 1; i >= 0; i--)
            {
                _running[i].Step(Time.deltaTime);
                if (!_running[i].IsUp)
                    _running.RemoveAt(i);
            }
        }

        private PoppedOrgan GetOrCreate(int index)
        {
            if (_organs.TryGetValue(index, out PoppedOrgan existing))
                return existing;
            Transform organ = _body.Organs[index].transform;
            string text = PointerLabelText.Build(PointerRole.Classmate, OrganNames.Display(organ.name), null);
            OrganLabel chip = OrganLabel.Create(organ, new OrganCaption(text, string.Empty, string.Empty), _fontAsset, _chipGap);
            chip.SetTint(_presenter.ColorOf(PointerRole.Classmate));
            var created = new PoppedOrgan(index, organ, chip, _liftMetres);
            _organs[index] = created;
            return created;
        }

        // One organ: its mesh children, their resting positions, the chip and the clock.
        private sealed class PoppedOrgan
        {
            private readonly Transform[] _nodes;
            private readonly Vector3[] _rest;
            private readonly OrganLabel _chip;
            private readonly RevealTimeline _timeline = new RevealTimeline(MotionSpec.PopOutIn, MotionSpec.PopOutOut);
            private readonly float _lift;

            public PoppedOrgan(int index, Transform organ, OrganLabel chip, float lift)
            {
                Index = index;
                _chip = chip;
                _lift = lift;
                // The organ's own children that hold meshes. The organ object itself is never moved.
                var nodes = new List<Transform>();
                foreach (Transform child in organ)
                    if (child.GetComponentInChildren<Renderer>(true) != null)
                        nodes.Add(child);
                _nodes = nodes.ToArray();
                _rest = new Vector3[_nodes.Length];
            }

            public int Index { get; }

            /// <summary>True from the first frame of the rise until the organ is fully back down.</summary>
            public bool IsUp => _timeline.IsVisible;

            public void Raise(bool withChip)
            {
                // The chip follows the reason for the lift: a classmate's name chip only for a classmate's pointer.
                if (!withChip)
                    _chip.Hide();
                RevealPhase before = _timeline.Phase;
                if (before == RevealPhase.Entering || before == RevealPhase.Shown)
                    return;
                if (before == RevealPhase.Hidden)
                    for (int i = 0; i < _nodes.Length; i++)
                        if (_nodes[i] != null)
                            _rest[i] = _nodes[i].localPosition;
                _timeline.Show();
                if (withChip)
                    _chip.Show();
            }

            public void Lower()
            {
                _timeline.Hide();
                _chip.Hide();
            }

            public void Step(float deltaSeconds)
            {
                _timeline.Tick(deltaSeconds);
                if (_timeline.Phase == RevealPhase.Hidden)
                    Place(0f);
                else
                    Place(_lift * Ease.OutBack(_timeline.Amount, 1f));
            }

            /// <summary>Back to rest at once, with no animation. Used when the component switches off.</summary>
            public void Reset()
            {
                if (_timeline.Phase != RevealPhase.Hidden)
                {
                    _timeline.Snap(false);
                    Place(0f);
                }
                if (_chip != null)
                    _chip.Hide();
            }

            public void Destroy()
            {
                if (_chip != null)
                    Object.Destroy(_chip.gameObject);
            }

            private void Place(float metres)
            {
                for (int i = 0; i < _nodes.Length; i++)
                {
                    Transform node = _nodes[i];
                    if (node == null)
                        continue;
                    Vector3 offset = metres > 0f ? node.parent.InverseTransformVector(Vector3.up * metres) : Vector3.zero;
                    node.localPosition = _rest[i] + offset;
                }
            }
        }
    }
}
