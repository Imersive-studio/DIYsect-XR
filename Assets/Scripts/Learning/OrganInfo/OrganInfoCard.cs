// Authority: none. Presentation only.
using DIYsect.Networking;
using UnityEngine;
using UnityEngine.UI;

namespace DIYsect.Learning
{
    /// <summary>
    /// One small world-space panel with an organ's name, function and fact. It floats a fixed height above the middle of
    /// the organ and turns to face the viewer. It is not a child of the organ, so its size is the same whatever the organ
    /// or body scale. It is one Canvas with one dark background and one text field, so it costs very little on a headset.
    /// Polling exception: the organ moves while held and the viewer's head moves every frame, and neither has a change
    /// event. The per-frame code runs only while the card is shown, because a hidden card is an inactive object.
    /// </summary>
    public class OrganInfoCard : MonoBehaviour
    {
        // The panel is 440 x 320 canvas units; one unit is half a millimetre, so it is 22 cm wide and 16 cm tall.
        private const float PanelWidth = 440f;
        private const float PanelHeight = 320f;
        private const float MetresPerUnit = 0.0005f;
        private const float Padding = 16f;
        private const int FontSize = 22;

        private Text _text;
        private Transform _organ;
        private ISnappable _snappable;
        private float _height;
        private Camera _camera;

        /// <summary>The words on the card.</summary>
        public string Content => _text != null ? _text.text : string.Empty;

        public bool IsShown => gameObject.activeSelf;

        /// <summary>
        /// Builds a hidden card for the organ. <paramref name="height"/> is the distance in metres from the middle of the
        /// organ to the bottom edge of the card.
        /// </summary>
        public static OrganInfoCard Create(Transform organ, ISnappable snappable, string text, float height, Font font)
        {
            var root = new GameObject($"Info Card {organ.name}", typeof(RectTransform));
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rect = (RectTransform)root.transform;
            rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.localScale = Vector3.one * MetresPerUnit;

            var background = new GameObject("Background", typeof(RectTransform));
            background.transform.SetParent(root.transform, false);
            Stretch((RectTransform)background.transform, 0f);
            var image = background.AddComponent<Image>();
            image.color = new Color(0.05f, 0.08f, 0.12f, 0.9f);
            image.raycastTarget = false;

            var textObject = new GameObject("Text", typeof(RectTransform));
            textObject.transform.SetParent(root.transform, false);
            Stretch((RectTransform)textObject.transform, Padding);
            var label = textObject.AddComponent<Text>();
            label.font = font;
            label.fontSize = FontSize;
            label.color = Color.white;
            label.alignment = TextAnchor.UpperLeft;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.supportRichText = true;
            label.raycastTarget = false;
            label.text = text;

            var card = root.AddComponent<OrganInfoCard>();
            card._text = label;
            card._organ = organ;
            card._snappable = snappable;
            card._height = height;
            root.SetActive(false);
            return card;
        }

        public void Show()
        {
            gameObject.SetActive(true);
            Follow();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            Follow();
        }

        // Puts the card above the middle of the organ and turns it to face the camera, keeping it upright.
        private void Follow()
        {
            if (_organ == null || _snappable == null)
                return;
            Vector3 centre = _snappable.CurrentCentrePose.position;
            if (_organ.parent != null)
                centre = _organ.parent.TransformPoint(centre);
            transform.position = centre + Vector3.up * _height;

            if (_camera == null)
                _camera = Camera.main;
            if (_camera == null)
                return;
            Vector3 away = transform.position - _camera.transform.position;
            if (away.sqrMagnitude > 1e-6f)
                transform.rotation = Quaternion.LookRotation(away, Vector3.up);
        }

        private static void Stretch(RectTransform rect, float padding)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }
    }
}
