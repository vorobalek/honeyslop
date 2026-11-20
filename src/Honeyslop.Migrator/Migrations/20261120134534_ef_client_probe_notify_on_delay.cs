using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Honeyslop.Migrator.Migrations
{
    /// <inheritdoc />
    public partial class ef_client_probe_notify_on_delay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                drop trigger if exists client_probe_changed on public.client_probe;

                create trigger client_probe_changed
                    after insert or delete on public.client_probe
                    for each statement execute function public.notify_client_probe_change();

                create trigger client_probe_delay_changed
                    after update on public.client_probe
                    for each row
                    when (old.delay_milliseconds is distinct from new.delay_milliseconds or old.timeout_milliseconds is distinct from new.timeout_milliseconds)
                    execute function public.notify_client_probe_change();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                drop trigger if exists client_probe_delay_changed on public.client_probe;
                drop trigger if exists client_probe_changed on public.client_probe;

                create trigger client_probe_changed
                    after insert or update or delete on public.client_probe
                    for each statement execute function public.notify_client_probe_change();
                """);
        }
    }
}
