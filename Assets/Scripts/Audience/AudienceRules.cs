using System;
using System.Collections.Generic;

namespace DIYsect.Audience
{
    public static class AudienceRules
    {
        // Use a local seed so seating does not change any other system's random sequence.
        public static int[] ChooseSeats(int seatCount, int count, int seed)
        {
            if (seatCount < 0 || count < 0 || count > seatCount)
                throw new ArgumentOutOfRangeException(nameof(count));

            var random = new Random(seed);
            var seats = new List<int>(seatCount);
            for (int i = 0; i < seatCount; i++)
                seats.Add(i);

            var chosen = new int[count];
            for (int i = 0; i < count; i++)
            {
                int index = random.Next(seats.Count);
                chosen[i] = seats[index];
                seats.RemoveAt(index);
            }

            Array.Sort(chosen);
            return chosen;
        }

        public static float ShiftInterval(int figureIndex) => 9f + (figureIndex * 7 % 13);

        public static bool FollowsStudent(int figureIndex, float distanceSquared) =>
            figureIndex % 5 == 0 && distanceSquared > 1.5f * 1.5f;
    }
}
