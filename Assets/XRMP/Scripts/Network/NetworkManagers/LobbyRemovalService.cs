using System.Threading.Tasks;
using Unity.Services.Lobbies;

namespace XRMultiplayer
{
    public sealed class LobbyRemovalService : ILobbyRemovalService
    {
        public Task DeleteAsync(string lobbyId) => LobbyService.Instance.DeleteLobbyAsync(lobbyId);
        public Task RemovePlayerAsync(string lobbyId, string playerId) => LobbyService.Instance.RemovePlayerAsync(lobbyId, playerId);
    }
}
