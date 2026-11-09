// Starts a real in-process Netcode host and clients over UnityTransport on localhost (no cloud services).
// Authority under test: the server alone writes the quiz phase and counts. Only the host may start, end or clear a
// round; only classmates may answer, once per round; each asker hears back whether it was right.
using System.Collections;
using System.Collections.Generic;
using System.Text;
using DIYsect.Networking;
using DIYsect.Networking.Tests;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine.TestTools;

namespace DIYsect.Learning.Tests
{
    public class QuizRoundTests : NetworkedBodyTestBase
    {
        private const int Target = 4;
        private const int WrongOrgan = 5;

        private QuizRound _server;
        private QuizRound _quizA;
        private QuizRound _quizB;
        private readonly List<(int round, QuizResult result, bool correct)> _answersA = new List<(int, QuizResult, bool)>();
        private readonly List<(int round, QuizResult result, bool correct)> _answersB = new List<(int, QuizResult, bool)>();
        private readonly List<QuizResult> _rejectionsA = new List<QuizResult>();

        [SetUp]
        public void ClearRecords()
        {
            _answersA.Clear();
            _answersB.Clear();
            _rejectionsA.Clear();
        }

        private IEnumerator StartTwoClients()
        {
            yield return StartHostAndSpawnBody();
            _server = _serverBody.GetComponent<QuizRound>();
            Assert.IsNotNull(_server, "The body prefab has no QuizRound. Run Tools > DIYsect > Add Quiz.");

            NetworkManager a = StartClient("ClientA");
            NetworkManager b = StartClient("ClientB");
            SharedBody bodyA = null;
            SharedBody bodyB = null;
            yield return WaitForBody(a, found => bodyA = found);
            yield return WaitForBody(b, found => bodyB = found);
            _quizA = bodyA.GetComponent<QuizRound>();
            _quizB = bodyB.GetComponent<QuizRound>();
            _quizA.LocalAnswerResolved += (round, result, correct) => _answersA.Add((round, result, correct));
            _quizB.LocalAnswerResolved += (round, result, correct) => _answersB.Add((round, result, correct));
            _quizA.LocalRequestRejected += result => _rejectionsA.Add(result);
            yield return WaitFor(() => _quizA.OrganCount == _serverBody.Organs.Count && _quizB.OrganCount == _serverBody.Organs.Count,
                "every client to receive one count per organ");
        }

        private IEnumerator StartRound(int organ, int expectedRound)
        {
            _server.RequestStart(organ);
            foreach (QuizRound quiz in new[] { _server, _quizA, _quizB })
                yield return WaitFor(() => quiz.Phase == QuizPhase.Asking && quiz.Round == expectedRound && quiz.TargetOrgan == organ,
                    $"{quiz.name} to start round {expectedRound}", () => $"Phase {quiz.Phase}, round {quiz.Round}, target {quiz.TargetOrgan}.");
        }

        private static IEnumerator WaitForCounts(QuizRound quiz, int right, int wrong)
        {
            yield return WaitFor(() => quiz.RightCount == right && quiz.WrongCount == wrong,
                $"{quiz.name} to show {right} right and {wrong} wrong", () => $"Shows {quiz.RightCount} right, {quiz.WrongCount} wrong.");
        }

        [UnityTest]
        public IEnumerator AnswersAreJudgedAndCountedOnEveryPeer()
        {
            yield return StartTwoClients();
            yield return StartRound(Target, 1);

            _quizA.SubmitAnswer(Target);
            _quizB.SubmitAnswer(WrongOrgan);

            foreach (QuizRound quiz in new[] { _server, _quizA, _quizB })
            {
                yield return WaitForCounts(quiz, 1, 1);
                Assert.AreEqual(1, quiz.TapsFor(Target));
                Assert.AreEqual(1, quiz.TapsFor(WrongOrgan));
                Assert.AreEqual(0, quiz.TapsFor(0));
            }
            yield return WaitFor(() => _answersA.Count == 1 && _answersB.Count == 1, "both askers to hear back");
            Assert.AreEqual((1, QuizResult.Accepted, true), _answersA[0]);
            Assert.AreEqual((1, QuizResult.Accepted, false), _answersB[0]);
        }

        [UnityTest]
        public IEnumerator ASecondAnswerInTheSameRoundIsRefused()
        {
            yield return StartTwoClients();
            yield return StartRound(Target, 1);

            _quizA.SubmitAnswer(WrongOrgan);
            yield return WaitFor(() => _answersA.Count == 1, "the first answer to be judged");
            _quizA.SubmitAnswer(Target);
            yield return WaitFor(() => _answersA.Count == 2, "the second answer to be judged");

            Assert.AreEqual(QuizResult.AlreadyAnswered, _answersA[1].result);
            Assert.AreEqual(0, _server.RightCount, "A second try must not turn a wrong answer into a right one.");
            Assert.AreEqual(1, _server.WrongCount);
        }

        [UnityTest]
        public IEnumerator OnlyTheHostRunsTheQuiz()
        {
            yield return StartTwoClients();
            _quizA.RequestStart(Target);
            yield return WaitFor(() => _rejectionsA.Count == 1, "the client's start to be refused");
            Assert.AreEqual(QuizResult.NotAllowed, _rejectionsA[0]);
            Assert.AreEqual(QuizPhase.Idle, _server.Phase);

            yield return StartRound(Target, 1);
            _quizA.RequestEnd();
            yield return WaitFor(() => _rejectionsA.Count == 2, "the client's end to be refused");
            Assert.AreEqual(QuizResult.NotAllowed, _rejectionsA[1]);
            Assert.AreEqual(QuizPhase.Asking, _server.Phase);
        }

        [UnityTest]
        public IEnumerator EndingFreezesTheCountsAndANewRoundStartsFresh()
        {
            yield return StartTwoClients();
            yield return StartRound(Target, 1);
            _quizA.SubmitAnswer(Target);
            yield return WaitForCounts(_quizB, 1, 0);

            _server.RequestEnd();
            yield return WaitFor(() => _quizB.Phase == QuizPhase.Revealed, "the round to end on client B");
            _quizB.SubmitAnswer(Target);
            yield return WaitFor(() => _answersB.Count == 1, "the late answer to be judged");
            Assert.AreEqual(QuizResult.WrongPhase, _answersB[0].result);
            Assert.AreEqual(1, _server.RightCount, "An answer after the end must not count.");

            yield return StartRound(WrongOrgan, 2);
            foreach (QuizRound quiz in new[] { _server, _quizA, _quizB })
            {
                yield return WaitForCounts(quiz, 0, 0);
                Assert.AreEqual(0, quiz.TapsFor(Target), "A new round starts with no taps.");
            }
            _quizA.SubmitAnswer(WrongOrgan);
            yield return WaitFor(() => _answersA.Count == 2, "client A's answer to the new round");
            Assert.AreEqual((2, QuizResult.Accepted, true), _answersA[1]);

            _server.RequestEnd();
            _server.RequestClose();
            yield return WaitFor(() => _quizA.Phase == QuizPhase.Idle && _quizB.Phase == QuizPhase.Idle, "the quiz to clear everywhere");
        }

        [UnityTest]
        public IEnumerator TheQuestionTextReachesEveryPeerAndIsClearedByTheNextRound()
        {
            yield return StartTwoClients();
            const string question = "Tap the organ that stores bile";
            _server.RequestStart(Target, question);
            foreach (QuizRound quiz in new[] { _server, _quizA, _quizB })
                yield return WaitFor(() => quiz.Phase == QuizPhase.Asking && quiz.Prompt == question,
                    $"{quiz.name} to show the question", () => $"Phase {quiz.Phase}, prompt \"{quiz.Prompt}\".");

            _server.RequestEnd();
            yield return WaitFor(() => _quizA.Phase == QuizPhase.Revealed, "the round to end");
            Assert.AreEqual(question, _quizA.Prompt, "The question stays on screen with the final counts.");

            _server.RequestStart(WrongOrgan);
            foreach (QuizRound quiz in new[] { _server, _quizA, _quizB })
                yield return WaitFor(() => quiz.Phase == QuizPhase.Asking && quiz.Round == 2 && quiz.Prompt.Length == 0,
                    $"{quiz.name} to drop the old question", () => $"Phase {quiz.Phase}, prompt \"{quiz.Prompt}\".");
        }

        [UnityTest]
        public IEnumerator ATooLongQuestionTextIsCutNotRefused()
        {
            yield return StartTwoClients();
            _server.RequestStart(Target, new string('x', 400));
            yield return WaitFor(() => _quizA.Phase == QuizPhase.Asking && _quizA.Prompt.Length > 0, "client A to receive the question");
            Assert.AreEqual(QuizPromptText.MaxBytes, Encoding.UTF8.GetByteCount(_quizA.Prompt));
        }

#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator EveryDefaultQuestionResolvesToAnOrganOfTheSpawnedBody()
        {
            yield return StartHostAndSpawnBody();
            var defaults = UnityEditor.AssetDatabase.LoadAssetAtPath<QuizQuestionSet>("Assets/Scripts/Learning/Quiz/DefaultQuizQuestions.asset");
            Assert.IsNotNull(defaults, "DefaultQuizQuestions.asset is missing.");
            var organs = _serverBody.Organs;
            for (int i = 0; i < defaults.Count; i++)
            {
                int organ = QuizOrganLookup.IndexOf(organs.Count, n => organs[n].name, defaults.Get(i).TargetOrgan);
                Assert.GreaterOrEqual(organ, 0, $"Question {i} asks for \"{defaults.Get(i).TargetOrgan}\", which is not a synced organ.");
            }
        }
#endif

        [UnityTest]
        public IEnumerator ALateJoinerSeesTheRunningRound()
        {
            yield return StartTwoClients();
            yield return StartRound(Target, 1);
            _quizA.SubmitAnswer(Target);
            yield return WaitForCounts(_server, 1, 0);

            NetworkManager late = StartClient("Late");
            SharedBody body = null;
            yield return WaitForBody(late, found => body = found);
            QuizRound quiz = body.GetComponent<QuizRound>();
            yield return WaitFor(() => quiz.Phase == QuizPhase.Asking && quiz.TargetOrgan == Target && quiz.RightCount == 1 && quiz.TapsFor(Target) == 1,
                "the late joiner to receive the round", () => $"Phase {quiz.Phase}, target {quiz.TargetOrgan}, right {quiz.RightCount}.");
        }
    }
}
