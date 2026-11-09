// Authority: none. Reads the connection state for display only.
using UnityEngine;

namespace XRMultiplayer
{
    /// <summary>
    /// A line of text in the corner of the classmate's screen that says whether they are in the professor's room:
    /// signing in, looking for the room, joining, or connected with the number of people in it. The phone has no menu,
    /// so without this a classmate cannot tell whether anything is happening.
    /// </summary>
    public class ClassmateStatusLabel : MonoBehaviour
    {
        [SerializeField, Range(0.015f, 0.06f), Tooltip("Text height as a fraction of the screen's short side.")]
        private float _textSize = 0.032f;

        private ClassmateAutoJoin _join;
        private string _typedCode = string.Empty;
        private string _codeMessage = string.Empty;
        private GUIStyle _style;
        private Texture2D _background;
        private string _text = "Starting…";
        private bool _connected;
        private float _nextRefresh;

        private void Awake()
        {
            _join = GetComponent<ClassmateAutoJoin>();
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextRefresh)
                return;
            _nextRefresh = Time.unscaledTime + 0.5f;
            _text = BuildText(out _connected);
        }

        private static string BuildText(out bool connected)
        {
            connected = XRINetworkGameManager.Connected.Value;
            if (connected)
            {
                int people = FindObjectsByType<XRINetworkPlayer>(FindObjectsSortMode.None).Length;
                string room = string.IsNullOrEmpty(XRINetworkGameManager.ConnectedRoomName.Value) ? "the room" : XRINetworkGameManager.ConnectedRoomName.Value;
                return people > 0 ? $"Connected to {room} · {people} in the room" : $"Connected to {room}";
            }

            switch (XRINetworkGameManager.CurrentConnectionState.Value)
            {
                case XRINetworkGameManager.ConnectionState.Authenticating:
                    return "Signing in…";
                case XRINetworkGameManager.ConnectionState.Authenticated:
                    return "Looking for the professor's room…";
                case XRINetworkGameManager.ConnectionState.Connecting:
                    return "Joining the room…";
                default:
                    return "Not connected";
            }
        }

        private void OnGUI()
        {
            if (_style == null)
            {
                _background = new Texture2D(1, 1);
                _background.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.55f));
                _background.Apply();
                _style = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    wordWrap = false,
                    normal = { textColor = Color.white, background = _background },
                    padding = new RectOffset(14, 14, 6, 6),
                };
            }

            int shortSide = Mathf.Min(Screen.width, Screen.height);
            _style.fontSize = Mathf.Max(12, Mathf.RoundToInt(shortSide * _textSize));
            _style.normal.textColor = _connected ? new Color(0.6f, 1f, 0.7f) : Color.white;

            Rect safe = Screen.safeArea;
            Vector2 size = _style.CalcSize(new GUIContent(_text));
            float left = safe.x + 12f;
            float top = Screen.height - (safe.y + safe.height) + 12f;
            GUI.Label(new Rect(left, top, size.x, size.y), _text, _style);
            if (!_connected)
                DrawCodeEntry(left, top + size.y + 10f);
        }

        // A box for the room code, for a classmate who was given the code instead of the link. Shown while not in a room.
        private void DrawCodeEntry(float left, float top)
        {
            if (_join == null)
                return;
            float height = _style.CalcSize(new GUIContent("KHC8QH")).y;
            float fieldWidth = height * 4.2f;
            var field = new GUIStyle(GUI.skin.textField) { fontSize = _style.fontSize, alignment = TextAnchor.MiddleCenter };
            _typedCode = GUI.TextField(new Rect(left, top, fieldWidth, height), _typedCode, RoomInvite.CodeLength + 2, field);
            var button = new GUIStyle(GUI.skin.button) { fontSize = _style.fontSize };
            if (GUI.Button(new Rect(left + fieldWidth + 8f, top, height * 2.4f, height), "Join", button))
            {
                _codeMessage = _join.JoinWithCode(_typedCode) ? string.Empty : $"A room code has {RoomInvite.CodeLength} letters and digits.";
            }
            string message = _join.CodeRejected ? "No room found with that code." : _codeMessage;
            if (message.Length > 0)
                GUI.Label(new Rect(left, top + height + 6f, Screen.width - left * 2f, height), message, _style);
        }

        private void OnDestroy()
        {
            if (_background != null)
                Destroy(_background);
        }
    }
}
