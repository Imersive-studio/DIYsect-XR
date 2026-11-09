// Authority: none. Presentation only.
using DIYsect.Presentation;
using TMPro;
using UnityEngine;

namespace DIYsect.Interaction
{
    /// <summary>
    /// The minimal organ label: the organ's name with its body system in a smaller, quieter line under it.
    /// It is not a child of the organ. While shown it is placed every frame just above the organ's current top
    /// (<see cref="LabelAnchor.Above"/> on the organ's cached renderer bounds), so a small organ gets a close label,
    /// and turning the organ over in the hand never swings the label sideways. Its <see cref="Billboard"/> turns it to the
    /// camera, and it has scale 1, so its size in the world does not depend on the organ's scale.
    /// Showing and hiding run a <see cref="RevealTimeline"/> (a rise and fade in, a quick dissolve out); the per-frame
    /// code runs only while the label is visible, because the object is inactive otherwise.
    /// </summary>
    public class OrganLabel : MonoBehaviour
    {
        private const float NameCap = 0.0135f;
        private const float DescriptorCap = 0.0081f;
        private const float DescriptorDelaySeconds = 0.04f;
        private static readonly Vector2 NameBox = new Vector2(0.4f, 0.026f);
        private static readonly Vector2 DescriptorBox = new Vector2(0.4f, 0.016f);
        private static readonly Vector3 DescriptorHome = new Vector3(0f, 0.008f, 0f);
        private static readonly Vector3 NameHome = new Vector3(0f, 0.0295f, 0f);

        private readonly RevealTimeline _timeline = new RevealTimeline(MotionSpec.LabelIn, MotionSpec.LabelOut);
        private TextMeshPro _name;
        private TextMeshPro _descriptor;
        private Transform _organ;
        private CachedBounds _bounds;
        private float _gap;
        private float _applied = -1f;

        public string Text => _name != null ? _name.text : string.Empty;

        /// <summary>The second line (the body system), empty when there is none.</summary>
        public string Descriptor => _descriptor != null ? _descriptor.text : string.Empty;

        /// <summary>True from the moment it is asked to show until it is asked to hide, including the fade in.</summary>
        public bool IsShown => _timeline.Phase == RevealPhase.Entering || _timeline.Phase == RevealPhase.Shown;

        /// <summary>True while anything of it can still be seen, including the fade out.</summary>
        public bool IsVisible => _timeline.IsVisible;

        /// <summary>Builds a hidden label for the organ. <paramref name="gap"/> is the clearance above the organ's top in metres.</summary>
        public static OrganLabel Create(Transform organ, OrganCaption caption, TMP_FontAsset font, float gap)
        {
            var root = new GameObject($"Label {caption.Name}");
            var label = root.AddComponent<OrganLabel>();
            label._organ = organ;
            label._bounds = new CachedBounds(organ);
            label._gap = gap;

            label._name = LabelText.Create(root.transform, "Name", font, NameCap, GuidePalette.TextPrimary, FontStyles.Bold,
                TextAlignmentOptions.Center, NameBox, new Vector2(0.5f, 0.5f), false, onTop: true);
            label._name.text = caption.Name;
            label._name.transform.localPosition = NameHome;
            label._descriptor = LabelText.Create(root.transform, "Descriptor", font, DescriptorCap, GuidePalette.TextMuted, FontStyles.Normal,
                TextAlignmentOptions.Center, DescriptorBox, new Vector2(0.5f, 0.5f), false, onTop: true);
            label._descriptor.text = caption.System;
            label._descriptor.transform.localPosition = DescriptorHome;
            label._descriptor.gameObject.SetActive(caption.System.Length > 0);

            root.AddComponent<Billboard>();
            label._timeline.PhaseChanged += label.OnPhaseChanged;
            root.SetActive(false);
            return label;
        }

        /// <summary>Colours the name text, for a label that belongs to someone (for example a classmate's chip).</summary>
        public void SetTint(Color color)
        {
            if (_name != null)
                _name.color = color;
        }

        public void Show()
        {
            _timeline.Show();
            Place();
            Apply();
        }

        public void Hide()
        {
            _timeline.Hide();
        }

        private void OnPhaseChanged(RevealPhase phase)
        {
            bool visible = _timeline.IsVisible;
            if (gameObject.activeSelf != visible)
                gameObject.SetActive(visible);
        }

        // Polling exception: the organ moves while held and the viewer's head moves every frame; neither has a change event.
        private void LateUpdate()
        {
            if (_organ == null)
            {
                Destroy(gameObject);
                return;
            }
            _timeline.Tick(Time.deltaTime);
            if (!_timeline.IsVisible)
                return;
            Place();
            if (!Mathf.Approximately(_applied, _timeline.Amount))
                Apply();
        }

        private void Place()
        {
            transform.position = LabelAnchor.Above(_bounds.Current(), _gap);
        }

        private void Apply()
        {
            _applied = _timeline.Amount;
            bool entering = _timeline.Phase != RevealPhase.Exiting;
            SetLine(_name, NameHome, StaggeredReveal.Progress(_timeline.Amount, MotionSpec.LabelIn, 0f, entering));
            SetLine(_descriptor, DescriptorHome, StaggeredReveal.Progress(_timeline.Amount, MotionSpec.LabelIn, DescriptorDelaySeconds, entering));
        }

        private static void SetLine(TextMeshPro line, Vector3 home, float progress)
        {
            float eased = Ease.OutCubic(progress);
            line.alpha = eased;
            line.transform.localPosition = home + Vector3.down * (MotionSpec.LabelRise * (1f - eased));
        }
    }
}
