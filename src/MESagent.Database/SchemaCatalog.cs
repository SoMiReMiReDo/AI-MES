using MESagent.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

internal static class SchemaCatalog
{
    public static void Write(MesDbContext db, TextWriter output)
    {
        output.WriteLine("# 数据库对象与字段字典\n");
        output.WriteLine("由 `MESagent.Database catalog` 从 EF Core 当前模型导出；重新生成命令：`python3 scripts/database.py catalog`。\n");
        output.WriteLine("业务表使用 UUID 主键、UTC 时间和 PostgreSQL `xmin` 并发令牌（系统列，不是额外业务列）。可空字段带 `?`。角色、授权和登录信息采用 ASP.NET Core Identity。实施边界与验证见 [数据库实施说明](06-database-implementation.md)。\n");
        foreach (var schema in db.Model.GetEntityTypes().GroupBy(e => e.GetSchema()).OrderBy(g => g.Key))
        {
            output.WriteLine($"## {schema.Key}（{schema.Count()} 张表）\n");
            output.WriteLine("| 数据表 / C# 对象 | 数据字段与类型 | 外键目标 |\n| --- | --- | --- |");
            foreach (var entity in schema.OrderBy(e => e.GetTableName()))
            {
                var table = StoreObjectIdentifier.Table(entity.GetTableName()!, entity.GetSchema());
                var fields = entity.GetProperties().Where(p => p.GetColumnName(table) != "xmin")
                    .OrderBy(p => p.GetColumnName(table)).Select(p => $"`{p.GetColumnName(table)}: {p.GetColumnType()}{(p.IsNullable ? "?" : "")}`");
                var relations = entity.GetForeignKeys().Select(f =>
                    $"`{string.Join(",", f.Properties.Select(p => p.GetColumnName(table)))} → {f.PrincipalEntityType.GetSchema()}.{f.PrincipalEntityType.GetTableName()}`");
                output.WriteLine($"| `{schema.Key}.{entity.GetTableName()}` / `{entity.ClrType.Name.Replace("`", "")}` | {string.Join("、", fields)} | {string.Join("；", relations)} |");
            }
            output.WriteLine();
        }
        output.WriteLine("## 数据库补充对象\n");
        output.WriteLine("- `public.__ef_migrations_history`：EF Core 迁移历史，不属于业务表。\n- `production.lot_quantities`：从有效 Wafer 归属及报废事实派生的数量视图。\n- `equipment.state_durations`：从设备状态起止时间派生的时长视图。\n- `btree_gist` 扩展、时间区间排斥约束和历史/发布版本保护触发器：见 `FabIntegrityGuards` 迁移及其 SQL 资源。\n");
        output.WriteLine("本表描述结构，不表示所有模块业务已实现。迁移是数据库结构的版本依据；真实部署中的对象以迁移状态核对。");
    }
}
