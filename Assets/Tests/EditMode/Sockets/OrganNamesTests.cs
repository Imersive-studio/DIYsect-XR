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

        [System.Serializable]
        private class Entry
        {
            public string file;
            public string original_name;
            public string layer;
        }

        [System.Serializable]
        private class Manifest
        {
            public List<Entry> items;
        }

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
            foreach (Entry entry in manifest.items)
            {
                if (entry.layer != "organ")
                    continue;
                string objectName = Path.GetFileNameWithoutExtension(entry.file);
                string expected = entry.original_name.EndsWith(".l") ? "Left " + entry.original_name.Substring(0, entry.original_name.Length - 2).ToLowerInvariant()
                    : entry.original_name.EndsWith(".r") ? "Right " + entry.original_name.Substring(0, entry.original_name.Length - 2).ToLowerInvariant()
                    : entry.original_name;
                Assert.AreEqual(expected, OrganNames.Display(objectName), entry.file);
                checkedOrgans++;
            }
            Assert.AreEqual(22, checkedOrgans);
        }
    }
}
