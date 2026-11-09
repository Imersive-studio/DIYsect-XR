using System.Threading;
using System.Threading.Tasks;

namespace XRMultiplayer
{
    public interface IRoomOperation
    {
        // Check the token after service awaits and before changing transport or published room state.
        Task ExecuteAsync(CancellationToken cancellation);
        // Release only resources belonging to this operation. Throw if cleanup is incomplete.
        Task RollbackAsync();
    }
}
