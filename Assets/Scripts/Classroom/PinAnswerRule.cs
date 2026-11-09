using Unity.Netcode;

namespace DIYsect.Classroom
{
    /// <summary>
    /// Who may mark a question pin answered: only the host. The professor's headset always hosts (ProfessorAutoHost)
    /// and classmates only join as clients. Pure logic with no network state, so it is easy to test.
    /// </summary>
    public static class PinAnswerRule
    {
        public static bool CanMarkAnswered(ulong senderClientId)
        {
            return senderClientId == NetworkManager.ServerClientId;
        }
    }
}
