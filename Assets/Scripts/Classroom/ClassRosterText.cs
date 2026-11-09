using System;
using System.Collections.Generic;
using System.Text;

namespace DIYsect.Classroom
{
    /// <summary>What a student is using to take part.</summary>
    public enum RosterKind
    {
        /// <summary>A headset, such as a student in the VR lecture hall.</summary>
        Headset,
        /// <summary>A phone or laptop.</summary>
        Screen,
    }

    /// <summary>One student as the professor's roster shows them.</summary>
    public readonly struct RosterEntry
    {
        public RosterEntry(ulong clientId, string name, RosterKind kind, int questions, int handOrder = 0)
        {
            ClientId = clientId;
            Name = name ?? string.Empty;
            Kind = kind;
            Questions = questions;
            HandOrder = handOrder;
        }

        public ulong ClientId { get; }
        public string Name { get; }
        public RosterKind Kind { get; }
        public int Questions { get; }
        /// <summary>1 for the first hand raised, 2 for the next, and so on. 0 when the student has no hand up.</summary>
        public int HandOrder { get; }
        public bool HandUp => HandOrder > 0;
    }

    /// <summary>
    /// Turns the connected students into the text of the professor's roster panel. Pure text work with no network or
    /// scene access, so it is tested on its own.
    /// </summary>
    public static class ClassRosterText
    {
        /// <summary>Students listed before the rest are summarised.</summary>
        public const int MaxLines = 8;
        /// <summary>Longest name shown in full.</summary>
        public const int MaxNameLength = 16;

        public static string Format(IReadOnlyList<RosterEntry> entries)
        {
            return Format(entries, null, null);
        }

        /// <summary>
        /// The roster with the room's invite above it, so the professor can read the code out or put the link on a slide.
        /// An empty code or link leaves that line out.
        /// </summary>
        public static string Format(IReadOnlyList<RosterEntry> entries, string joinCode, string joinLink)
        {
            string invite = InviteLines(joinCode, joinLink);
            string roster = FormatRoster(entries);
            return invite.Length == 0 ? roster : invite + "\n" + roster;
        }

        /// <summary>The invite lines alone (join code, then link). Empty when there is neither.</summary>
        public static string InviteLines(string joinCode, string joinLink)
        {
            string code = Clean(joinCode);
            string link = Clean(joinLink);
            var lines = new StringBuilder();
            if (code.Length > 0)
                lines.Append("Join code: ").Append(code);
            if (link.Length > 0)
                lines.Append(lines.Length > 0 ? "\n" : string.Empty).Append(link);
            return lines.ToString();
        }

        private static string FormatRoster(IReadOnlyList<RosterEntry> entries)
        {
            int count = entries?.Count ?? 0;
            var text = new StringBuilder();
            text.Append("Class: ").Append(Summary(entries));
            if (count == 0)
                return text.ToString();

            List<RosterEntry> sorted = Sorted(entries);

            int shown = Math.Min(sorted.Count, MaxLines);
            for (int i = 0; i < shown; i++)
            {
                RosterEntry entry = sorted[i];
                text.Append('\n').Append(Label(entry, sorted));
                if (entry.HandUp)
                    text.Append("  ").Append(HandLabel(entry.HandOrder));
                text.Append("  ").Append(KindLabel(entry.Kind));
                if (entry.Questions > 0)
                    text.Append("  ").Append(QuestionLabel(entry.Questions));
            }
            if (sorted.Count > shown)
                text.Append('\n').Append($"+{sorted.Count - shown} more");
            return text.ToString();
        }

        /// <summary>Raised hands first, in the order they went up; then everyone else by name.</summary>
        public static List<RosterEntry> Sorted(IReadOnlyList<RosterEntry> entries)
        {
            var sorted = new List<RosterEntry>(entries ?? Array.Empty<RosterEntry>());
            sorted.Sort((a, b) =>
            {
                if (a.HandUp != b.HandUp)
                    return a.HandUp ? -1 : 1;
                if (a.HandUp && a.HandOrder != b.HandOrder)
                    return a.HandOrder.CompareTo(b.HandOrder);
                int byName = string.Compare(Clean(a.Name), Clean(b.Name), StringComparison.OrdinalIgnoreCase);
                return byName != 0 ? byName : a.ClientId.CompareTo(b.ClientId);
            });
            return sorted;
        }

        /// <summary>The class count line, such as "3 students, 1 hand up".</summary>
        public static string Summary(IReadOnlyList<RosterEntry> entries)
        {
            int count = entries?.Count ?? 0;
            if (count == 0)
                return "no students yet";
            var text = new StringBuilder(count == 1 ? "1 student" : $"{count} students");
            int hands = 0;
            foreach (RosterEntry entry in entries)
                if (entry.HandUp)
                    hands++;
            if (hands > 0)
                text.Append(hands == 1 ? ", 1 hand up" : $", {hands} hands up");
            return text.ToString();
        }

        /// <summary>"1 question" or "3 questions".</summary>
        public static string QuestionLabel(int questions) => questions == 1 ? "1 question" : $"{questions} questions";

        /// <summary>The muted detail after a name: the device, then the pinned questions when there are any.</summary>
        public static string DetailText(RosterEntry entry)
        {
            string kind = KindLabel(entry.Kind);
            return entry.Questions > 0 ? kind + "  " + QuestionLabel(entry.Questions) : kind;
        }

        /// <summary>The name shown for a student. A name that two students share gets the client id added.</summary>
        public static string Label(RosterEntry entry, IReadOnlyList<RosterEntry> all)
        {
            string name = Clean(entry.Name);
            if (name.Length == 0)
                name = "Student";
            if (name.Length > MaxNameLength)
                name = name.Substring(0, MaxNameLength - 1) + "…";
            int sameName = 0;
            foreach (RosterEntry other in all)
                if (string.Equals(Clean(other.Name), Clean(entry.Name), StringComparison.OrdinalIgnoreCase))
                    sameName++;
            return sameName > 1 ? $"{name} #{entry.ClientId}" : name;
        }

        /// <summary>The marker next to a name whose hand is up. Plain letters, because the headset's text font has no hand symbol.</summary>
        public static string HandLabel(int handOrder) => $"[HAND {handOrder}]";

        public static string KindLabel(RosterKind kind) => kind == RosterKind.Headset ? "headset" : "phone/laptop";

        // Names come from other players, and TextMeshPro reads <b>-style tags, so angle brackets and line breaks go.
        private static string Clean(string name)
        {
            if (string.IsNullOrEmpty(name))
                return string.Empty;
            return name.Replace("<", string.Empty).Replace(">", string.Empty).Replace("\n", " ").Replace("\r", " ").Trim();
        }
    }
}
