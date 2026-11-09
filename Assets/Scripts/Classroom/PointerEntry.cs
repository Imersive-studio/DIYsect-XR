using DIYsect.Networking;

namespace DIYsect.Classroom
{
    /// <summary>One pointer as a classmate sees it: who, which organ, the label words, and where it is on this screen.</summary>
    public class PointerEntry
    {
        public PointerEntry(PointerRole role, int organIndex, string organName, string hidingLayer)
        {
            Role = role;
            OrganIndex = organIndex;
            OrganName = organName;
            HidingLayer = hidingLayer;
            Text = PointerLabelText.Build(role, organName, hidingLayer);
        }

        public PointerRole Role { get; }
        public int OrganIndex { get; }
        public string OrganName { get; }

        /// <summary>The layer still hiding the organ ("the omentum"), or null when it is in view.</summary>
        public string HidingLayer { get; }

        public string Text { get; }

        /// <summary>Updated every frame by the presenter while a view camera exists.</summary>
        public PointerPlacement Placement { get; set; }
    }
}
