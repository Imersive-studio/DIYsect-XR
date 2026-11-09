using System;
using System.Threading;
using System.Threading.Tasks;

namespace XRMultiplayer
{
    // Authority: local connection intent only. This queue never writes shared lesson state.
    public sealed class RoomOperationQueue
    {
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
        private CancellationTokenSource _activeCancellation;
        private IRoomOperation _unfinishedCleanup;
        private long _revision;

        public RoomOperationPhase Phase { get; private set; } = RoomOperationPhase.Idle;
        public Exception LastError { get; private set; }
        public event Action<RoomOperationPhase> Changed;

        public Task<RoomOperationResult> RunAsync(IRoomOperation operation)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            long revision = Supersede();
            return RunQueuedAsync(revision, operation);
        }

        // Completion means the cancelled service work has settled and its resources have been released.
        public async Task<bool> CancelAndDrainAsync()
        {
            long revision = Supersede();
            await _gate.WaitAsync();
            try
            {
                if (revision != _revision) return false;
                if (!await RetryCleanupAsync()) return false;
                Change(RoomOperationEvent.Drain);
                return true;
            }
            finally { _gate.Release(); }
        }

        private long Supersede()
        {
            long revision = ++_revision;
            Change(RoomOperationEvent.Cancel);
            _activeCancellation?.Cancel();
            return revision;
        }

        private async Task<RoomOperationResult> RunQueuedAsync(long revision, IRoomOperation operation)
        {
            await _gate.WaitAsync();
            try
            {
                if (revision != _revision) return RoomOperationResult.Cancelled;
                if (!await RetryCleanupAsync()) return RoomOperationResult.CleanupFailed;
                if (revision != _revision) return RoomOperationResult.Cancelled;
                LastError = null;
                Change(RoomOperationEvent.Start);
                // A phase listener may submit another intent before execution starts.
                if (revision != _revision)
                {
                    Change(RoomOperationEvent.Drain);
                    return RoomOperationResult.Cancelled;
                }
                using (var cancellation = new CancellationTokenSource())
                {
                    _activeCancellation = cancellation;
                    try
                    {
                        await operation.ExecuteAsync(cancellation.Token);
                        cancellation.Token.ThrowIfCancellationRequested();
                        Change(RoomOperationEvent.Complete);
                        return RoomOperationResult.Completed;
                    }
                    catch (Exception exception)
                    {
                        LastError = exception;
                        _unfinishedCleanup = operation;
                        if (!await RetryCleanupAsync()) return RoomOperationResult.CleanupFailed;
                        bool cancelled = cancellation.IsCancellationRequested || exception is OperationCanceledException;
                        Change(cancelled ? RoomOperationEvent.Drain : RoomOperationEvent.Fail);
                        return cancelled ? RoomOperationResult.Cancelled : RoomOperationResult.Failed;
                    }
                    finally { _activeCancellation = null; }
                }
            }
            finally { _gate.Release(); }
        }

        private async Task<bool> RetryCleanupAsync()
        {
            if (_unfinishedCleanup == null) return true;
            try
            {
                await _unfinishedCleanup.RollbackAsync();
                _unfinishedCleanup = null;
                if (Phase == RoomOperationPhase.CleanupFailed) Change(RoomOperationEvent.Drain);
                return true;
            }
            catch (Exception exception)
            {
                LastError = exception;
                Change(RoomOperationEvent.CleanupFail);
                return false;
            }
        }

        private void Change(RoomOperationEvent trigger)
        {
            RoomOperationPhase next = RoomOperationRule.Next(Phase, trigger);
            if (next == Phase) return;
            Phase = next;
            Changed?.Invoke(next);
        }
    }
}
