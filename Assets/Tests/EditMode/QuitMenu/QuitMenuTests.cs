using DIYsect.Menu;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DIYsect.QuitMenu.Tests
{
    /// <summary>Guards the quit menu rules and the component that applies them.</summary>
    public class QuitMenuTests
    {
        private GameObject _root;
        private GameObject _panel;
        private GameObject _confirm;
        private DIYsect.Menu.QuitMenu _menu;
        private int _quitCalls;

        [SetUp]
        public void Create()
        {
            _root = new GameObject("QuitMenu");
            _panel = new GameObject("Panel");
            _confirm = new GameObject("Confirm");
            _panel.SetActive(false);
            _confirm.SetActive(false);
            _menu = _root.AddComponent<DIYsect.Menu.QuitMenu>();
            var serialized = new SerializedObject(_menu);
            serialized.FindProperty("_panel").objectReferenceValue = _panel;
            serialized.FindProperty("_confirmGroup").objectReferenceValue = _confirm;
            serialized.ApplyModifiedProperties();
            _quitCalls = 0;
            _menu.Quitter = () => _quitCalls++;
        }

        [TearDown]
        public void Destroy()
        {
            Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_panel);
            Object.DestroyImmediate(_confirm);
        }

        [TestCase(QuitMenuState.Closed, QuitMenuEvent.Toggle, QuitMenuState.Open)]
        [TestCase(QuitMenuState.Closed, QuitMenuEvent.Quit, QuitMenuState.Closed)]
        [TestCase(QuitMenuState.Closed, QuitMenuEvent.Cancel, QuitMenuState.Closed)]
        [TestCase(QuitMenuState.Open, QuitMenuEvent.Toggle, QuitMenuState.Closed)]
        [TestCase(QuitMenuState.Open, QuitMenuEvent.Quit, QuitMenuState.Confirming)]
        [TestCase(QuitMenuState.Open, QuitMenuEvent.Cancel, QuitMenuState.Closed)]
        [TestCase(QuitMenuState.Confirming, QuitMenuEvent.Toggle, QuitMenuState.Confirming)]
        [TestCase(QuitMenuState.Confirming, QuitMenuEvent.Quit, QuitMenuState.Confirming)]
        [TestCase(QuitMenuState.Confirming, QuitMenuEvent.Cancel, QuitMenuState.Open)]
        public void RuleTableGivesTheNextState(QuitMenuState state, QuitMenuEvent menuEvent, QuitMenuState expected)
        {
            Assert.AreEqual(expected, QuitMenuRules.Next(state, menuEvent));
        }

        [Test]
        public void OnlyQuitWhileConfirmingRequestsQuit()
        {
            foreach (QuitMenuState state in System.Enum.GetValues(typeof(QuitMenuState)))
                foreach (QuitMenuEvent menuEvent in System.Enum.GetValues(typeof(QuitMenuEvent)))
                {
                    bool expected = state == QuitMenuState.Confirming && menuEvent == QuitMenuEvent.Quit;
                    Assert.AreEqual(expected, QuitMenuRules.IsQuitRequested(state, menuEvent), $"{state} + {menuEvent}");
                }
        }

        [Test]
        public void ToggleOpensAndASecondToggleCloses()
        {
            _menu.Toggle();
            Assert.AreEqual(QuitMenuState.Open, _menu.State);
            Assert.IsTrue(_panel.activeSelf);
            Assert.IsFalse(_confirm.activeSelf);

            _menu.Toggle();
            Assert.AreEqual(QuitMenuState.Closed, _menu.State);
            Assert.IsFalse(_panel.activeSelf);
        }

        [Test]
        public void QuitNeedsTwoRequestsAndCallsTheQuitterOnce()
        {
            _menu.Toggle();
            _menu.RequestQuit();
            Assert.AreEqual(QuitMenuState.Confirming, _menu.State);
            Assert.IsTrue(_confirm.activeSelf);
            Assert.AreEqual(0, _quitCalls);

            _menu.RequestQuit();
            Assert.AreEqual(1, _quitCalls);
        }

        [Test]
        public void CancelFromConfirmingGoesBackToOpenWithoutQuitting()
        {
            _menu.Toggle();
            _menu.RequestQuit();
            _menu.Cancel();
            Assert.AreEqual(QuitMenuState.Open, _menu.State);
            Assert.IsTrue(_panel.activeSelf);
            Assert.IsFalse(_confirm.activeSelf);
            Assert.AreEqual(0, _quitCalls);
        }

        [Test]
        public void ChangedIsRaisedWithTheNewState()
        {
            QuitMenuState seen = QuitMenuState.Closed;
            _menu.Changed += state => seen = state;
            _menu.Toggle();
            Assert.AreEqual(QuitMenuState.Open, seen);
        }
    }
}
