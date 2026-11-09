using System;
using System.Collections;
using System.Reflection;
using DIYsect.Dissection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DIYsect.Learning.Tests
{
    public class ChecklistPanelTests
    {
        private GameObject _source;
        private GameObject _view;
        private ChecklistTracker _tracker;
        private ChecklistProgress _progress;
        private ChecklistPanel _panel;
        private TMP_Text _card;
        private GuideShape[] _nodes;
        private LineRenderer[] _ticks;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Keep the networking adapter dormant. Feed its public presentation event from real rule changes.
            _source = new GameObject("Checklist source");
            _source.SetActive(false);
            _tracker = _source.AddComponent<ChecklistTracker>();
            _progress = (ChecklistProgress)Field(_tracker, "_progress").GetValue(_tracker);
            _progress.Changed += state => ((Action<ChecklistState>)Field(_tracker, "StateChanged").GetValue(_tracker))?.Invoke(state);
            var cues = _source.AddComponent<ChecklistBodyCues>();
            _view = new GameObject("Checklist view");
            _view.SetActive(false);
            _panel = _view.AddComponent<ChecklistPanel>();
            _card = Text("Headline");
            TMP_Text outgoing = Text("Outgoing");
            var rows = new TMP_Text[7];
            _ticks = new LineRenderer[7];
            _nodes = new GuideShape[7];
            var segments = new GuideShape[7];
            var connectors = new GuideShape[6];
            for (int i = 0; i < 7; i++)
            {
                rows[i] = Text("Row");
                var tick = new GameObject("Tick");
                tick.transform.SetParent(_view.transform, false);
                _ticks[i] = tick.AddComponent<LineRenderer>();
                _ticks[i].positionCount = 3;
                _nodes[i] = Shape("Node");
                segments[i] = Shape("Segment");
                if (i < 6) connectors[i] = Shape("Connector");
            }
            var toolChip = new GameObject("Tool chip");
            toolChip.transform.SetParent(_view.transform, false);
            Set(_panel, "_tracker", _tracker);
            Set(_panel, "_cues", cues);
            Set(_panel, "_cardRoot", _card.transform);
            Set(_panel, "_headline", _card);
            Set(_panel, "_outgoingHeadline", outgoing);
            Set(_panel, "_hint", Text("Hint"));
            Set(_panel, "_stepChip", Text("Step chip"));
            Set(_panel, "_toolChip", toolChip);
            Set(_panel, "_toolChipText", Text("Tool text"));
            Set(_panel, "_badge", Shape("Badge"));
            Set(_panel, "_badgeNumber", Text("Badge number"));
            Set(_panel, "_halo", Shape("Halo"));
            Set(_panel, "_segments", segments);
            Set(_panel, "_count", Text("Count"));
            Set(_panel, "_highlight", Shape("Highlight"));
            Set(_panel, "_rows", rows);
            Set(_panel, "_nodes", _nodes);
            Set(_panel, "_ticks", _ticks);
            Set(_panel, "_connectors", connectors);
            _view.SetActive(true);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(_view);
            Object.Destroy(_source);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SameFrameStepsCoalesceAndReenableSnaps()
        {
            int transitions = 0;
            _panel.TransitionChanged += transition => transitions++;
            _progress.ApplyStage(DissectionStage.Incised);
            _progress.ApplyStage(DissectionStage.SkinOpen);
            yield return null;
            yield return null;
            Assert.That(transitions, Is.EqualTo(1));
            Assert.That(_panel.Text, Is.EqualTo(ChecklistLabels.Headline(ChecklistState.OpenRectus)));
            yield return new WaitForSecondsRealtime(0.75f);
            Assert.That(_card.alpha, Is.EqualTo(1f).Within(0.001f));
            Assert.That(_ticks[0].enabled && _ticks[1].enabled, Is.True);
            Assert.That(_ticks[2].enabled, Is.False);
            _view.SetActive(false);
            _progress.ApplyStage(DissectionStage.OmentumLifted);
            _view.SetActive(true);
            Assert.That(_panel.Transition, Is.EqualTo(ChecklistTransition.Snap));
            Assert.That(_panel.Text, Is.EqualTo(ChecklistLabels.Headline(ChecklistState.LiftOrgan)));
        }

        [UnityTest]
        public IEnumerator InitialNetworkSnapshotAfterEnableDoesNotAnimate()
        {
            _progress.ApplyStage(DissectionStage.OmentumLifted);
            ((Action)Field(_tracker, "StateSynchronized").GetValue(_tracker))?.Invoke();
            yield return null;
            yield return null;
            Assert.That(_panel.Transition, Is.EqualTo(ChecklistTransition.Snap));
            Assert.That(_panel.Text, Is.EqualTo(ChecklistLabels.Headline(ChecklistState.LiftOrgan)));
        }

        [UnityTest]
        public IEnumerator RevertResetAndCompleteInterruptCleanly()
        {
            _progress.ApplyStage(DissectionStage.OmentumLifted);
            _progress.OnHolderChanged(0, true);
            yield return null;
            yield return null;
            _progress.OnHolderChanged(0, false);
            yield return null;
            yield return null;
            Assert.That(_panel.Transition, Is.EqualTo(ChecklistTransition.Revert));
            _progress.ApplyStage(DissectionStage.Intact);
            yield return null;
            yield return null;
            Assert.That(_panel.Transition, Is.EqualTo(ChecklistTransition.Reset));
            _progress.ApplyStage(DissectionStage.OmentumLifted);
            _progress.OnHolderChanged(0, true);
            _progress.OnHeldLongEnough(0);
            _progress.OnSnapResolved(0, true);
            yield return null;
            yield return null;
            Assert.That(_panel.Transition, Is.EqualTo(ChecklistTransition.Complete));
            yield return new WaitForSecondsRealtime(1.3f);
            Assert.That(_panel.Text, Is.EqualTo("Dissection complete"));
            Assert.That(_card.alpha, Is.EqualTo(1f).Within(0.001f));
            Assert.That(_nodes[6].transform.localScale.x, Is.EqualTo(1f).Within(0.001f));
            foreach (LineRenderer tick in _ticks) Assert.That(tick.enabled, Is.True);
        }

        private GuideShape Shape(string name)
        {
            var item = new GameObject(name);
            item.transform.SetParent(_view.transform, false);
            return item.AddComponent<GuideShape>();
        }

        private TMP_Text Text(string name)
        {
            var item = new GameObject(name);
            item.transform.SetParent(_view.transform, false);
            var text = item.AddComponent<TextMeshPro>();
            text.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            return text;
        }

        private static FieldInfo Field(object target, string name) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        private static void Set(object target, string name, object value) => Field(target, name).SetValue(target, value);
    }
}
