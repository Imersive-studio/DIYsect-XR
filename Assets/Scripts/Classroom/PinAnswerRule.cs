using DIYsect.Networking;

namespace DIYsect.Classroom
{
    /// <summary>
    /// Who may mark a question pin answered. It reuses the pointer role rule, so there is one answer to "who is the
    /// professor": every client that is not the scalpel steerer. Pure logic with no network state, so it is easy to test.
    /// </summary>
    public static class PinAnswerRule
    {
        public static bool CanMarkAnswered(ulong sender, ulong scalpelSteerer)
        {
            return PointerRoleRule.For(sender, scalpelSteerer) == PointerRole.Professor;
        }
    }
}
