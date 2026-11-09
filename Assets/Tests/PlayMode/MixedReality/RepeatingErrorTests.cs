// Two errors that repeated every frame on a headset: a name tag loop that read a player that had left, and a board read before it woke.
using System.Collections.Generic;
using System.Reflection;
using DIYsect.Classroom;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using XRMultiplayer;

namespace DIYsect.MixedReality.Tests
{
    public class RepeatingErrorTests
    {
        [Test]
        public void TheQuestionBoard_ReadsAsEmptyBeforeItsListExists()
        {
            // The bare board logs that its body is missing, and the bare canvas throws when it is destroyed without a network manager.
            // Neither is what these tests check.
            LogAssert.ignoreFailingMessages = true;
            var go = new GameObject("board");
            try
            {
                var board = go.AddComponent<QuestionBoard>();
                typeof(QuestionBoard).GetField("_pins", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(board, null);

                Assert.AreEqual(0, board.Count);
            }
            finally
            {
                Object.DestroyImmediate(go);
                LogAssert.ignoreFailingMessages = false;
            }
        }

        [Test]
        public void TheWorldCanvas_DropsANameTagWhosePlayerIsGone_InsteadOfThrowingEveryFrame()
        {
            LogAssert.ignoreFailingMessages = true;
            var canvasObject = new GameObject("world canvas");
            var tagObject = new GameObject("name tag");
            try
            {
                var canvas = canvasObject.AddComponent<WorldCanvas>();
                var tag = tagObject.AddComponent<PlayerNameTag>();
                var dictionary = (Dictionary<PlayerNameTag, XRINetworkPlayer>)typeof(WorldCanvas)
                    .GetField("playerDictionary", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(canvas);
                dictionary.Add(tag, null);
                MethodInfo update = typeof(WorldCanvas).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.DoesNotThrow(() => update.Invoke(canvas, null));
                Assert.AreEqual(0, dictionary.Count, "The stale entry must be removed so the loop is clean on the next frame.");
            }
            finally
            {
                Object.DestroyImmediate(tagObject);
                Object.DestroyImmediate(canvasObject);
                LogAssert.ignoreFailingMessages = false;
            }
        }
    }
}
