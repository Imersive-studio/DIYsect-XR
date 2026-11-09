// Authority: none. Presentation only; it draws what PointerPresenter computed.
using DIYsect.Networking;
using UnityEngine;
using UnityEngine.XR;

namespace DIYsect.Classroom
{
    /// <summary>
    /// Draws the pointers on a flat screen (laptop or phone): a pulsing ring (professor) or diamond (student) on an organ that
    /// is in view, an edge arrow (triangle for the professor, chevron for the student) toward one that is off screen or
    /// behind the camera, and a label with the role and organ name. The professor's label sits above its marker and the
    /// student's below, so both can show at once. It draws nothing inside a headset. Immediate-mode GUI keeps it free of scene setup.
    /// </summary>
    [RequireComponent(typeof(PointerPresenter))]
    public class PointerHud : MonoBehaviour
    {
        private const float BaseMarker = 64f;
        private const float LabelHeight = 34f;

        private static Texture2D _ring, _diamond, _triangle, _chevron, _panel;

        private PointerPresenter _presenter;
        private GUIStyle _style;
        private readonly GUIContent _measure = new GUIContent();
        private Rect _bounds;

        private void Awake()
        {
            _presenter = GetComponent<PointerPresenter>();
        }

        private void OnGUI()
        {
            if (Event.current.type != EventType.Repaint || XRSettings.isDeviceActive)
                return;
            var entries = _presenter.Entries;
            if (entries.Count == 0)
                return;
            EnsureAssets();

            float scale = Mathf.Clamp(Mathf.Min(Screen.width, Screen.height) / 540f, 1f, 3f);
            // Read each repaint, so a rotation or a changed cutout is followed at once.
            _bounds = ScreenLayoutMath.Inset(ScreenLayoutMath.GuiSafeRect(Screen.safeArea, Screen.width, Screen.height), 4f);
            _style.fontSize = Mathf.RoundToInt(18f * scale);
            float pulse = 1f + 0.12f * (_presenter.Pulse - 0.5f) * 2f;
            foreach (PointerEntry entry in entries)
                Draw(entry, scale, pulse);
        }

        private void Draw(PointerEntry entry, float scale, float pulse)
        {
            Color color = _presenter.ColorOf(entry.Role);
            bool professor = entry.Role == PointerRole.Professor;
            PointerPlacement place = entry.Placement;
            var anchor = new Vector2(place.Position.x * Screen.width, (1f - place.Position.y) * Screen.height);
            float size = BaseMarker * scale * pulse;
            // An edge arrow stays whole inside the safe area, so a notch or a gesture bar never covers it.
            if (!place.OnScreen)
                anchor = ScreenLayoutMath.ClampInside(anchor, ScreenLayoutMath.Inset(_bounds, size * 0.5f));

            Matrix4x4 saved = GUI.matrix;
            Color savedColor = GUI.color;
            GUI.color = color;
            if (place.OnScreen)
            {
                GUI.DrawTexture(Centered(anchor, size), professor ? _ring : _diamond);
            }
            else
            {
                GUIUtility.RotateAroundPivot(-place.AngleDegrees, anchor);
                GUI.DrawTexture(Centered(anchor, size), professor ? _triangle : _chevron);
                GUI.matrix = saved;
            }
            GUI.color = savedColor;

            // The label sits above (professor) or below (student) the marker; off screen it is pulled toward the screen centre.
            _measure.text = entry.Text;
            float width = _style.CalcSize(_measure).x + 24f * scale;
            float height = LabelHeight * scale;
            Vector2 labelCentre = anchor;
            if (!place.OnScreen)
            {
                Vector2 inward = _bounds.center - anchor;
                labelCentre += inward.normalized * (size * 0.5f + height * 0.5f);
            }
            labelCentre.y += (professor ? -1f : 1f) * (size * 0.5f + height * 0.5f + 4f * scale);
            var rect = new Rect(labelCentre.x - width * 0.5f, labelCentre.y - height * 0.5f, width, height);
            rect = ScreenLayoutMath.ClampInside(rect, _bounds);

            GUI.color = new Color(0f, 0f, 0f, 0.75f);
            GUI.DrawTexture(rect, _panel);
            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.x, rect.y, 6f * scale, rect.height), _panel);
            GUI.color = savedColor;
            GUI.Label(new Rect(rect.x + 6f * scale, rect.y, rect.width - 6f * scale, rect.height), entry.Text, _style);
            GUI.matrix = saved;
        }

        private static Rect Centered(Vector2 centre, float size)
        {
            return new Rect(centre.x - size * 0.5f, centre.y - size * 0.5f, size, size);
        }

        private void EnsureAssets()
        {
            if (_ring == null) _ring = PointerShapes.Ring();
            if (_diamond == null) _diamond = PointerShapes.Diamond();
            if (_triangle == null) _triangle = PointerShapes.Triangle();
            if (_chevron == null) _chevron = PointerShapes.Chevron();
            if (_panel == null)
            {
                _panel = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
                _panel.SetPixel(0, 0, Color.white);
                _panel.Apply();
            }
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, wordWrap = false };
                _style.normal.textColor = Color.white;
            }
        }
    }
}
