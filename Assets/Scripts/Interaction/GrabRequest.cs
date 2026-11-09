using System;

namespace DIYsect.Interaction
{
    /// <summary>
    /// The client side of asking the server for something exclusive, as one explicit state machine.
    /// Plain C# and table driven, like LayerOrderRule: each (phase, event) row gives the next phase and the
    /// actions the caller must carry out. A component feeds it the events it sees (the user grabs or lets go, the
    /// server answers, the server takes it back) and performs the returned actions in its own way.
    /// Unlisted pairs change nothing and ask for nothing.
    /// </summary>
    public sealed class GrabRequest
    {
        public enum Event { Select, Exit, Granted, Denied, Revoked, Disable }

        /// <summary>What the caller must do after an event. Several can be combined.</summary>
        [Flags]
        public enum Actions
        {
            None = 0,
            /// <summary>Ask the server for it.</summary>
            SendRequest = 1,
            /// <summary>Give it back to the server (send any last value first).</summary>
            SendRelease = 2,
            /// <summary>Start sending updates (pose, depth, points).</summary>
            StartStreaming = 4,
            /// <summary>Stop sending updates.</summary>
            StopStreaming = 8,
            /// <summary>End the local selection; the user no longer holds it.</summary>
            CancelSelection = 16,
        }

        private readonly struct Row
        {
            public readonly GrabPhase From;
            public readonly Event On;
            public readonly GrabPhase To;
            public readonly Actions Do;

            public Row(GrabPhase from, Event on, GrabPhase to, Actions doActions)
            {
                From = from;
                On = on;
                To = to;
                Do = doActions;
            }
        }

        private static readonly Row[] Rows =
        {
            new Row(GrabPhase.Idle, Event.Select, GrabPhase.Requested, Actions.SendRequest),
            // Grabbed again while the earlier answer is still on its way: that answer now serves this grab.
            new Row(GrabPhase.Abandoned, Event.Select, GrabPhase.Requested, Actions.None),

            new Row(GrabPhase.Requested, Event.Granted, GrabPhase.Granted, Actions.StartStreaming),
            new Row(GrabPhase.Requested, Event.Denied, GrabPhase.Idle, Actions.CancelSelection),
            // The user let go before the answer: wait for it, then give a yes straight back.
            new Row(GrabPhase.Requested, Event.Exit, GrabPhase.Abandoned, Actions.None),
            new Row(GrabPhase.Requested, Event.Revoked, GrabPhase.Abandoned, Actions.CancelSelection),
            new Row(GrabPhase.Requested, Event.Disable, GrabPhase.Idle, Actions.SendRelease),

            new Row(GrabPhase.Abandoned, Event.Granted, GrabPhase.Idle, Actions.SendRelease),
            new Row(GrabPhase.Abandoned, Event.Denied, GrabPhase.Idle, Actions.None),
            new Row(GrabPhase.Abandoned, Event.Disable, GrabPhase.Idle, Actions.SendRelease),

            // A yes for a request something else made (not this component's own grab): this client holds it now,
            // so it must give it back on disable. No streaming until the user actually grabs it here.
            new Row(GrabPhase.Idle, Event.Granted, GrabPhase.Granted, Actions.None),
            new Row(GrabPhase.Granted, Event.Select, GrabPhase.Granted, Actions.StartStreaming),

            new Row(GrabPhase.Granted, Event.Exit, GrabPhase.Idle, Actions.StopStreaming | Actions.SendRelease),
            new Row(GrabPhase.Granted, Event.Revoked, GrabPhase.Idle, Actions.StopStreaming | Actions.CancelSelection),
            new Row(GrabPhase.Granted, Event.Disable, GrabPhase.Idle, Actions.StopStreaming | Actions.SendRelease),
        };

        public GrabPhase Phase { get; private set; } = GrabPhase.Idle;

        /// <summary>Raised when the phase changes (previous, current).</summary>
        public event Action<GrabPhase, GrabPhase> PhaseChanged;

        /// <summary>Applies an event and returns what the caller must do. Unlisted (phase, event) pairs return None.</summary>
        public Actions Handle(Event happened)
        {
            foreach (Row row in Rows)
            {
                if (row.From != Phase || row.On != happened)
                    continue;
                GrabPhase previous = Phase;
                Phase = row.To;
                if (previous != Phase)
                    PhaseChanged?.Invoke(previous, Phase);
                return row.Do;
            }
            return Actions.None;
        }

        /// <summary>Applies the server's answer to a request.</summary>
        public Actions HandleAnswer(bool granted) => Handle(granted ? Event.Granted : Event.Denied);
    }
}
