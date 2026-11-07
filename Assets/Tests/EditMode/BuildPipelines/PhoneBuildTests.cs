using System.Collections.Generic;
using DIYsect.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.XR.Management;
using UnityEngine.XR.Management;

namespace DIYsect.Build.Tests
{
    public class PhoneBuildTests
    {
        [Test]
        public void Options_ContainOnlyTheClassmateScene()
        {
            BuildPlayerOptions options = BuildAndroidClassmate.CreateOptions();

            CollectionAssert.AreEqual(new[] { "Assets/Scenes/Classmate_Desktop.unity" }, options.scenes);
            Assert.AreEqual(BuildTarget.Android, options.target);
            StringAssert.StartsWith("Builds/Android-Classmate/", options.locationPathName);
        }

        [Test]
        public void Scope_TurnsXrOffInsideAndRestoresQuestSettingsAfter()
        {
            XRGeneralSettings xr = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
            Assert.IsNotNull(xr, "The project has no Android XR settings.");
            bool initBefore = xr.InitManagerOnStart;
            var loadersBefore = new List<XRLoader>(xr.Manager.activeLoaders);
            string idBefore = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
            string nameBefore = PlayerSettings.productName;
            var before = new SerializedObject(UnityEngine.Resources.FindObjectsOfTypeAll<PlayerSettings>()[0]);
            bool overrideBefore = before.FindProperty("overrideDefaultApplicationIdentifier").boolValue;

            using (new PhoneBuildSettingsScope())
            {
                Assert.IsFalse(xr.InitManagerOnStart);
                Assert.AreEqual(0, xr.Manager.activeLoaders.Count);
                Assert.AreEqual(idBefore + ".classmate", PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android));
                Assert.AreNotEqual(nameBefore, PlayerSettings.productName);
            }

            Assert.AreEqual(initBefore, xr.InitManagerOnStart);
            CollectionAssert.AreEqual(loadersBefore, new List<XRLoader>(xr.Manager.activeLoaders));
            Assert.AreEqual(idBefore, PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android));
            Assert.AreEqual(nameBefore, PlayerSettings.productName);
            var settings = new SerializedObject(UnityEngine.Resources.FindObjectsOfTypeAll<PlayerSettings>()[0]);
            Assert.AreEqual(overrideBefore, settings.FindProperty("overrideDefaultApplicationIdentifier").boolValue);
        }

        [Test]
        public void ProducingOptions_LeavesQuestXrUntouched()
        {
            XRGeneralSettings xr = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
            bool initBefore = xr.InitManagerOnStart;
            int loadersBefore = xr.Manager.activeLoaders.Count;

            BuildAndroidClassmate.CreateOptions();

            Assert.AreEqual(initBefore, xr.InitManagerOnStart);
            Assert.AreEqual(loadersBefore, xr.Manager.activeLoaders.Count);
        }
    }
}
