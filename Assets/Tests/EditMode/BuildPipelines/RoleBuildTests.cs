using DIYsect.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;

namespace DIYsect.Build.Tests
{
    public class RoleBuildTests
    {
        [Test]
        public void ProfessorOptions_ContainOnlyTheProfessorScene()
        {
            BuildPlayerOptions options = BuildAndroidRole.CreateProfessorOptions();

            CollectionAssert.AreEqual(new[] { "Assets/Scenes/Professor_MR.unity" }, options.scenes);
            Assert.AreEqual(BuildTarget.Android, options.target);
            StringAssert.StartsWith("Builds/Android-Professor/", options.locationPathName);
        }

        [Test]
        public void StudentOptions_ContainTheSameSceneAsTheProfessorAndPhone()
        {
            BuildPlayerOptions options = BuildAndroidRole.CreateStudentOptions();

            CollectionAssert.AreEqual(BuildAndroidRole.CreateProfessorOptions().scenes, options.scenes);
            CollectionAssert.AreEqual(BuildAndroidClassmate.CreateOptions().scenes, options.scenes);
            StringAssert.StartsWith("Builds/Android-Student/", options.locationPathName);
        }

        [Test]
        public void RoleBuilds_AreNotDevelopmentBuilds_LikeThePhoneBuild()
        {
            Assert.AreEqual(BuildOptions.None, BuildAndroidRole.CreateProfessorOptions().options & BuildOptions.Development);
            Assert.AreEqual(BuildOptions.None, BuildAndroidRole.CreateStudentOptions().options & BuildOptions.Development);
            Assert.AreEqual(BuildOptions.None, BuildAndroidClassmate.CreateOptions().options & BuildOptions.Development);
        }

        [Test]
        public void Scope_GivesTheRoleItsOwnIdAndNameInsideAndRestoresThemAfter()
        {
            string idBefore = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
            string nameBefore = PlayerSettings.productName;
            var before = new SerializedObject(UnityEngine.Resources.FindObjectsOfTypeAll<PlayerSettings>()[0]);
            bool overrideBefore = before.FindProperty("overrideDefaultApplicationIdentifier").boolValue;

            using (new RoleBuildSettingsScope(".professor", "DIYsect Professor"))
            {
                Assert.AreEqual(idBefore + ".professor", PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android));
                Assert.AreEqual("DIYsect Professor", PlayerSettings.productName);
            }

            Assert.AreEqual(idBefore, PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android));
            Assert.AreEqual(nameBefore, PlayerSettings.productName);
            var after = new SerializedObject(UnityEngine.Resources.FindObjectsOfTypeAll<PlayerSettings>()[0]);
            Assert.AreEqual(overrideBefore, after.FindProperty("overrideDefaultApplicationIdentifier").boolValue);
        }

        [Test]
        public void Scope_RejectsASuffixWithoutADot()
        {
            Assert.Throws<System.ArgumentException>(() => new RoleBuildSettingsScope("professor", "x"));
        }
    }
}
