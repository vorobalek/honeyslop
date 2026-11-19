using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Honeyslop.Migrator.Migrations
{
    /// <inheritdoc />
    public partial class ef_drop_delay_job : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "delay_job",
                schema: "public");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "delay_job",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    answered = table.Column<bool>(type: "boolean", nullable: false),
                    claimed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    claimed_by = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    client_ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    duration_ms = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_delay_job", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_delay_job_claimed_at_id",
                schema: "public",
                table: "delay_job",
                columns: new[] { "claimed_at", "id" });
        }
    }
}
