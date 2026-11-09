// Presentation and local retry only: no network. The connection state the banner listens to is driven through the manager's static
// bindables, the same ones the real connection updates, so the test covers the real subscription.
using System.Collections;
using System.Reflection;
using DIYsect.ClassmateConnection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using XRMultiplayer;

namespace DIYsect.Networking.Tests
{
    public class ReconnectBannerTests
    {
        private const string RoomCode = "KHC8QH";

        private GameObject _root;
        private ReconnectBanner _banner;
        private ClassmateRejoin _rejoin;
        private ClassmateAutoJoin _join;

        [SetUp]
        public void ResetStatics()
        {
            ClearStaleListeners("m_Connected");
            ClearStaleListeners("m_ConnectionState");
            SetStatic("m_Connected", false);
            SetStatic("m_ConnectionState", XRINetworkGameManager.ConnectionState.None);
            XRINetworkGameManager.ConnectedRoomCode = null;
        }

        [TearDown]
        public void Cleanup()
        {
            if (_root != null)
                Object.DestroyImmediate(_root);
            ClearStaleListeners("m_Connected");
            ClearStaleListeners("m_ConnectionState");
            SetStatic("m_Connected", false);
            SetStatic("m_ConnectionState", XRINetworkGameManager.ConnectionState.None);
            XRINetworkGameManager.ConnectedRoomCode = null;
        }

        // Scene tests that ran earlier leave destroyed objects subscribed to these statics; their callbacks would throw here.
        private static void ClearStaleListeners(string field)
        {
            object bindable = typeof(XRINetworkGameManager).GetField(field, BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            for (var type = bindable.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo updated = type.GetField("valueUpdated", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                if (updated != null)
                {
                    updated.SetValue(bindable, null);
                    return;
                }
            }
            Assert.Fail("The bindable has no valueUpdated event to clear.");
        }

        private static void SetStatic(string field, object value)
        {
            object bindable = typeof(XRINetworkGameManager).GetField(field, BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            bindable.GetType().GetProperty("Value").SetValue(bindable, value);
        }

        private static void Set(object target, string field, object value)
        {
            target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
        }

        private void Build(float retrySeconds = 5f, float giveUpSeconds = 120f)
        {
            _root = new GameObject("BannerTest");
            _root.SetActive(false);
            _join = _root.AddComponent<ClassmateAutoJoin>();
            _rejoin = _root.AddComponent<ClassmateRejoin>();
            Set(_rejoin, "_join", _join);
            Set(_rejoin, "_retrySeconds", retrySeconds);
            Set(_rejoin, "_giveUpSeconds", giveUpSeconds);

            var canvas = new GameObject("Canvas", typeof(Canvas));
            canvas.transform.SetParent(_root.transform);
            var panel = new GameObject("Panel");
            panel.transform.SetParent(canvas.transform);
            var label = new GameObject("Message", typeof(Text)).GetComponent<Text>();
            label.transform.SetParent(panel.transform);

            _banner = _root.AddComponent<ReconnectBanner>();
            Set(_banner, "_panel", panel);
            Set(_banner, "_label", label);
            Set(_banner, "_rejoin", _rejoin);
            _root.SetActive(true);
        }

        [Test]
        public void IsHiddenUntilTheConnectionIsLost()
        {
            Build();
            Assert.IsFalse(_banner.IsShowing);
            SetStatic("m_ConnectionState", XRINetworkGameManager.ConnectionState.Authenticated);
            SetStatic("m_ConnectionState", XRINetworkGameManager.ConnectionState.Connecting);
            Assert.IsFalse(_banner.IsShowing, "Joining for the first time is the status line's job.");
            SetStatic("m_Connected", true);
            Assert.IsFalse(_banner.IsShowing);
        }

        [Test]
        public void ShowsReconnectingWhenTheConnectionDrops_AndHidesWhenItReturns()
        {
            Build();
            SetStatic("m_ConnectionState", XRINetworkGameManager.ConnectionState.Connected);
            SetStatic("m_Connected", true);

            SetStatic("m_Connected", false);
            SetStatic("m_ConnectionState", XRINetworkGameManager.ConnectionState.Authenticated);
            Assert.IsTrue(_banner.IsShowing);
            StringAssert.Contains("Reconnecting", _banner.CurrentMessage);

            SetStatic("m_ConnectionState", XRINetworkGameManager.ConnectionState.Connecting);
            StringAssert.Contains("Rejoining", _banner.CurrentMessage);

            SetStatic("m_ConnectionState", XRINetworkGameManager.ConnectionState.Connected);
            SetStatic("m_Connected", true);
            Assert.IsFalse(_banner.IsShowing);
        }

        [Test]
        public void ShowsTheFriendlyMessageForAFullRoomAndAWrongCode()
        {
            Build();
            _banner.OnFailureText("Lobby is full");
            Assert.AreEqual(JoinFailureMessages.For(JoinFailure.RoomFull), _banner.CurrentMessage);
            _banner.OnFailureText("Lobby not found. Please try a new Lobby.");
            Assert.AreEqual(JoinFailureMessages.For(JoinFailure.RoomNotFound), _banner.CurrentMessage);
        }

        [Test]
        public void ADropAsksTheAutoJoinForTheSameRoomCode()
        {
            Build();
            XRINetworkGameManager.ConnectedRoomCode = RoomCode;
            SetStatic("m_Connected", true);
            Assert.AreEqual(RoomCode, _rejoin.RoomCode);
            Assert.AreEqual(string.Empty, _join.PendingCode);

            SetStatic("m_Connected", false);
            Assert.AreEqual(RoomCode, _join.PendingCode, "The auto join must be told to rejoin the same room.");
        }

        [Test]
        public void ADropBeforeEverJoiningAsksForNothing()
        {
            Build();
            SetStatic("m_Connected", false);
            Assert.AreEqual(string.Empty, _join.PendingCode);
        }

        [UnityTest]
        public IEnumerator TellsTheClassmateWhenTheRejoinGivesUp_AndClearsItOnReturn()
        {
            Build(retrySeconds: 0.1f, giveUpSeconds: 1f);
            XRINetworkGameManager.ConnectedRoomCode = RoomCode;
            SetStatic("m_Connected", true);
            SetStatic("m_Connected", false);
            SetStatic("m_ConnectionState", XRINetworkGameManager.ConnectionState.Authenticated);

            float end = Time.realtimeSinceStartup + 4f;
            while (_banner.CurrentMessage != ReconnectBannerModel.GaveUpText && Time.realtimeSinceStartup < end)
                yield return null;
            Assert.AreEqual(ReconnectBannerModel.GaveUpText, _banner.CurrentMessage);

            SetStatic("m_Connected", true);
            Assert.IsFalse(_banner.IsShowing);
        }
    }
}
