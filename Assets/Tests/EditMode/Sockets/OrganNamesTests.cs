using System.Collections.Generic;
using System.IO;
using DIYsect.Interaction;
using NUnit.Framework;
using UnityEngine;

namespace DIYsect.Interaction.Tests
{
    public class OrganNamesTests
    {
        private const string ManifestPath = "Assets/Models/Anatomy/manifest.json";

        // The field names must match the JSON keys, which is how JsonUtility finds them.
#pragma warning disable 0649
        [System.Serializable]
        private class Entry
        {
            [SerializeField] private string file;
            [SerializeField] private string original_name;
            [SerializeField] private string layer;

            public string File => file;
            public string OriginalName => original_name;
            public string Layer => layer;
        }

        [System.Serializable]
        private class Manifest
        {
            [SerializeField] private List<Entry> items;

            public List<Entry> Items => items;
        }
#pragma warning restore 0649

        [TestCase("liver", "Liver")]
        [TestCase("kidney_l", "Left kidney")]
        [TestCase("kidney_r", "Right kidney")]
        [TestCase("superior_lobe_of_left_lung", "Superior lobe of left lung")]
        [TestCase("mucosa_of_stomach", "Mucosa of stomach")]
        [TestCase("Kidney.l", "Left kidney")]
        [TestCase("  spleen ", "Spleen")]
        [TestCase("l", "L")]
        public void ObjectNameBecomesDisplayName(string objectName, string expected)
        {
            Assert.AreEqual(expected, OrganNames.Display(objectName));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("_")]
        public void EmptyInputGivesEmptyText(string objectName)
        {
            Assert.AreEqual(string.Empty, OrganNames.Display(objectName));
        }

        [Test]
        public void EveryOrganInTheManifestGetsItsManifestName()
        {
            var manifest = JsonUtility.FromJson<Manifest>("{\"items\":" + File.ReadAllText(ManifestPath) + "}");
            int checkedOrgans = 0;
            foreach (Entry entry in manifest.Items)
            {
                if (entry.Layer != "organ")
                    continue;
                string objectName = Path.GetFileNameWithoutExtension(entry.File);
                string expected = entry.OriginalName.EndsWith(".l") ? "Left " + entry.OriginalName.Substring(0, entry.OriginalName.Length - 2).ToLowerInvariant()
                    : entry.OriginalName.EndsWith(".r") ? "Right " + entry.OriginalName.Substring(0, entry.OriginalName.Length - 2).ToLowerInvariant()
                    : entry.OriginalName;
                Assert.AreEqual(expected, OrganNames.Display(objectName), entry.File);
                checkedOrgans++;
            }
            Assert.AreEqual(22, checkedOrgans);
        }
    }
}
