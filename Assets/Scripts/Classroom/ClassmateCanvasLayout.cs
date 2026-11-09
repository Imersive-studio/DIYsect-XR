using UnityEngine;
using UnityEngine.UI;

namespace DIYsect.Classroom
{
    /// <summary>
    /// Makes one classmate canvas phone-ready: everything on it moves under a full-screen "SafeArea" panel, and the canvas
    /// matches width or height by orientation. Generic: any button or label added to the canvas later (directly or under the
    /// panel) is covered when this runs again. Safe to run more than once.
    /// </summary>
    public static class ClassmateCanvasLayout
    {
        public const string PanelName = "SafeArea";

        public static RectTransform Apply(Canvas canvas)
        {
            Transform root = canvas.transform;
            RectTransform panel = root.Find(PanelName) as RectTransform;
            if (panel == null)
            {
                var panelObject = new GameObject(PanelName, typeof(RectTransform));
                panel = (RectTransform)panelObject.transform;
                panel.SetParent(root, false);
                panel.anchorMin = Vector2.zero;
                panel.anchorMax = Vector2.one;
                panel.offsetMin = Vector2.zero;
                panel.offsetMax = Vector2.zero;
            }
            if (panel.GetComponent<SafeAreaFitter>() == null)
                panel.gameObject.AddComponent<SafeAreaFitter>();

            // Collect first: reparenting while looping would skip children.
            var others = new System.Collections.Generic.List<Transform>();
            foreach (Transform child in root)
                if (child != panel)
                    others.Add(child);
            foreach (Transform child in others)
                child.SetParent(panel, false);

            if (canvas.TryGetComponent(out CanvasScaler _) && canvas.GetComponent<CanvasOrientationScaler>() == null)
                canvas.gameObject.AddComponent<CanvasOrientationScaler>();
            return panel;
        }
    }
}
