using MESagent.Domain.Entities;
using MESagent.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.QualityExceptionDisposition;

public static class QualityGates
{
    public static IQueryable<Guid> PendingReleaseLots(MesDbContext db) => db.Set<HoldRecord>()
        .Where(h => h.ReleasedAt != null && !db.Set<LotRelease>().Any(r => r.LotId == h.LotId
            && r.ReleaseType == "Quality" && r.ReleasedAt >= h.ReleasedAt))
        .Select(h => h.LotId);
}
