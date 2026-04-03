using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Pgvector;

#nullable disable

namespace NarrativeEngine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "iX_users_email",
                table: "users");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.AddColumn<string>(
                name: "deletedBy",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "role",
                table: "users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "projects",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    userId = table.Column<long>(type: "bigint", nullable: false),
                    isDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    deletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deletedBy = table.Column<string>(type: "text", nullable: true),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    createdBy = table.Column<string>(type: "text", nullable: true),
                    modifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    modifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pK_projects", x => x.id);
                    table.ForeignKey(
                        name: "fK_projects_users_userId",
                        column: x => x.userId,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "chapters",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    orderIndex = table.Column<int>(type: "integer", nullable: false),
                    tokenCount = table.Column<int>(type: "integer", nullable: false),
                    summary = table.Column<string>(type: "text", nullable: true),
                    projectId = table.Column<long>(type: "bigint", nullable: false),
                    isDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    deletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deletedBy = table.Column<string>(type: "text", nullable: true),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    createdBy = table.Column<string>(type: "text", nullable: true),
                    modifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    modifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pK_chapters", x => x.id);
                    table.ForeignKey(
                        name: "fK_chapters_projects_projectId",
                        column: x => x.projectId,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "characters",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    projectId = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    isDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    deletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deletedBy = table.Column<string>(type: "text", nullable: true),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    createdBy = table.Column<string>(type: "text", nullable: true),
                    modifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    modifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pK_characters", x => x.id);
                    table.ForeignKey(
                        name: "fK_characters_projects_projectId",
                        column: x => x.projectId,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "emotion_scores",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    paragraphIndex = table.Column<int>(type: "integer", nullable: false),
                    score = table.Column<float>(type: "real", nullable: true),
                    emotionTag = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    chapterId = table.Column<long>(type: "bigint", nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    createdBy = table.Column<string>(type: "text", nullable: true),
                    modifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pK_emotion_scores", x => x.id);
                    table.ForeignKey(
                        name: "fK_emotion_scores_chapters_chapterId",
                        column: x => x.chapterId,
                        principalTable: "chapters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_embeddings",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    embedding = table.Column<Vector>(type: "vector(1536)", nullable: false),
                    characterId = table.Column<long>(type: "bigint", nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    createdBy = table.Column<string>(type: "text", nullable: true),
                    modifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pK_character_embeddings", x => x.id);
                    table.ForeignKey(
                        name: "fK_character_embeddings_characters_characterId",
                        column: x => x.characterId,
                        principalTable: "characters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_traits",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    characterId = table.Column<long>(type: "bigint", nullable: false),
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    label = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    options = table.Column<string>(type: "jsonb", nullable: true),
                    value = table.Column<string>(type: "text", nullable: true),
                    orderIndex = table.Column<int>(type: "integer", nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    createdBy = table.Column<string>(type: "text", nullable: true),
                    modifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    modifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pK_character_traits", x => x.id);
                    table.ForeignKey(
                        name: "fK_character_traits_characters_characterId",
                        column: x => x.characterId,
                        principalTable: "characters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_users_email_active",
                table: "users",
                column: "email",
                unique: true,
                filter: "\"isDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "iX_chapters_projectId",
                table: "chapters",
                column: "projectId");

            migrationBuilder.CreateIndex(
                name: "ux_character_embeddings_character_id",
                table: "character_embeddings",
                column: "characterId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_character_traits_character_key",
                table: "character_traits",
                columns: new[] { "characterId", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "iX_characters_projectId",
                table: "characters",
                column: "projectId");

            migrationBuilder.CreateIndex(
                name: "ux_emotion_scores_chapter_paragraph",
                table: "emotion_scores",
                columns: new[] { "chapterId", "paragraphIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "iX_projects_userId",
                table: "projects",
                column: "userId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "character_embeddings");

            migrationBuilder.DropTable(
                name: "character_traits");

            migrationBuilder.DropTable(
                name: "emotion_scores");

            migrationBuilder.DropTable(
                name: "characters");

            migrationBuilder.DropTable(
                name: "chapters");

            migrationBuilder.DropTable(
                name: "projects");

            migrationBuilder.DropIndex(
                name: "ux_users_email_active",
                table: "users");

            migrationBuilder.DropColumn(
                name: "deletedBy",
                table: "users");

            migrationBuilder.DropColumn(
                name: "role",
                table: "users");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.CreateIndex(
                name: "iX_users_email",
                table: "users",
                column: "email",
                unique: true);
        }
    }
}
