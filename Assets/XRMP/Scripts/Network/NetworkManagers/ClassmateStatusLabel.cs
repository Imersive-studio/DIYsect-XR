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

        private GUIStyle _style;
        private Texture2D _background;
        private string _text = "Starting…";
        private bool _connected;
        private float _nextRefresh;

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
            GUI.Label(new Rect(safe.x + 12f, Screen.height - (safe.y + safe.height) + 12f, size.x, size.y), _text, _style);
        }

        private void OnDestroy()
        {
            if (_background != null)
                Destroy(_background);
        }
    }
}
