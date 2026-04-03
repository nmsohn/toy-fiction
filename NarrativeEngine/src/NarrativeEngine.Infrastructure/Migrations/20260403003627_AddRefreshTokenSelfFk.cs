using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NarrativeEngine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokenSelfFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_characters_projectId",
                table: "characters");

            migrationBuilder.DropIndex(
                name: "ix_chapters_projectId_orderIndex",
                table: "chapters");

            migrationBuilder.AddColumn<long>(
                name: "replacedByTokenId",
                table: "refresh_tokens",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "iX_refresh_tokens_replacedByTokenId",
                table: "refresh_tokens",
                column: "replacedByTokenId");

            migrationBuilder.CreateIndex(
                name: "ix_characters_projectId",
                table: "characters",
                column: "projectId",
                filter: "\"is_deleted\" = false");

            migrationBuilder.CreateIndex(
                name: "ix_chapters_projectId_orderIndex",
                table: "chapters",
                columns: new[] { "projectId", "orderIndex" },
                filter: "\"is_deleted\" = false");

            migrationBuilder.AddForeignKey(
                name: "fK_refresh_tokens_refresh_tokens_replacedByTokenId",
                table: "refresh_tokens",
                column: "replacedByTokenId",
                principalTable: "refresh_tokens",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fK_refresh_tokens_refresh_tokens_replacedByTokenId",
                table: "refresh_tokens");

            migrationBuilder.DropIndex(
                name: "iX_refresh_tokens_replacedByTokenId",
                table: "refresh_tokens");

            migrationBuilder.DropIndex(
                name: "ix_characters_projectId",
                table: "characters");

            migrationBuilder.DropIndex(
                name: "ix_chapters_projectId_orderIndex",
                table: "chapters");

            migrationBuilder.DropColumn(
                name: "replacedByTokenId",
                table: "refresh_tokens");

            migrationBuilder.CreateIndex(
                name: "ix_characters_projectId",
                table: "characters",
                column: "projectId",
                filter: "\"isDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "ix_chapters_projectId_orderIndex",
                table: "chapters",
                columns: new[] { "projectId", "orderIndex" },
                filter: "\"isDeleted\" = false");
        }
    }
}
