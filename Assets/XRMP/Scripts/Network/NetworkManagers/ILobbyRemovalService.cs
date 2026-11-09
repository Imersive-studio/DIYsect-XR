using System.Threading.Tasks;

namespace XRMultiplayer
{
    public interface ILobbyRemovalService
    {
        Task DeleteAsync(string lobbyId);
        Task RemovePlayerAsync(string lobbyId, string playerId);
    }
}
