using System;
using System.Collections.Generic;

namespace DIYsect.Classroom
{
    /// <summary>What a student's microphone is doing, as the professor's participant list shows it.</summary>
    public enum MicState
    {
        /// <summary>Muted, and not allowed to unmute.</summary>
        Muted,
        /// <summary>Allowed to speak but their microphone is off for now.</summary>
        CanSpeak,
        /// <summary>Allowed and the microphone is on: everyone hears them.</summary>
        Live,
    }

    /// <summary>One student in the professor's participant list.</summary>
    public readonly struct SpeakRow
    {
        public SpeakRow(ulong clientId, string name, int handOrder, bool allowed, bool live)
        {
            ClientId = clientId;
            Name = name ?? string.Empty;
            HandOrder = handOrder;
            Allowed = allowed;
            Live = live;
        }

        public ulong ClientId { get; }
        public string Name { get; }
        /// <summary>1 for the first hand up, 2 for the next. 0 when the hand is down.</summary>
        public int HandOrder { get; }
        public bool Allowed { get; }
        /// <summary>True when their microphone is on.</summary>
        public bool Live { get; }
    }

    /// <summary>
    /// The words, states and order of the professor's participant list (a student per row, with a hand marker, a microphone marker and an
    /// Allow / Mute button, like a video call). Pure text and rules with no Unity objects, so it is tested on its own.
    /// </summary>
    public static class SpeakRowText
    {
        public const string AllowLabel = "Allow";
        public const string MuteLabel = "Mute";
        public const string EmptyText = "No students yet";

        /// <summary>Their microphone, from whether they may speak and whether it is on. A microphone cannot be live without permission.</summary>
        public static MicState StateOf(bool allowed, bool live)
        {
            if (!allowed)
                return MicState.Muted;
            return live ? MicState.Live : MicState.CanSpeak;
        }

        /// <summary>Allow when they may not speak yet; Mute (take the permission away) when they may.</summary>
        public static string ButtonLabel(bool allowed)
        {
            return allowed ? MuteLabel : AllowLabel;
        }

        /// <summary>A short line under the name.</summary>
        public static string StatusText(MicState state, int handOrder)
        {
            switch (state)
            {
                case MicState.Live:
                    return "speaking: mic on";
                case MicState.CanSpeak:
                    return "can speak, mic off";
                default:
                    return handOrder > 0 ? "asking to speak" : "muted";
            }
        }

        /// <summary>The number beside the raised-hand marker, or empty when the hand is down.</summary>
        public static string HandText(int handOrder)
        {
            return handOrder > 0 ? $"#{handOrder}" : string.Empty;
        }

        /// <summary>Raised hands first, in the order they went up, then everyone else by name.</summary>
        public static List<SpeakRow> Sorted(IReadOnlyList<SpeakRow> rows)
        {
            var sorted = new List<SpeakRow>(rows ?? Array.Empty<SpeakRow>());
            sorted.Sort((a, b) =>
            {
                bool aUp = a.HandOrder > 0;
                bool bUp = b.HandOrder > 0;
                if (aUp != bUp)
                    return aUp ? -1 : 1;
                if (aUp)
                    return a.HandOrder.CompareTo(b.HandOrder);
                int byName = string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
                return byName != 0 ? byName : a.ClientId.CompareTo(b.ClientId);
            });
            return sorted;
        }
    }
}
