using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using XRMultiplayer;

namespace DIYsect.Invite.Tests
{
    public class RoomOperationQueueTests
    {
        private sealed class FakeOperation : IRoomOperation
        {
            public readonly TaskCompletionSource<bool> Response = new TaskCompletionSource<bool>();
            public int Starts;
            public int Commits;
            public int Cleanups;
            public bool CleanupFails;
            public TaskCompletionSource<bool> CleanupResponse;
            public async Task ExecuteAsync(CancellationToken token)
            {
                Starts++;
                await Response.Task;
                token.ThrowIfCancellationRequested();
                Commits++;
            }
            public async Task RollbackAsync()
            {
                Cleanups++;
                if (CleanupResponse != null) await CleanupResponse.Task;
                if (CleanupFails) throw new Exception("Cleanup unavailable");
            }
        }

        [Test]
        public async Task CancellationWaitsForAnUncancellableServiceResponseAndCleanup()
        {
            var queue = new RoomOperationQueue();
            var first = new FakeOperation();
            Task<RoomOperationResult> running = queue.RunAsync(first);
            Task<bool> cancelled = queue.CancelAndDrainAsync();
            Assert.That(queue.Phase, Is.EqualTo(RoomOperationPhase.Cancelling));
            Assert.That(cancelled.IsCompleted, Is.False);
            first.Response.SetResult(true);
            Assert.That(await running, Is.EqualTo(RoomOperationResult.Cancelled));
            Assert.That(await cancelled, Is.True);
            Assert.That(first.Commits, Is.Zero);
            Assert.That(first.Cleanups, Is.EqualTo(1));
            Assert.That(queue.Phase, Is.EqualTo(RoomOperationPhase.Idle));
        }

        [Test]
        public async Task NewIntentCannotStartUntilOldCleanupAndSupersededQueuedIntentNeverRuns()
        {
            var queue = new RoomOperationQueue();
            var first = new FakeOperation();
            var replaced = new FakeOperation();
            var latest = new FakeOperation();
            Task<RoomOperationResult> one = queue.RunAsync(first);
            Task<RoomOperationResult> two = queue.RunAsync(replaced);
            Task<RoomOperationResult> three = queue.RunAsync(latest);
            Assert.That(latest.Starts, Is.Zero);
            first.Response.SetResult(true);
            Assert.That(await one, Is.EqualTo(RoomOperationResult.Cancelled));
            Assert.That(await two, Is.EqualTo(RoomOperationResult.Cancelled));
            latest.Response.SetResult(true);
            Assert.That(await three, Is.EqualTo(RoomOperationResult.Completed));
            Assert.That(first.Cleanups, Is.EqualTo(1));
            Assert.That(replaced.Starts, Is.Zero);
            Assert.That(latest.Commits, Is.EqualTo(1));
        }

        [Test]
        public async Task FailedCleanupBlocksAnotherConnectionUntilCleanupCanBeRetried()
        {
            var queue = new RoomOperationQueue();
            var first = new FakeOperation { CleanupFails = true };
            Task<RoomOperationResult> one = queue.RunAsync(first);
            first.Response.SetException(new Exception("Prepare failed"));
            Assert.That(await one, Is.EqualTo(RoomOperationResult.CleanupFailed));
            var next = new FakeOperation();
            Assert.That(await queue.RunAsync(next), Is.EqualTo(RoomOperationResult.CleanupFailed));
            Assert.That(next.Starts, Is.Zero);
            first.CleanupFails = false;
            Task<RoomOperationResult> retry = queue.RunAsync(next);
            next.Response.SetResult(true);
            Assert.That(await retry, Is.EqualTo(RoomOperationResult.Completed));
        }

        [Test]
        public async Task PhaseListenerCanCancelBeforeTheServiceStarts()
        {
            var queue = new RoomOperationQueue();
            Task<bool> cancellation = null;
            queue.Changed += phase => { if (phase == RoomOperationPhase.Running) cancellation = queue.CancelAndDrainAsync(); };
            var operation = new FakeOperation();
            Assert.That(await queue.RunAsync(operation), Is.EqualTo(RoomOperationResult.Cancelled));
            Assert.That(await cancellation, Is.True);
            Assert.That(operation.Starts, Is.Zero);
        }

        [Test]
        public async Task DrainSupersededDuringCleanupCannotAuthorizePractice()
        {
            var queue = new RoomOperationQueue();
            var failed = new FakeOperation { CleanupFails = true };
            Task<RoomOperationResult> first = queue.RunAsync(failed);
            failed.Response.SetException(new Exception("Prepare failed"));
            Assert.That(await first, Is.EqualTo(RoomOperationResult.CleanupFailed));
            failed.CleanupFails = false;
            failed.CleanupResponse = new TaskCompletionSource<bool>();
            Task<bool> drain = queue.CancelAndDrainAsync();
            var newer = new FakeOperation();
            Task<RoomOperationResult> next = queue.RunAsync(newer);
            failed.CleanupResponse.SetResult(true);
            Assert.That(await drain, Is.False);
            newer.Response.SetResult(true);
            Assert.That(await next, Is.EqualTo(RoomOperationResult.Completed));
        }
        [Test]
        public async Task PreparationFailureRollsBackAndAllowsRetry()
        {
            var queue = new RoomOperationQueue();
            var failed = new FakeOperation();
            Task<RoomOperationResult> one = queue.RunAsync(failed);
            failed.Response.SetException(new Exception("Invalid code"));
            Assert.That(await one, Is.EqualTo(RoomOperationResult.Failed));
            Assert.That(queue.Phase, Is.EqualTo(RoomOperationPhase.Failed));
            Assert.That(failed.Cleanups, Is.EqualTo(1));
            var corrected = new FakeOperation();
            corrected.Response.SetResult(true);
            Assert.That(await queue.RunAsync(corrected), Is.EqualTo(RoomOperationResult.Completed));
        }
    }
}
