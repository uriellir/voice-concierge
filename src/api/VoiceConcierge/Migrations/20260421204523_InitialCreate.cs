using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoiceConcierge.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "knowledge_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CanonicalQuestion = table.Column<string>(type: "text", nullable: false),
                    AnswerText = table.Column<string>(type: "text", nullable: false),
                    FactsJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_knowledge_items", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "unanswered_questions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NormalizedQuestion = table.Column<string>(type: "text", nullable: false),
                    OriginalText = table.Column<string>(type: "text", nullable: false),
                    Count = table.Column<int>(type: "integer", nullable: false),
                    FirstSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_unanswered_questions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "knowledge_embeddings",
                columns: table => new
                {
                    KnowledgeItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmbeddingText = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_knowledge_embeddings", x => x.KnowledgeItemId);
                    table.ForeignKey(
                        name: "FK_knowledge_embeddings_knowledge_items_KnowledgeItemId",
                        column: x => x.KnowledgeItemId,
                        principalTable: "knowledge_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_unanswered_questions_NormalizedQuestion",
                table: "unanswered_questions",
                column: "NormalizedQuestion",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "knowledge_embeddings");

            migrationBuilder.DropTable(
                name: "unanswered_questions");

            migrationBuilder.DropTable(
                name: "knowledge_items");
        }
    }
}
