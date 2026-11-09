namespace XRMultiplayer
{
    public static class RoomOperationRule
    {
        private static readonly RoomOperationPhase[,] Table = Build();
        public static RoomOperationPhase Next(RoomOperationPhase phase, RoomOperationEvent trigger)
            => Table[(int)phase, (int)trigger];

        private static RoomOperationPhase[,] Build()
        {
            var table = new RoomOperationPhase[6, 6];
            for (int state = 0; state < 6; state++)
                for (int trigger = 0; trigger < 6; trigger++)
                    table[state, trigger] = (RoomOperationPhase)state;
            foreach (RoomOperationPhase ready in new[] { RoomOperationPhase.Idle, RoomOperationPhase.Completed, RoomOperationPhase.Failed })
                table[(int)ready, (int)RoomOperationEvent.Start] = RoomOperationPhase.Running;
            table[(int)RoomOperationPhase.Running, (int)RoomOperationEvent.Cancel] = RoomOperationPhase.Cancelling;
            table[(int)RoomOperationPhase.Running, (int)RoomOperationEvent.Complete] = RoomOperationPhase.Completed;
            table[(int)RoomOperationPhase.Running, (int)RoomOperationEvent.Fail] = RoomOperationPhase.Failed;
            for (int state = 0; state < 6; state++)
            {
                table[state, (int)RoomOperationEvent.CleanupFail] = RoomOperationPhase.CleanupFailed;
                table[state, (int)RoomOperationEvent.Drain] = RoomOperationPhase.Idle;
            }
            return table;
        }
    }
}
