// Run Tools > DIYsect > Apply Realistic Look after re-exporting the skin, drapes or lamp.
// Batch: Unity -batchmode -quit -projectPath . -executeMethod DIYsect.Editor.ApplyRealisticLook.Run
using UnityEditor;

namespace DIYsect.Editor
{
    /// <summary>
    /// One step for the whole look: re-measures the incision and hinges on the current skin flaps (and adds
    /// the cut line), adds the drapes and tissue materials to the body prefab, puts the lecture hall around the student's table,
    /// hangs the operating light
    /// in the VR and desktop scenes, moves the instruments off the draped table onto a stand,
    /// and gives the scalpel and forceps their real models. Each part is its own idempotent step and can also be run alone.
    /// </summary>
    public static class ApplyRealisticLook
    {
        [MenuItem("Tools/DIYsect/Apply Realistic Look")]
        public static void Run()
        {
            AnatomyBodyDissection.ApplyToPrefab();
            AnatomyBodyLook.ApplyToPrefab();
            AddLectureHall.Apply();
            AddOperatingLight.Apply();
            ArrangeInstrumentStand.Apply();
            UseRealInstruments.Apply();
            AssetDatabase.SaveAssets();
        }
    }
}
