// The learning layer: organ info cards, the dissection checklist, the quiz and sound cues.
// Everything for it lives in this folder and its own assembly, so it never edits another team member's files.
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DIYsect.Learning.EditTests")]
[assembly: InternalsVisibleTo("DIYsect.Learning.PlayTests")]

namespace DIYsect.Learning
{
    /// <summary>Names this assembly in one place, so tests and editor steps can check it is loaded.</summary>
    public static class LearningAssembly
    {
        public const string Name = "DIYsect.Learning";
    }
}
