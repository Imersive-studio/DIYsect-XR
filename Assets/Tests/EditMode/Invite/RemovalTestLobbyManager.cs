using XRMultiplayer;

namespace DIYsect.Invite.Tests
{
    public class RemovalTestLobbyManager : LobbyManager
    {
        public ILobbyRemovalService Service { get; set; }
        protected override ILobbyRemovalService RemovalService => Service;
    }
}
