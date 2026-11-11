using MESagent.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

if (args.Length != 1 || args[0] is not ("migrate" or "status" or "catalog"))
{
    Console.Error.WriteLine("用法: MESagent.Database migrate|status|catalog；数据库操作通过 ConnectionStrings__Mes 提供连接信息。");
    return 2;
}

if (args[0] == "catalog")
{
    await using var design = new MesDbContextFactory().CreateDbContext([]);
    SchemaCatalog.Write(design, Console.Out);
    return 0;
}

var connection = Environment.GetEnvironmentVariable("ConnectionStrings__Mes");
if (string.IsNullOrWhiteSpace(connection))
{
    Console.Error.WriteLine("缺少 ConnectionStrings__Mes，未连接或修改数据库。");
    return 2;
}

await using var db = MesDbContextFactory.Create(connection);
if (args[0] == "migrate")
    await db.Database.MigrateAsync();

var applied = (await db.Database.GetAppliedMigrationsAsync()).ToArray();
var pending = (await db.Database.GetPendingMigrationsAsync()).ToArray();
Console.WriteLine($"已应用迁移: {applied.Length}；待应用: {pending.Length}");

foreach (var migration in applied)
    Console.WriteLine(migration);

return pending.Length == 0 ? 0 : 1;
