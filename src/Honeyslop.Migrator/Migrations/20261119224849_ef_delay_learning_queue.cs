using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Honeyslop.Migrator.Migrations
{
    /// <inheritdoc />
    public partial class ef_delay_learning_queue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM public.delay_job;");

            migrationBuilder.AddColumn<long>(
                name: "last_request_id",
                schema: "public",
                table: "client_probe",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "delay_learning_job",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    client_ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    in_work = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    claimed_by = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    claimed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_delay_learning_job", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_request_client_ip_id",
                schema: "public",
                table: "request",
                columns: new[] { "client_ip", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_delay_learning_job_client_ip_in_work",
                schema: "public",
                table: "delay_learning_job",
                columns: new[] { "client_ip", "in_work" },
                unique: true);

            migrationBuilder.Sql("DELETE FROM public.client_probe;");
            migrationBuilder.Sql("""
                INSERT INTO public.delay_learning_job (client_ip, in_work, created_at)
                SELECT DISTINCT CASE WHEN client_ip LIKE '::ffff:%.%' THEN substr(client_ip, 8) ELSE client_ip END, false, now()
                FROM public.request
                WHERE client_ip <> '';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "delay_learning_job",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "ix_request_client_ip_id",
                schema: "public",
                table: "request");

            migrationBuilder.DropColumn(
                name: "last_request_id",
                schema: "public",
                table: "client_probe");
        }
    }
}
