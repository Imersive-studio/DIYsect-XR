using UnityEngine;

namespace DIYsect.Interaction
{
    /// <summary>A handheld instrument. Other systems (for example a depth control) address a tool through this.</summary>
    public interface ITool
    {
        string ToolName { get; }

        /// <summary>The working point of the tool, in world space (the blade tip of a scalpel).</summary>
        Transform Tip { get; }
    }
}
