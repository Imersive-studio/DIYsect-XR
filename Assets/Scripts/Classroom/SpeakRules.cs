using Unity.Netcode;

namespace DIYsect.Classroom
{
    /// <summary>Who may let a classmate speak or take that away: only the host (the professor). Pure, so it is tested without a network.</summary>
    public static class SpeakRules
    {
        public static bool CanChange(ulong senderClientId)
        {
            return senderClientId == NetworkManager.ServerClientId;
        }
    }
}
