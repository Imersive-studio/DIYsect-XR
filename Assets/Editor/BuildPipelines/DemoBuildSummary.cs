using System;
using System.Collections.Generic;
using System.Text;

namespace DIYsect.Editor
{
    /// <summary>Builds the text of Builds/DemoBuilds.txt: which commit the APKs came from, when, and where they are.</summary>
    public static class DemoBuildSummary
    {
        /// <summary>One APK line. SizeBytes is negative when the file is missing.</summary>
        public readonly struct Entry
        {
            public string Name { get; }
            public string Path { get; }
            public long SizeBytes { get; }

            public Entry(string name, string path, long sizeBytes)
            {
                Name = name;
                Path = path;
                SizeBytes = sizeBytes;
            }
        }

        public static string Format(string commit, DateTime date, IReadOnlyList<Entry> entries)
        {
            var text = new StringBuilder();
            text.AppendLine($"Commit: {(string.IsNullOrWhiteSpace(commit) ? "unknown" : commit.Trim())}");
            text.AppendLine($"Date: {date:yyyy-MM-dd HH:mm}");
            foreach (Entry entry in entries)
            {
                string size = entry.SizeBytes < 0 ? "missing" : $"{entry.SizeBytes / (1024.0 * 1024.0):F1} MB";
                text.AppendLine($"{entry.Name}: {entry.Path} ({size})");
            }
            return text.ToString();
        }
    }
}
