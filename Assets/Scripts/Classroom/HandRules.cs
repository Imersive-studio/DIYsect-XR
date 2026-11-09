using Unity.Netcode;

namespace DIYsect.Classroom
{
    /// <summary>Who may take a hand down: the person it belongs to, or the host (the professor). Pure, so it is tested without a network.</summary>
    public static class HandRules
    {
        public static bool CanLower(ulong senderClientId, ulong handOwnerClientId)
        {
            return senderClientId == handOwnerClientId || senderClientId == NetworkManager.ServerClientId;
        }

        /// <summary>Only the host may take everyone's hand down at once.</summary>
        public static bool CanLowerAll(ulong senderClientId)
        {
            return senderClientId == NetworkManager.ServerClientId;
        }
    }
}
