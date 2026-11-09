using NUnit.Framework;
using UnityEditor;

namespace DIYsect.Learning.Tests
{
    /// <summary>The name card a quick tap shows: its words, how it fades, and that the real library names every organ the body has.</summary>
    public class OrganNameTests
    {
        private const string LibraryPath = "Assets/Scripts/Learning/OrganInfo/OrganInfoLibrary.asset";

        private static OrganInfoEntry Kidney() => new OrganInfoEntry("kidney_l", "Left kidney", "Filters blood and makes urine.", "Each holds about a million filters.");

        [Test]
        public void AKnownOrgan_ShowsItsNameInBoldAndAShortMutedLine_NotAParagraph()
        {
            var kidney = new OrganInfoEntry("kidney_l", "Left kidney", "Filters blood and makes urine.", "Each holds about a million filters.", "Urinary system", "Filters blood");
            Assert.AreEqual("<b>Left kidney</b>\n<size=78%><color=#9FB8C4>Urinary system · Filters blood</color></size>", OrganNameText.Build(kidney, "kidney_l"));
            Assert.AreEqual("<b>Left kidney</b>", OrganNameText.Build(Kidney(), "kidney_l"), "Without a system or role, the name alone; the long function line is not shown on a tap.");
        }

        [Test]
        public void AnUnknownOrgan_StillShowsAReadableNameFromItsObjectName()
        {
            Assert.AreEqual("<b>Left kidney</b>", OrganNameText.Build(null, "kidney_l"));
            Assert.AreEqual("<b>Spleen</b>", OrganNameText.Build(null, "spleen"));
        }

        [Test]
        public void NoNameAtAll_GivesNoCard()
        {
            Assert.AreEqual(string.Empty, OrganNameText.Build(null, ""));
            Assert.AreEqual(string.Empty, OrganNameText.Build(null, null));
        }

        [Test]
        public void EveryOrganInTheRealLibrary_GetsACardFromItsObjectName()
        {
            var library = AssetDatabase.LoadAssetAtPath<OrganInfoLibrary>(LibraryPath);
            Assert.IsNotNull(library);
            foreach (OrganInfoEntry entry in library.Entries)
            {
                Assert.IsTrue(library.TryGet(entry.OrganName, out OrganInfoEntry found), entry.OrganName);
                StringAssert.StartsWith("<b>" + entry.DisplayName + "</b>", OrganNameText.Build(found, entry.OrganName));
            }
        }

        [Test]
        public void TheCardIsFullyVisibleThenFadesThenIsGone()
        {
            var fade = new NameCardFade(holdSeconds: 2f, fadeSeconds: 1f);
            Assert.AreEqual(0f, fade.Alpha(0f), "Nothing was shown yet.");

            fade.Show(10f);
            Assert.AreEqual(1f, fade.Alpha(10f));
            Assert.AreEqual(1f, fade.Alpha(12f), 1e-4f);
            Assert.AreEqual(0.5f, fade.Alpha(12.5f), 1e-4f);
            Assert.AreEqual(0f, fade.Alpha(13f));
            Assert.IsFalse(fade.Visible(14f));
            Assert.IsTrue(fade.Visible(12.1f));
        }

        [Test]
        public void ShowingAgainRestartsTheClock()
        {
            var fade = new NameCardFade(2f, 1f);
            fade.Show(0f);
            Assert.AreEqual(0f, fade.Alpha(3.5f));
            fade.Show(3.5f);
            Assert.AreEqual(1f, fade.Alpha(5f));
        }

        [Test]
        public void ABadFadeSettingFailsLoudly()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new NameCardFade(-1f, 1f));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new NameCardFade(1f, 0f));
        }
    }
}
