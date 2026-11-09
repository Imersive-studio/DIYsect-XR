using UnityEngine;

namespace DIYsect.Interaction
{
    /// <summary>
    /// A source of organ captions that the Interaction code can read without knowing where the text is stored.
    /// The Learning assembly provides the real one (its organ info library), so Interaction never has to reference Learning.
    /// </summary>
    public abstract class OrganCaptions : ScriptableObject
    {
        /// <summary>Gives the caption for an organ's GameObject name (for example "kidney_l"), or false when it is unknown.</summary>
        public abstract bool TryGetCaption(string organObjectName, out OrganCaption caption);

        /// <summary>
        /// The caption for the organ. When <paramref name="captions"/> is missing or does not know the organ, the name is
        /// worked out from the object name by <see cref="OrganNames"/> and there is no second line.
        /// </summary>
        public static OrganCaption Lookup(OrganCaptions captions, string organObjectName)
        {
            if (captions != null && captions.TryGetCaption(organObjectName, out OrganCaption caption))
                return caption;
            return new OrganCaption(OrganNames.Display(organObjectName), string.Empty, string.Empty);
        }
    }
}
