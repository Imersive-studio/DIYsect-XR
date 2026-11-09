// Authority: none. Local presentation only; it reads the organ the classmate tapped and shows its name on their own screen.
using DIYsect.Classroom;
using DIYsect.Networking;
using UnityEngine;
using UnityEngine.UI;

namespace DIYsect.Learning
{
    /// <summary>
    /// A quick tap on an organ shows its name (and one line about what it does) on the classmate's own screen. The card fades
    /// out after a few seconds. A press held for half a second places a question pin instead (PinPlacementInput), and a drag
    /// orbits the camera, so neither shows a name.
    /// While a quiz round runs the tap belongs to the quiz: it is an answer (see <see cref="ITapClaim"/>) and shows no name,
    /// because the name would give the answer away.
    /// </summary>
    public class OrganNameTapInput : MonoBehaviour
    {
        [SerializeField] private SharedBody _body;
        [SerializeField, Tooltip("Reports taps; a drag and a hold are never reported as taps.")]
        private PointerGestureTracker _gestures;
        [SerializeField, Tooltip("Camera that looks at the body. Leave empty to use the camera tagged MainCamera.")]
        private Camera _camera;
        [SerializeField] private OrganInfoLibrary _library;
        [SerializeField, Tooltip("The card that fades. It needs a CanvasGroup.")]
        private CanvasGroup _card;
        [SerializeField] private Text _label;
        [SerializeField, Min(0.1f)] private float _maxDistance = 20f;
        [SerializeField] private LayerMask _hitLayers = ~0;
        [SerializeField, Min(0f), Tooltip("Seconds the card stays fully visible.")]
        private float _holdSeconds = 2.5f;
        [SerializeField, Min(0.1f), Tooltip("Seconds the card takes to fade away.")]
        private float _fadeSeconds = 1f;
        [SerializeField, Tooltip("Behaviours that implement ITapClaim. While any of them claims the tap, no name is shown (for example the quiz answer input).")]
        private MonoBehaviour[] _tapClaimers = new MonoBehaviour[0];

        private System.Collections.Generic.List<ITapClaim> _claimers = new System.Collections.Generic.List<ITapClaim>();
        private NameCardFade _fade;

        /// <summary>The name of the organ on the card, or empty while no card is showing.</summary>
        public string CurrentText => IsShowing ? _label.text : string.Empty;

        /// <summary>True while the card is on screen.</summary>
        public bool IsShowing => _card != null && _card.gameObject.activeSelf;

        private void Awake()
        {
            bool valid = true;
            if (_body == null) { Debug.LogError($"{name}: OrganNameTapInput has no SharedBody. Run Tools > DIYsect > Build Shared Scene.", this); valid = false; }
            if (_gestures == null) { Debug.LogError($"{name}: OrganNameTapInput has no PointerGestureTracker.", this); valid = false; }
            if (_library == null) { Debug.LogError($"{name}: OrganNameTapInput has no OrganInfoLibrary.", this); valid = false; }
            if (_card == null || _label == null) { Debug.LogError($"{name}: OrganNameTapInput has no card or label.", this); valid = false; }
            if (!valid)
            {
                enabled = false;
                return;
            }
            _claimers = TapClaims.Resolve(_tapClaimers, this);
            _fade = new NameCardFade(_holdSeconds, _fadeSeconds);
            _card.gameObject.SetActive(false);
        }

        // Shows a wrong assignment in the Inspector as soon as it is made.
        private void OnValidate()
        {
            TapClaims.Resolve(_tapClaimers, this);
        }

        private void OnEnable()
        {
            if (_gestures != null)
                _gestures.Tapped += HandleTap;
        }

        private void OnDisable()
        {
            if (_gestures != null)
                _gestures.Tapped -= HandleTap;
        }

        private void HandleTap(Vector2 screenPosition)
        {
            Camera view = _camera != null ? _camera : Camera.main;
            if (view != null)
                TryShowName(view.ScreenPointToRay(screenPosition));
        }

        /// <summary>Casts the ray and, if it hits an organ and no quiz round claims the tap, shows that organ's name. Returns true if it showed one.</summary>
        public bool TryShowName(Ray ray)
        {
            if (TapClaims.AnyClaims(_claimers))
                return false;
            if (!QuizOrganRaycast.TryHit(ray, _body, _maxDistance, _hitLayers, out int organIndex))
                return false;
            string organName = _body.Organs[organIndex].name;
            _library.TryGet(organName, out OrganInfoEntry entry);
            string text = OrganNameText.Build(entry, organName);
            if (text.Length == 0)
                return false;
            Show(text);
            return true;
        }

        private void Show(string text)
        {
            _label.text = text;
            _fade.Show(Time.unscaledTime);
            _card.alpha = 1f;
            _card.gameObject.SetActive(true);
        }

        private void Update()
        {
            if (!IsShowing)
                return;
            float alpha = _fade.Alpha(Time.unscaledTime);
            if (alpha <= 0f)
            {
                _card.gameObject.SetActive(false);
                return;
            }
            _card.alpha = alpha;
        }
    }
}
