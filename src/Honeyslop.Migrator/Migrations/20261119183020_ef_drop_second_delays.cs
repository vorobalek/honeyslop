using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Honeyslop.Migrator.Migrations
{
    /// <inheritdoc />
    public partial class ef_drop_second_delays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("delete from public.setting where key = 'response_delay_seconds';");

            migrationBuilder.DropColumn(
                name: "response_delay_seconds",
                schema: "public",
                table: "entry");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "response_delay_seconds",
                schema: "public",
                table: "entry",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(
                """
                update public.entry
                set response_delay_seconds = response_delay_milliseconds / 1000
                where response_delay_milliseconds is not null;

                insert into public.setting (key, value, updated_at)
                select 'response_delay_seconds', (value::bigint / 1000)::text, now()
                from public.setting
                where key = 'response_delay_milliseconds' and value ~ '^[0-9]+$'
                on conflict (key) do nothing;
                """);
        }
    }
}
