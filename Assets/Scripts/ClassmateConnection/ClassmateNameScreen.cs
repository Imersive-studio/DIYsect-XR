// Authority: none. Local UI only; the name is this device's own and is sent with the player like any other player name.
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XRMultiplayer;

namespace DIYsect.ClassmateConnection
{
    /// <summary>
    /// The first page a classmate sees on a phone: it asks for their name, which the professor and the class see. It comes before the
    /// Join page, and the Join page is covered until they press Continue. The name is remembered for next time and typed into the
    /// box already. It only appears where the Join page does (a phone waiting for a press), not in the editor or in scenes that join on
    /// their own. An invite link starts the join without the page, so such a classmate joins under the name they used last, or the default.
    /// </summary>
    public class ClassmateNameScreen : MonoBehaviour
    {
        public const string PrefsKey = "diysect.playerName";

        [SerializeField, Tooltip("The page that is shown and hidden.")] private GameObject _root;
        [SerializeField] private TMP_InputField _field;
        [SerializeField] private Button _continue;
        [SerializeField, Tooltip("Says what is wrong with the typed name.")] private TMP_Text _message;
        [SerializeField, Tooltip("Provides the joiner. Leave a ClassmateJoinAdapter here.")] private MonoBehaviour _joiner;

        private IClassmateJoiner _service;
        private bool _done;
        private bool _filled;

        /// <summary>True while the page is on screen.</summary>
        public bool IsShowing => _root != null && _root.activeSelf;

        /// <summary>True once the classmate has pressed Continue with a good name.</summary>
        public bool Done => _done;

        /// <summary>The name that was sent, or empty before Continue.</summary>
        public string SubmittedName { get; private set; } = string.Empty;

        private void Awake()
        {
            if (_root == null || _field == null || _continue == null || _message == null || (_joiner == null && _service == null))
            {
                Debug.LogError($"{name}: ClassmateNameScreen is missing a part. Run Tools > DIYsect > Build Shared Scene.", this);
                enabled = false;
                if (_root != null)
                    _root.SetActive(false);
                return;
            }
            if (_service == null)
                _service = _joiner as IClassmateJoiner;
            if (_service == null)
            {
                Debug.LogError($"{name}: {_joiner.GetType().Name} does not implement IClassmateJoiner.", this);
                enabled = false;
                _root.SetActive(false);
                return;
            }
            _root.SetActive(false);
            // Someone who joins by link never sees the page, so they use the name they used last.
            if (PlayerPrefs.HasKey(PrefsKey) && NameRules.TryClean(PlayerPrefs.GetString(PrefsKey), out string saved))
                XRINetworkGameManager.LocalPlayerName.Value = saved;
        }

        /// <summary>Replaces the joiner. Used by tests, so no lobby service is needed.</summary>
        public void SetJoiner(IClassmateJoiner service)
        {
            _service = service ?? throw new System.ArgumentNullException(nameof(service));
        }

        private void OnEnable()
        {
            if (_continue != null)
                _continue.onClick.AddListener(Submit);
        }

        private void OnDisable()
        {
            // The button can be destroyed first when the scene unloads.
            if (_continue != null)
                _continue.onClick.RemoveListener(Submit);
        }

        private void Update()
        {
            Refresh();
        }

        /// <summary>The Continue button: send the typed name and go on to the Join page.</summary>
        public void Submit()
        {
            if (!NameRules.TryClean(_field.text, out string cleaned))
            {
                _message.text = NameRules.EmptyText;
                return;
            }
            PlayerPrefs.SetString(PrefsKey, cleaned);
            PlayerPrefs.Save();
            // Setting the local player name sends it to the room, also when the classmate is already in it.
            XRINetworkGameManager.LocalPlayerName.Value = cleaned;
            SubmittedName = cleaned;
            _done = true;
            Refresh();
        }

        private void Refresh()
        {
            bool show = !_done && _service.WaitingForPress && !_service.Connected;
            if (_root.activeSelf != show)
                _root.SetActive(show);
            if (show && !_filled)
            {
                _filled = true;
                if (PlayerPrefs.HasKey(PrefsKey))
                    _field.text = PlayerPrefs.GetString(PrefsKey);
            }
            if (show && _message.text.Length > 0 && _field.text.Length > 0 && NameRules.TryClean(_field.text, out _))
                _message.text = string.Empty;
        }
    }
}
