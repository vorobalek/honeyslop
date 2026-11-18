using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Honeyslop.Migrator.Migrations
{
    /// <inheritdoc />
    public partial class ef_header_and_body_limits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("update request set headers = left(headers, 8192) where length(headers) > 8192;");
            migrationBuilder.Sql("update request set body = left(body, 65536) where length(body) > 65536;");
            migrationBuilder.Sql("update entry set response_body = left(response_body, 65536) where length(response_body) > 65536;");
            migrationBuilder.Sql("update entry set request_headers = left(request_headers, 8192) where length(request_headers) > 8192;");
            migrationBuilder.Sql("update entry set request_body = left(request_body, 65536) where length(request_body) > 65536;");

            migrationBuilder.AlterColumn<string>(
                name: "headers",
                schema: "public",
                table: "request",
                type: "character varying(8192)",
                maxLength: 8192,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "body",
                schema: "public",
                table: "request",
                type: "character varying(65536)",
                maxLength: 65536,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "response_body",
                schema: "public",
                table: "entry",
                type: "character varying(65536)",
                maxLength: 65536,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "request_headers",
                schema: "public",
                table: "entry",
                type: "character varying(8192)",
                maxLength: 8192,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "request_body",
                schema: "public",
                table: "entry",
                type: "character varying(65536)",
                maxLength: 65536,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "headers",
                schema: "public",
                table: "request",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(8192)",
                oldMaxLength: 8192,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "body",
                schema: "public",
                table: "request",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(65536)",
                oldMaxLength: 65536,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "response_body",
                schema: "public",
                table: "entry",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(65536)",
                oldMaxLength: 65536,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "request_headers",
                schema: "public",
                table: "entry",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(8192)",
                oldMaxLength: 8192,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "request_body",
                schema: "public",
                table: "entry",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(65536)",
                oldMaxLength: 65536,
                oldNullable: true);
        }
    }
}
