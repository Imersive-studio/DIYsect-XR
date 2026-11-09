using System.Collections.Generic;
using UnityEngine;

namespace DIYsect.Classroom
{
    /// <summary>
    /// Turns the behaviours assigned in the Inspector into <see cref="ITapClaim"/> claimers and asks whether any claims the tap.
    /// Unity cannot serialize an interface, so the Inspector holds behaviours and this checks them.
    /// </summary>
    public static class TapClaims
    {
        /// <summary>Keeps the entries that implement <see cref="ITapClaim"/>. Logs an error for any other entry (including a missing one) and leaves it out.</summary>
        public static List<ITapClaim> Resolve(IReadOnlyList<MonoBehaviour> assigned, Object owner)
        {
            var claimers = new List<ITapClaim>();
            if (assigned == null)
                return claimers;
            for (int i = 0; i < assigned.Count; i++)
            {
                if (assigned[i] is ITapClaim claim)
                    claimers.Add(claim);
                else
                    Debug.LogError($"{owner.name}: tap claimer {i} is {(assigned[i] == null ? "empty" : assigned[i].GetType().Name + ", which does not implement ITapClaim")}. It is ignored.", owner);
            }
            return claimers;
        }

        /// <summary>True when any claimer claims the tap.</summary>
        public static bool AnyClaims(IReadOnlyList<ITapClaim> claimers)
        {
            if (claimers == null)
                return false;
            for (int i = 0; i < claimers.Count; i++)
                if (claimers[i].ClaimsTap)
                    return true;
            return false;
        }
    }
}
