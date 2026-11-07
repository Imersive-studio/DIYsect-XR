// Authority: none. Presentation only.
using UnityEngine;

namespace DIYsect.Interaction
{
    /// <summary>
    /// A name tag above one organ. It is a child of the organ, so it follows the organ while it is held
    /// with no per-frame code of its own. The <see cref="Billboard"/> on it turns it toward the viewer and only
    /// runs while the tag is shown.
    /// </summary>
    public class OrganLabel : MonoBehaviour
    {
        private TextMesh _text;

        public string Text => _text != null ? _text.text : string.Empty;

        public bool IsShown => gameObject.activeSelf;

        /// <summary>Builds a hidden label as a child of the organ, <paramref name="height"/> metres above <paramref name="centre"/> (organ space).</summary>
        public static OrganLabel Create(Transform organ, Vector3 centre, string text, float height, float characterSize)
        {
            var labelObject = new GameObject($"Label {text}");
            labelObject.transform.SetParent(organ, false);
            labelObject.transform.localPosition = centre;
            labelObject.transform.position += Vector3.up * height;
            // Keep the label the same world size whatever the organ scale.
            Vector3 scale = organ.lossyScale;
            labelObject.transform.localScale = new Vector3(1f / scale.x, 1f / scale.y, 1f / scale.z);

            var mesh = labelObject.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.anchor = TextAnchor.LowerCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.characterSize = characterSize;
            mesh.fontSize = 64;
            mesh.color = Color.white;
            labelObject.AddComponent<Billboard>();

            var label = labelObject.AddComponent<OrganLabel>();
            label._text = mesh;
            labelObject.SetActive(false);
            return label;
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
