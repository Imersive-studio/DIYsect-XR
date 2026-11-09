// Under test: which layer hides which organ at each dissection stage, and that every organ of the body has a row.
using DIYsect.Dissection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DIYsect.Classroom.Tests
{
    public class OrganCoverTests
    {
        private static readonly OrganCover Cover = OrganCover.Default;

        [Test]
        public void EveryOrganOfTheBodyHasACoverRow()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/AnatomyBody.prefab");
            Transform organs = prefab.transform.Find("Supine/Organs");
            Assert.AreEqual(22, organs.childCount);
            foreach (Transform organ in organs)
                Assert.IsTrue(Cover.Knows(organ.name), $"{organ.name} has no row in OrganCover.");
        }

        [TestCase(DissectionStage.Intact)]
        [TestCase(DissectionStage.Incised)]
        public void EveryOrganIsUnderTheSkinUntilItOpens(DissectionStage stage)
        {
            Assert.AreEqual("the skin", Cover.HidingLayer("liver", stage));
            Assert.AreEqual("the skin", Cover.HidingLayer("stomach", stage));
            Assert.AreEqual("the skin", Cover.HidingLayer("left_ventricle", stage));
        }

        [Test]
        public void TheAbdominalWallHidesTheLiverUntilTheRectusIsOpen()
        {
            Assert.AreEqual("the abdominal wall", Cover.HidingLayer("liver", DissectionStage.SkinOpen));
            Assert.IsNull(Cover.HidingLayer("liver", DissectionStage.RectusOpen));
        }

        [Test]
        public void TheOmentumHidesTheBowelUntilItIsLifted()
        {
            Assert.AreEqual("the omentum", Cover.HidingLayer("transverse_colon", DissectionStage.RectusOpen));
            Assert.AreEqual("the omentum", Cover.HidingLayer("kidney_l", DissectionStage.RectusOpen));
            Assert.IsNull(Cover.HidingLayer("transverse_colon", DissectionStage.OmentumLifted));
        }

        [Test]
        public void TheChestStaysUnderTheRibCage()
        {
            Assert.AreEqual("the rib cage", Cover.HidingLayer("right_atrium", DissectionStage.OmentumLifted));
        }

        [Test]
        public void UnknownOrganOrNullIsTreatedAsVisible()
        {
            Assert.IsNull(Cover.HidingLayer("not_an_organ", DissectionStage.Intact));
            Assert.IsNull(Cover.HidingLayer(null, DissectionStage.Intact));
        }
    }
}
