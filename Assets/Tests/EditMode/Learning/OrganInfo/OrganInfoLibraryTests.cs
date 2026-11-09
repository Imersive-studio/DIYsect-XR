using System.IO;
using System.Linq;
using DIYsect.Interaction;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DIYsect.Learning.Tests
{
    /// <summary>Checks the filled-in OrganInfoLibrary.asset against the organs that are really in the body.</summary>
    public class OrganInfoLibraryTests
    {
        private const string LibraryPath = "Assets/Scripts/Learning/OrganInfo/OrganInfoLibrary.asset";
        private const string OrganModelsFolder = "Assets/Models/Anatomy/Organs";
        private const string PrefabPath = "Assets/Prefabs/AnatomyBody.prefab";
        // The card is 440 x 320 units with 22 point text; this many characters per field leaves room for the name line too.
        private const int MaxFieldLength = 120;

        private static readonly string[] HeartParts = { "left_atrium", "left_ventricle", "right_atrium", "right_ventricle" };

        private OrganInfoLibrary _library;

        [SetUp]
        public void LoadLibrary()
        {
            _library = AssetDatabase.LoadAssetAtPath<OrganInfoLibrary>(LibraryPath);
            Assert.IsNotNull(_library, $"{LibraryPath} is missing.");
        }

        private static string[] OrganModelNames()
        {
            return Directory.GetFiles(OrganModelsFolder, "*.glb").Select(path => Path.GetFileNameWithoutExtension(path)).OrderBy(n => n).ToArray();
        }

        [Test]
        public void EveryOrganModelHasAnEntry()
        {
            string[] names = OrganModelNames();
            Assert.AreEqual(22, names.Length);
            foreach (string organ in names)
                Assert.IsTrue(_library.TryGet(organ, out OrganInfoEntry _), $"{organ} has no organ info entry.");
        }

        [Test]
        public void EveryOrganInTheBodyPrefabHasAnEntry()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Transform organs = prefab.transform.Find("Supine/Organs");
            Assert.AreEqual(22, organs.childCount);
            foreach (Transform organ in organs)
                Assert.IsTrue(_library.TryGet(organ.name, out OrganInfoEntry _), $"{organ.name} has no organ info entry.");
        }

        [Test]
        public void TheLibraryHasExactlyOneEntryPerOrganAndNoStrays()
        {
            string[] names = OrganModelNames();
            string[] entryNames = _library.Entries.Select(e => e.OrganName).OrderBy(n => n).ToArray();

            CollectionAssert.AreEqual(names, entryNames);
        }

        [Test]
        public void NoEntryHasAnEmptyField()
        {
            foreach (OrganInfoEntry entry in _library.Entries)
                Assert.IsTrue(entry.IsComplete, $"{entry.OrganName} has an empty field.");
        }

        [Test]
        public void EveryFunctionAndFactFitsOnTheCard()
        {
            foreach (OrganInfoEntry entry in _library.Entries)
            {
                Assert.LessOrEqual(entry.Function.Length, MaxFieldLength, $"{entry.OrganName}: function is too long.");
                Assert.LessOrEqual(entry.Fact.Length, MaxFieldLength, $"{entry.OrganName}: fact is too long.");
                Assert.IsFalse(entry.Function.Contains("<") || entry.Fact.Contains("<"), $"{entry.OrganName}: text must not contain markup.");
            }
        }

        [Test]
        public void TheDisplayNameIsTheSameWordsAsTheNameLabel()
        {
            foreach (OrganInfoEntry entry in _library.Entries)
                Assert.AreEqual(OrganNames.Display(entry.OrganName), entry.DisplayName, $"{entry.OrganName}: card title differs from the name label.");
        }

        [Test]
        public void EachHeartPartHasItsOwnEntryThatMentionsTheHeart()
        {
            foreach (string part in HeartParts)
            {
                Assert.IsTrue(_library.TryGet(part, out OrganInfoEntry entry), $"{part} has no entry.");
                string words = (entry.Function + " " + entry.Fact).ToLowerInvariant();
                Assert.IsTrue(words.Contains("heart"), $"{part} does not mention the heart.");
            }
            Assert.AreNotEqual(_library.Entries.First(e => e.OrganName == "left_atrium").Function, _library.Entries.First(e => e.OrganName == "right_atrium").Function);
        }

        [Test]
        public void AnUnknownOrganGivesNoEntry()
        {
            Assert.IsFalse(_library.TryGet("not_an_organ", out OrganInfoEntry entry));
            Assert.IsNull(entry);
            Assert.IsFalse(_library.TryGet(null, out OrganInfoEntry _));
        }

        [Test]
        public void EveryEntryHasABodySystemAndAShortRole()
        {
            foreach (OrganInfoEntry entry in _library.Entries)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(entry.System), $"{entry.OrganName}: no body system.");
                StringAssert.EndsWith("system", entry.System, $"{entry.OrganName}: the body system should read like 'Digestive system'.");
                int words = entry.Role.Split(' ').Length;
                Assert.That(words, Is.InRange(2, 5), $"{entry.OrganName}: the role should be a few words.");
            }
        }

        [Test]
        public void TheLibraryGivesLabelCaptionsAndUnknownOrgansGetNone()
        {
            Assert.IsTrue(_library.TryGetCaption("kidney_l", out OrganCaption caption));
            Assert.AreEqual("Left kidney", caption.Name);
            Assert.AreEqual("Urinary system", caption.System);
            Assert.AreEqual("Filters blood", caption.Role);
            Assert.IsFalse(_library.TryGetCaption("not_an_organ", out _));
            Assert.AreEqual("Not an organ", OrganCaptions.Lookup(_library, "not_an_organ").Name);
        }
    }
}
