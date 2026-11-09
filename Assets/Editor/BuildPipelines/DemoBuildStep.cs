using System;

namespace DIYsect.Editor
{
    /// <summary>One named build in the demo sequence. Build returns true on success.</summary>
    public sealed class DemoBuildStep
    {
        public string Name { get; }
        public string OutputPath { get; }
        public Func<bool> Build { get; }

        public DemoBuildStep(string name, string outputPath, Func<bool> build)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            OutputPath = outputPath;
            Build = build ?? throw new ArgumentNullException(nameof(build));
        }
    }
}
