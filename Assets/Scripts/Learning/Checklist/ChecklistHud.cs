// Authority: none. Presentation only; it draws what ChecklistTracker reports.
using UnityEngine;
using UnityEngine.XR;

namespace DIYsect.Learning
{
    /// <summary>
    /// The checklist for classmates on a laptop or phone: one line in the top left corner saying how many steps
    /// are done and what comes next. It draws nothing inside a headset, where <see cref="ChecklistPanel"/> is used.
    /// Immediate-mode GUI keeps it free of scene setup, like the pointer HUD.
    /// </summary>
    [RequireComponent(typeof(ChecklistTracker))]
    public class ChecklistHud : MonoBehaviour
    {
        private ChecklistTracker _tracker;
        private GUIStyle _style;
        private readonly GUIContent _content = new GUIContent();

        private void Awake()
        {
            _tracker = GetComponent<ChecklistTracker>();
        }

        private void OnGUI()
        {
            if (Event.current.type != EventType.Repaint || XRSettings.isDeviceActive)
                return;
            EnsureStyle();

            float scale = Mathf.Clamp(Mathf.Min(Screen.width, Screen.height) / 540f, 1f, 3f);
            _style.fontSize = Mathf.RoundToInt(18f * scale);
            _content.text = ChecklistLabels.OneLine(_tracker.State);
            Vector2 size = _style.CalcSize(_content);
            var rect = new Rect(12f * scale, 12f * scale, size.x, size.y);
            _style.Draw(rect, _content, false, false, false, false);
        }

        private void EnsureStyle()
        {
            if (_style != null)
                return;
            _style = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleLeft,
                wordWrap = false,
                padding = new RectOffset(12, 12, 6, 6),
            };
            _style.normal.textColor = Color.white;
        }
    }
}
