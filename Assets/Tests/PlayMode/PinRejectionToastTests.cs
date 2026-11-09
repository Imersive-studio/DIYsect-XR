// Presentation only: no network. The toast listens to a QuestionBoard event, so a board is stood in by calling Show directly.
using System.Collections;
using System.Reflection;
using DIYsect.Classroom;
using TMPro;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DIYsect.Networking.Tests
{
    public class PinRejectionToastTests
    {
        private GameObject _root;

        [TearDown]
        public void Cleanup()
        {
            if (_root != null)
                Object.Destroy(_root);
        }

        private PinRejectionToast Build(float seconds, out TMP_Text label)
        {
            _root = new GameObject("ToastTest");
            _root.SetActive(false);
            // The toast needs a board reference to pass its Awake check; the board is never spawned here.
            var boardHolder = new GameObject("Board");
            boardHolder.transform.SetParent(_root.transform);
            boardHolder.SetActive(false);
            // Adding SharedBody to an empty object logs its own setup error; that is expected for this stand-in.
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("SharedBody has no organs"));
            boardHolder.AddComponent<NetworkObject>();
            boardHolder.AddComponent<SharedBody>();
            var board = boardHolder.AddComponent<QuestionBoard>();

            var canvas = new GameObject("Canvas", typeof(Canvas));
            canvas.transform.SetParent(_root.transform);
            var labelObject = new GameObject("Message", typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(canvas.transform);
            label = labelObject.GetComponent<TMP_Text>();

            var toast = _root.AddComponent<PinRejectionToast>();
            Set(toast, "_board", board);
            Set(toast, "_label", label);
            Set(toast, "_seconds", seconds);
            _root.SetActive(true);
            return toast;
        }

        private static void Set(object target, string field, object value)
        {
            target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
        }

        [UnityTest]
        public IEnumerator ShowsTheMappedTextThenHidesAfterItsDuration()
        {
            PinRejectionToast toast = Build(0.3f, out TMP_Text label);
            Assert.IsFalse(toast.IsShowing, "Nothing to say yet.");

            toast.Show(PinRejection.RateLimited);
            Assert.IsTrue(toast.IsShowing);
            Assert.AreEqual(PinRejectionMessages.For(PinRejection.RateLimited), label.text);

            yield return new WaitForSecondsRealtime(0.6f);
            Assert.IsFalse(toast.IsShowing, "The message must hide after its duration.");
        }

        [UnityTest]
        public IEnumerator ASecondRefusalReplacesTheTextAndRestartsTheTimer()
        {
            PinRejectionToast toast = Build(0.5f, out TMP_Text label);
            toast.Show(PinRejection.BadText);
            yield return new WaitForSecondsRealtime(0.3f);
            toast.Show(PinRejection.BadOrgan);
            yield return new WaitForSecondsRealtime(0.3f);

            Assert.IsTrue(toast.IsShowing, "The timer restarted, so it is still visible.");
            Assert.AreEqual(PinRejectionMessages.For(PinRejection.BadOrgan), label.text);
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.IsFalse(toast.IsShowing);
        }
    }
}
