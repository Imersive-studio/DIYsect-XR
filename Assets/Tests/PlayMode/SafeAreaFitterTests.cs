// Under test: classmate canvases fit the safe area and the portrait or landscape match, with the screen numbers injected.
// Authority under test: none (local layout). A real phone's cutout and rotation are not simulated here.
using System.Collections;
using DIYsect.Classroom;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace DIYsect.Networking.Tests
{
    public class SafeAreaFitterTests
    {
        private const float MinButtonHeight = 44f;
        private static readonly Vector2 _reference = new Vector2(1280f, 720f);

        private sealed class FakeMetrics : IScreenMetrics
        {
            public Vector2Int Resolution { get; set; }
            public Rect SafeArea { get; set; }
            public ScreenOrientation Orientation { get; set; }
        }

        private GameObject _root;

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
                Object.DestroyImmediate(_root);
        }

        // A canvas like the rig's: Reset view top right, 240x96 at 24 from the corner, plus a toast strip along the bottom.
        private (Canvas canvas, RectTransform button, RectTransform toast) BuildCanvas()
        {
            _root = new GameObject("TestCanvas");
            var canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = _root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = _reference;
            scaler.matchWidthOrHeight = 0.5f;

            var button = new GameObject("Button", typeof(RectTransform)).GetComponent<RectTransform>();
            button.SetParent(_root.transform, false);
            button.anchorMin = button.anchorMax = button.pivot = Vector2.one;
            button.sizeDelta = new Vector2(240f, 96f);
            button.anchoredPosition = new Vector2(-24f, -24f);

            var toast = new GameObject("Toast", typeof(RectTransform)).GetComponent<RectTransform>();
            toast.SetParent(_root.transform, false);
            toast.anchorMin = new Vector2(0.1f, 0.08f);
            toast.anchorMax = new Vector2(0.9f, 0.18f);
            toast.offsetMin = toast.offsetMax = Vector2.zero;
            return (canvas, button, toast);
        }

        // The rect in screen pixels, worked out from anchors and the scaler maths (a canvas in a test does not follow the injected size).
        private static Rect PixelRect(RectTransform panel, RectTransform child, FakeMetrics metrics, float match)
        {
            var res = new Vector2(metrics.Resolution.x, metrics.Resolution.y);
            float scale = ScreenLayoutMath.CanvasScale(res, _reference, match);
            Vector2 canvasSize = res / scale;
            Vector2 panelMin = Vector2.Scale(panel.anchorMin, canvasSize);
            Vector2 panelMax = Vector2.Scale(panel.anchorMax, canvasSize);
            Vector2 childMin = panelMin + Vector2.Scale(child.anchorMin, panelMax - panelMin) + child.offsetMin;
            Vector2 childMax = panelMin + Vector2.Scale(child.anchorMax, panelMax - panelMin) + child.offsetMax;
            return Rect.MinMaxRect(childMin.x * scale, childMin.y * scale, childMax.x * scale, childMax.y * scale);
        }

        private static IEnumerable Cases()
        {
            yield return new TestCaseData(1080, 2400, new Rect(0f, 60f, 1080f, 2250f), ScreenOrientation.Portrait).SetName("Portrait 1080x2400 with a notch");
            yield return new TestCaseData(2400, 1080, new Rect(120f, 0f, 2160f, 1020f), ScreenOrientation.LandscapeLeft).SetName("Landscape 2400x1080 with a notch");
            yield return new TestCaseData(1280, 720, new Rect(0f, 0f, 1280f, 720f), ScreenOrientation.LandscapeLeft).SetName("Desktop 1280x720");
        }

        [TestCaseSource(nameof(Cases))]
        public void EveryButtonIsInsideTheSafeAreaAndBigEnoughToTap(int width, int height, Rect safe, ScreenOrientation orientation)
        {
            var (canvas, button, toast) = BuildCanvas();
            RectTransform panel = ClassmateCanvasLayout.Apply(canvas);
            var metrics = new FakeMetrics { Resolution = new Vector2Int(width, height), SafeArea = safe, Orientation = orientation };
            panel.GetComponent<SafeAreaFitter>().SetMetrics(metrics);
            _root.GetComponent<CanvasOrientationScaler>().SetMetrics(metrics);

            Assert.AreEqual(panel, button.parent, "Content moved under the SafeArea panel.");
            float match = _root.GetComponent<CanvasScaler>().matchWidthOrHeight;
            Assert.AreEqual(ScreenLayoutMath.MatchFor(new Vector2(width, height)), match);

            foreach (RectTransform item in new[] { button, toast })
            {
                Rect px = PixelRect(panel, item, metrics, match);
                Assert.GreaterOrEqual(px.xMin, safe.xMin - 0.01f, $"{item.name} left");
                Assert.GreaterOrEqual(px.yMin, safe.yMin - 0.01f, $"{item.name} bottom");
                Assert.LessOrEqual(px.xMax, safe.xMax + 0.01f, $"{item.name} right");
                Assert.LessOrEqual(px.yMax, safe.yMax + 0.01f, $"{item.name} top");
            }
            Rect buttonPx = PixelRect(panel, button, metrics, match);
            Assert.GreaterOrEqual(buttonPx.height, MinButtonHeight, "button height in pixels");
            Assert.LessOrEqual(buttonPx.width, safe.width * 0.5f, "button should not take most of the width");
        }

        [Test]
        public void ChangingTheSafeAreaRefitsOnTheNextUpdateAndNotBefore()
        {
            var (canvas, _, _) = BuildCanvas();
            RectTransform panel = ClassmateCanvasLayout.Apply(canvas);
            var fitter = panel.GetComponent<SafeAreaFitter>();
            var metrics = new FakeMetrics { Resolution = new Vector2Int(1080, 2400), SafeArea = new Rect(0f, 0f, 1080f, 2400f) };
            fitter.SetMetrics(metrics);
            Assert.AreEqual(Vector2.one, panel.anchorMax);

            // Rotated: the fitter picks up the new numbers when it next runs.
            metrics.Resolution = new Vector2Int(2400, 1080);
            metrics.SafeArea = new Rect(120f, 0f, 2160f, 1080f);
            Assert.AreEqual(Vector2.one, panel.anchorMax, "Nothing runs until Update.");
            fitter.SendMessage("Update");
            Assert.AreEqual(120f / 2400f, panel.anchorMin.x, 1e-5f);
            Assert.AreEqual(2280f / 2400f, panel.anchorMax.x, 1e-5f);
        }

        [Test]
        public void RunningTheLayoutTwiceAddsNothing()
        {
            var (canvas, button, _) = BuildCanvas();
            RectTransform first = ClassmateCanvasLayout.Apply(canvas);
            // A control another tool adds straight to the canvas is picked up on the next run.
            var late = new GameObject("Late", typeof(RectTransform)).transform;
            late.SetParent(_root.transform, false);
            RectTransform second = ClassmateCanvasLayout.Apply(canvas);
            Assert.AreEqual(first, second);
            Assert.AreEqual(second, late.parent);
            Assert.AreEqual(second, button.parent);
            Assert.AreEqual(1, _root.GetComponentsInChildren<SafeAreaFitter>().Length);
            Assert.AreEqual(1, _root.GetComponents<CanvasOrientationScaler>().Length);
            Assert.AreEqual(1, _root.transform.childCount);
        }
    }
}
