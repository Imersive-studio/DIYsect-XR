using DIYsect.Dissection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DIYsect.Dissection.Tests
{
    public class IncisionRuleTests
    {
        private const float Tolerance = 0.04f;
        private const string BodyPrefabPath = "Assets/Prefabs/AnatomyBody.prefab";

        private static readonly Vector3 Start = new Vector3(0f, 0.18f, -0.25f);
        private static readonly Vector3 End = new Vector3(0f, 0.18f, 0.25f);

        // A belly that arches 5 cm above the straight midline in the middle, like the real skin.
        private static readonly Vector3[] Arch =
        {
            new Vector3(0f, 0.18f, -0.25f),
            new Vector3(0f, 0.23f, 0f),
            new Vector3(0f, 0.18f, 0.25f),
        };

        [Test]
        public void ABladeOnTheArchedSkinCounts()
        {
            Assert.IsTrue(IncisionRule.IsOnLine(new Vector3(0f, 0.23f, 0f), Start, End, Arch, Tolerance, out float along));
            Assert.AreEqual(0.5f, along, 0.001f);
        }

        [Test]
        public void ABladePressedIntoTheBodyCounts()
        {
            Assert.IsTrue(IncisionRule.IsOnLine(new Vector3(0f, 0.17f, 0f), Start, End, Arch, Tolerance, out _));
        }

        [Test]
        public void ABladeHighInTheAirDoesNotCount()
        {
            Assert.IsFalse(IncisionRule.IsOnLine(new Vector3(0f, 0.23f + Tolerance + 0.01f, 0f), Start, End, Arch, Tolerance, out _));
        }

        [Test]
        public void ABladeOffToTheSideDoesNotCount()
        {
            Assert.IsFalse(IncisionRule.IsOnLine(new Vector3(Tolerance + 0.01f, 0.23f, 0f), Start, End, Arch, Tolerance, out _));
        }

        [Test]
        public void WithoutMeasuredSkinTheStraightMidlineIsUsed()
        {
            Assert.IsTrue(IncisionRule.IsOnLine(new Vector3(0f, 0.18f + Tolerance - 0.005f, 0f), Start, End, null, Tolerance, out _));
            Assert.IsFalse(IncisionRule.IsOnLine(new Vector3(0f, 0.23f, 0f), Start, End, null, Tolerance, out _));
        }

        [Test]
        public void SurfaceHeightFollowsTheSkinBetweenPoints()
        {
            Assert.AreEqual(0.205f, IncisionRule.SurfaceHeight(Arch, 0.25f, 0f), 0.0001f);
            Assert.AreEqual(0.18f, IncisionRule.SurfaceHeight(Arch, 1f, 0f), 0.0001f);
            Assert.AreEqual(0.7f, IncisionRule.SurfaceHeight(new Vector3[0], 0.5f, 0.7f), 0.0001f);
        }

        [Test]
        public void HighestRiseIsTheTopOfTheArch()
        {
            Assert.AreEqual(0.05f, IncisionRule.HighestRise(Arch, Start, End), 0.0001f);
        }

        [Test]
        public void TheBodysCutZoneHoldsTheWholeGlowingLine()
        {
            var body = AssetDatabase.LoadAssetAtPath<GameObject>(BodyPrefabPath);
            Assert.IsNotNull(body, $"{BodyPrefabPath} is missing.");
            var zone = body.GetComponentInChildren<IncisionTrigger>(true);
            Assert.IsNotNull(zone, "The body has no IncisionTrigger. Run Tools > DIYsect > Add Body Dissection.");
            var box = zone.GetComponent<BoxCollider>();
            var line = body.GetComponentInChildren<IncisionLinePresenter>(true);
            SerializedProperty path = new SerializedObject(line).FindProperty("_path");
            Assert.Greater(path.arraySize, 1, "The glowing line has no points.");

            for (int i = 0; i < path.arraySize; i++)
            {
                Vector3 bodyPoint = path.GetArrayElementAtIndex(i).vector3Value;
                Vector3 local = zone.transform.InverseTransformPoint(body.transform.TransformPoint(bodyPoint)) - box.center;
                Vector3 half = box.size * 0.5f;
                Assert.IsTrue(Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y && Mathf.Abs(local.z) <= half.z + 0.001f,
                    $"Point {i} of the glowing line ({bodyPoint}) is outside the cut zone. Run Tools > DIYsect > Add Body Dissection.");
            }
        }
    }
}
