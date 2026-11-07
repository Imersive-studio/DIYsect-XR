// Run Tools > DIYsect > Apply Realistic Look after re-exporting the skin, drapes or lamp.
// Batch: Unity -batchmode -quit -projectPath . -executeMethod DIYsect.Editor.ApplyRealisticLook.Run
using UnityEditor;

namespace DIYsect.Editor
{
    /// <summary>
    /// One step for the whole look: re-measures the incision and hinges on the current skin flaps (and adds
    /// the cut line), adds the drapes and tissue materials to the body prefab, hangs the operating light
    /// in the VR and desktop scenes, and moves the instruments off the draped table onto a stand. Each part is its own idempotent step and can also be run alone.
    /// </summary>
    public static class ApplyRealisticLook
    {
        [MenuItem("Tools/DIYsect/Apply Realistic Look")]
        public static void Run()
        {
            AnatomyBodyDissection.ApplyToPrefab();
            AnatomyBodyLook.ApplyToPrefab();
            AddOperatingLight.Apply();
            ArrangeInstrumentStand.Apply();
            AssetDatabase.SaveAssets();
        }
    }
}
