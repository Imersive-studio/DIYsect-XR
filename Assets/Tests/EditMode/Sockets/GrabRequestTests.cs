using DIYsect.Interaction;
using NUnit.Framework;
using A = DIYsect.Interaction.GrabRequest.Actions;
using E = DIYsect.Interaction.GrabRequest.Event;

namespace DIYsect.Interaction.Tests
{
    public class GrabRequestTests
    {
        private GrabRequest _request;

        [SetUp]
        public void CreateRequest()
        {
            _request = new GrabRequest();
        }

        [Test]
        public void GrabbingAsksTheServer()
        {
            Assert.AreEqual(A.SendRequest, _request.Handle(E.Select));
            Assert.AreEqual(GrabPhase.Requested, _request.Phase);
        }

        [Test]
        public void AYesStartsStreamingAndLettingGoGivesItBack()
        {
            _request.Handle(E.Select);
            Assert.AreEqual(A.StartStreaming, _request.HandleAnswer(true));
            Assert.AreEqual(GrabPhase.Granted, _request.Phase);
            Assert.AreEqual(A.StopStreaming | A.SendRelease, _request.Handle(E.Exit));
            Assert.AreEqual(GrabPhase.Idle, _request.Phase);
        }

        [Test]
        public void ANoCancelsTheLocalGrab()
        {
            _request.Handle(E.Select);
            Assert.AreEqual(A.CancelSelection, _request.HandleAnswer(false));
            Assert.AreEqual(GrabPhase.Idle, _request.Phase);
        }

        [Test]
        public void AYesThatArrivesAfterLettingGoIsGivenStraightBack()
        {
            _request.Handle(E.Select);
            Assert.AreEqual(A.None, _request.Handle(E.Exit));
            Assert.AreEqual(GrabPhase.Abandoned, _request.Phase);
            Assert.AreEqual(A.SendRelease, _request.HandleAnswer(true));
            Assert.AreEqual(GrabPhase.Idle, _request.Phase);
        }

        [Test]
        public void ANoThatArrivesAfterLettingGoNeedsNothing()
        {
            _request.Handle(E.Select);
            _request.Handle(E.Exit);
            Assert.AreEqual(A.None, _request.HandleAnswer(false));
            Assert.AreEqual(GrabPhase.Idle, _request.Phase);
        }

        [Test]
        public void GrabbingAgainWhileTheAnswerIsOnItsWayReusesThatAnswer()
        {
            _request.Handle(E.Select);
            _request.Handle(E.Exit);
            Assert.AreEqual(A.None, _request.Handle(E.Select), "No second request: the first answer is still coming.");
            Assert.AreEqual(A.StartStreaming, _request.HandleAnswer(true));
            Assert.AreEqual(GrabPhase.Granted, _request.Phase);
        }

        [Test]
        public void TheServerTakingItBackStopsAndCancels()
        {
            _request.Handle(E.Select);
            _request.HandleAnswer(true);
            Assert.AreEqual(A.StopStreaming | A.CancelSelection, _request.Handle(E.Revoked));
            Assert.AreEqual(GrabPhase.Idle, _request.Phase);
        }

        [Test]
        public void TakenBackBeforeTheAnswerCancelsNowAndReturnsALateYes()
        {
            _request.Handle(E.Select);
            Assert.AreEqual(A.CancelSelection, _request.Handle(E.Revoked));
            Assert.AreEqual(GrabPhase.Abandoned, _request.Phase);
            Assert.AreEqual(A.SendRelease, _request.HandleAnswer(true));
            Assert.AreEqual(GrabPhase.Idle, _request.Phase);
        }

        [Test]
        public void DisablingGivesBackAnythingHeldOrAskedFor()
        {
            Assert.AreEqual(A.None, _request.Handle(E.Disable), "Idle has nothing to give back.");
            _request.Handle(E.Select);
            Assert.AreEqual(A.SendRelease, _request.Handle(E.Disable));
            _request.Handle(E.Select);
            _request.HandleAnswer(true);
            Assert.AreEqual(A.StopStreaming | A.SendRelease, _request.Handle(E.Disable));
            Assert.AreEqual(GrabPhase.Idle, _request.Phase);
        }

        [Test]
        public void EventsThatDoNotApplyChangeNothing()
        {
            Assert.AreEqual(A.None, _request.HandleAnswer(false), "A no nobody asked for.");
            Assert.AreEqual(A.None, _request.Handle(E.Exit));
            Assert.AreEqual(A.None, _request.Handle(E.Revoked));
            Assert.AreEqual(GrabPhase.Idle, _request.Phase);
            _request.Handle(E.Select);
            Assert.AreEqual(A.None, _request.Handle(E.Select), "A second grab while asking.");
            Assert.AreEqual(GrabPhase.Requested, _request.Phase);
        }

        [Test]
        public void AYesForARequestMadeElsewhereIsHeldAndGivenBackOnDisable()
        {
            Assert.AreEqual(A.None, _request.HandleAnswer(true));
            Assert.AreEqual(GrabPhase.Granted, _request.Phase);
            Assert.AreEqual(A.StopStreaming | A.SendRelease, _request.Handle(E.Disable));
            Assert.AreEqual(GrabPhase.Idle, _request.Phase);
        }

        [Test]
        public void GrabbingSomethingAlreadyHeldJustStartsStreaming()
        {
            _request.HandleAnswer(true);
            Assert.AreEqual(A.StartStreaming, _request.Handle(E.Select));
            Assert.AreEqual(GrabPhase.Granted, _request.Phase);
        }

        [Test]
        public void PhaseChangesAreReported()
        {
            int changes = 0;
            GrabPhase last = GrabPhase.Idle;
            _request.PhaseChanged += (previous, current) => { changes++; last = current; };
            _request.Handle(E.Select);
            _request.HandleAnswer(true);
            _request.Handle(E.Exit);
            Assert.AreEqual(3, changes);
            Assert.AreEqual(GrabPhase.Idle, last);
        }
    }
}
