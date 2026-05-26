using System.Threading;
using System.Threading.Tasks;

namespace SyncMaster.Engine;

public interface ISyncCycle
{
    Task<SyncResult> RunCycleAsync(CancellationToken ct = default);
}
