using System;
using System.Collections.Generic;

namespace DIYsect.Dissection
{
    /// <summary>
    /// Which layers lie over which organ, as data. Each organ lists its covering layers from the outside in.
    /// A layer is open once the dissection reaches the stage that opens it; the organ is hidden by the first
    /// layer in its list that is still closed. The rib cage never opens in this demo. The mapping is a simplified
    /// teaching layout, not a surgical atlas: retroperitoneal organs are grouped with the bowel under the omentum.
    /// Pure logic with no Unity or network state.
    /// </summary>
    public sealed class OrganCover
    {
        private readonly struct Layer
        {
            public Layer(string phrase, DissectionStage? opensAt)
            {
                Phrase = phrase;
                OpensAt = opensAt;
            }

            public string Phrase { get; }
            public DissectionStage? OpensAt { get; }
        }

        private static readonly Layer Skin = new Layer("the skin", DissectionStage.SkinOpen);
        private static readonly Layer Wall = new Layer("the abdominal wall", DissectionStage.RectusOpen);
        private static readonly Layer Omentum = new Layer("the omentum", DissectionStage.OmentumLifted);
        private static readonly Layer RibCage = new Layer("the rib cage", null);

        private static readonly Layer[] Chest = { Skin, RibCage };
        private static readonly Layer[] UpperAbdomen = { Skin, Wall };
        private static readonly Layer[] Bowel = { Skin, Wall, Omentum };

        /// <summary>The cover of the 22 organs of the anatomy body, by object name.</summary>
        public static OrganCover Default { get; } = new OrganCover(new Dictionary<string, Layer[]>
        {
            { "superior_lobe_of_left_lung", Chest },
            { "inferior_lobe_of_left_lung", Chest },
            { "superior_lobe_of_right_lung", Chest },
            { "middle_lobe_of_right_lung", Chest },
            { "inferior_lobe_of_right_lung", Chest },
            { "left_atrium", Chest },
            { "left_ventricle", Chest },
            { "right_atrium", Chest },
            { "right_ventricle", Chest },
            { "liver", UpperAbdomen },
            { "gallbladder", UpperAbdomen },
            { "spleen", UpperAbdomen },
            { "stomach", Bowel },
            { "mucosa_of_stomach", Bowel },
            { "duodenum", Bowel },
            { "pancreas", Bowel },
            { "kidney_l", Bowel },
            { "kidney_r", Bowel },
            { "ascending_colon", Bowel },
            { "transverse_colon", Bowel },
            { "descending_colon", Bowel },
            { "sigmoid_colon", Bowel },
        });

        private readonly Dictionary<string, Layer[]> _cover;

        private OrganCover(Dictionary<string, Layer[]> cover)
        {
            _cover = cover ?? throw new ArgumentNullException(nameof(cover));
        }

        public bool Knows(string organObjectName)
        {
            return organObjectName != null && _cover.ContainsKey(organObjectName);
        }

        /// <summary>
        /// The layer still hiding the organ at this stage, as a phrase such as "the omentum", or null when the organ
        /// is in view (or unknown, which is treated as visible).
        /// </summary>
        public string HidingLayer(string organObjectName, DissectionStage stage)
        {
            if (organObjectName == null || !_cover.TryGetValue(organObjectName, out Layer[] layers))
                return null;
            foreach (Layer layer in layers)
                if (layer.OpensAt == null || stage < layer.OpensAt.Value)
                    return layer.Phrase;
            return null;
        }
    }
}
