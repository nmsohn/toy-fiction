using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NarrativeEngine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateChapterIndex : Migration
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_characters_projectId",
                table: "characters");

            migrationBuilder.DropIndex(
                name: "ix_chapters_projectId_orderIndex",
                table: "chapters");

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
