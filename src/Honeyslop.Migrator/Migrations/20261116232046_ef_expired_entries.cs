using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Honeyslop.Migrator.Migrations
{
    /// <inheritdoc />
    public partial class ef_expired_entries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_entry_key_hash",
                schema: "public",
                table: "entry");

            migrationBuilder.CreateIndex(
                name: "ix_entry_key_hash",
                schema: "public",
                table: "entry",
                column: "key_hash",
                unique: true,
                filter: "status <> 3");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_entry_key_hash",
                schema: "public",
                table: "entry");

            migrationBuilder.CreateIndex(
                name: "ix_entry_key_hash",
                schema: "public",
                table: "entry",
                column: "key_hash",
                unique: true);
        }
    }
}
