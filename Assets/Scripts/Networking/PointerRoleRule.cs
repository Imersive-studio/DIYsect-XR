namespace DIYsect.Networking
{
    /// <summary>
    /// Decides how a pointing client is labelled. Roles are not modelled in the project, so the rule mirrors the
    /// guiding hand: the client that steers the scalpel is the Student, every other pointing client is the Professor.
    /// Pure logic with no network state, so it is easy to test.
    /// </summary>
    public static class PointerRoleRule
    {
        public static PointerRole For(ulong sender, ulong scalpelSteerer)
        {
            bool isSteerer = scalpelSteerer != ScalpelSteering.NoSteerer && sender == scalpelSteerer;
            return isSteerer ? PointerRole.Student : PointerRole.Professor;
        }
    }
}
