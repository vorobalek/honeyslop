using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Honeyslop.Migrator.Migrations
{
    /// <inheritdoc />
    public partial class ef_entry_generator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "generator",
                schema: "public",
                table: "entry",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.Sql(
                """
                update public.entry
                set generator = substring(model from position('@' in model) + 1),
                    model = substring(model for position('@' in model) - 1)
                where position('@' in model) > 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                update public.entry
                set model = model || '@' || generator
                where model is not null and generator is not null;
                """);

            migrationBuilder.DropColumn(
                name: "generator",
                schema: "public",
                table: "entry");
        }
    }
}
