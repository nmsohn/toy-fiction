using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NarrativeEngine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAndRefreshToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    passwordHash = table.Column<string>(type: "text", nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    createdBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    modifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    modifiedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    isDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    deletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    tokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    expiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    isRevoked = table.Column<bool>(type: "boolean", nullable: false),
                    revokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    replacedByTokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    revokeReason = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    userId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pK_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fK_refresh_tokens_users_userId",
                        column: x => x.userId,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "userSettings",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    museEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    museTrigger = table.Column<string>(type: "text", nullable: false),
                    emotionAnalysisAuto = table.Column<bool>(type: "boolean", nullable: false),
                    bgmEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    createdBy = table.Column<string>(type: "text", nullable: true),
                    modifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modifiedBy = table.Column<string>(type: "text", nullable: true),
                    userId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pK_userSettings", x => x.id);
                    table.ForeignKey(
                        name: "fK_userSettings_users_userId",
                        column: x => x.userId,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "iX_refresh_tokens_tokenHash",
                table: "refresh_tokens",
                column: "tokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "iX_refresh_tokens_userId",
                table: "refresh_tokens",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "iX_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "iX_userSettings_userId",
                table: "userSettings",
                column: "userId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "userSettings");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
