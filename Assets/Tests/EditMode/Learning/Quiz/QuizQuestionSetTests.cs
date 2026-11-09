using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DIYsect.Learning.Tests
{
    public class QuizQuestionSetTests
    {
        private const string DefaultSetPath = "Assets/Scripts/Learning/Quiz/DefaultQuizQuestions.asset";
        private const string BodyPrefabPath = "Assets/Prefabs/AnatomyBody.prefab";

        private QuizQuestionSet _set;

        [SetUp]
        public void CreateSet()
        {
            _set = ScriptableObject.CreateInstance<QuizQuestionSet>();
        }

        [TearDown]
        public void DestroySet()
        {
            UnityEngine.Object.DestroyImmediate(_set);
        }

        [Test]
        public void ANewSetIsEmpty()
        {
            Assert.AreEqual(0, _set.Count);
            Assert.Throws<ArgumentOutOfRangeException>(() => _set.Get(0));
        }

        [Test]
        public void ASetServesItsQuestionsInOrderThroughTheInterface()
        {
            _set.SetQuestions(new[]
            {
                new QuizQuestion("Tap the organ that stores bile", "gallbladder"),
                new QuizQuestion("Tap the largest solid organ in the abdomen", "liver"),
            });
            IQuizSource source = _set;

            Assert.AreEqual(2, source.Count);
            Assert.AreEqual("Tap the organ that stores bile", source.Get(0).Prompt);
            Assert.AreEqual("gallbladder", source.Get(0).TargetOrgan);
            Assert.AreEqual("liver", source.Get(1).TargetOrgan);
            Assert.Throws<ArgumentOutOfRangeException>(() => source.Get(2));
            Assert.Throws<ArgumentOutOfRangeException>(() => source.Get(-1));
        }

        [Test]
        public void AQuestionNeedsAPromptAndAnOrganAndAShortEnoughPrompt()
        {
            Assert.IsTrue(new QuizQuestion("Tap the liver", "liver").IsValid(out _));
            Assert.IsFalse(new QuizQuestion("", "liver").IsValid(out string noPrompt));
            StringAssert.Contains("prompt is empty", noPrompt);
            Assert.IsFalse(new QuizQuestion("Tap the liver", " ").IsValid(out string noOrgan));
            StringAssert.Contains("organ is empty", noOrgan);
            Assert.IsFalse(new QuizQuestion(new string('a', 300), "liver").IsValid(out string tooLong));
            StringAssert.Contains("longer than", tooLong);
            Assert.IsFalse(default(QuizQuestion).IsValid(out _));
        }

        [Test]
        public void TheDefaultSetHasAboutTenValidQuestionsWithDifferentAnswers()
        {
            var defaults = AssetDatabase.LoadAssetAtPath<QuizQuestionSet>(DefaultSetPath);
            Assert.IsNotNull(defaults, $"{DefaultSetPath} is missing or its script reference is broken.");
            Assert.That(defaults.Count, Is.InRange(8, 10));

            var seen = new System.Collections.Generic.HashSet<string>();
            for (int i = 0; i < defaults.Count; i++)
            {
                QuizQuestion question = defaults.Get(i);
                Assert.IsTrue(question.IsValid(out string problem), $"Question {i}: {problem}.");
                Assert.IsTrue(seen.Add(question.TargetOrgan), $"Question {i} repeats the organ {question.TargetOrgan}.");
            }
        }

        [Test]
        public void EveryDefaultQuestionNamesAnOrganThatIsInTheBody()
        {
            var defaults = AssetDatabase.LoadAssetAtPath<QuizQuestionSet>(DefaultSetPath);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BodyPrefabPath);
            Assert.IsNotNull(defaults);
            Assert.IsNotNull(prefab, $"{BodyPrefabPath} is missing.");
            // The organs are objects named after their model, so a plain walk over the body's objects finds them.
            Transform[] parts = prefab.GetComponentsInChildren<Transform>(true);

            for (int i = 0; i < defaults.Count; i++)
            {
                string target = defaults.Get(i).TargetOrgan;
                Assert.GreaterOrEqual(QuizOrganLookup.IndexOf(parts.Length, n => parts[n].name, target), 0,
                    $"Question {i} asks for \"{target}\", which is not an object of the body.");
            }
        }
    }
}
