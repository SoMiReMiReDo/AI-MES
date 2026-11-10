using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESagent.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FabIntegrityGuards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(ReadSql("Integrity.up.sql"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(ReadSql("Integrity.down.sql"));
        }

        private static string ReadSql(string filename)
        {
            using var stream = typeof(FabIntegrityGuards).Assembly.GetManifestResourceStream(
                "MESagent.Infrastructure.Persistence.Sql." + filename)
                ?? throw new InvalidOperationException("Missing migration resource: " + filename);
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
